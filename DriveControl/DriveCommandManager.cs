using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.DriveControl.Interfaces;

namespace SwarmIntelligence.Logic.DriveControl
{
    /// <summary>
    /// Manages per-agent slots for drive commands. Handles slot creation on demand
    /// and provides lifecycle operations for starting and stopping processing.
    /// </summary>
    public class DriveCommandManager : IDriveCommandManager
    {
        private readonly ConcurrentDictionary<byte, AgentCommandSlot> _commandSlots;
        private bool _isRunning;

        #region Events

        public event EventHandler<AgentRegistrationEventArgs>? AgentRegistrationChanged;
        public event EventHandler<CommandAssignedEventArgs>? CommandAssigned;
        public event EventHandler<CommandClearedEventArgs>? CommandCleared;
        public event EventHandler<ManagerStateChangedEventArgs>? ManagerStateChanged;
        public event EventHandler<CommandErrorEventArgs>? ErrorOccurred;

        #endregion

        public bool IsRunning => _isRunning;

        public DriveCommandManager()
        {
            _commandSlots = new ConcurrentDictionary<byte, AgentCommandSlot>();
            _isRunning = false;
        }

        #region Lifecycle

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            var handler = ManagerStateChanged;
            handler?.Invoke(this, new ManagerStateChangedEventArgs(_isRunning));
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;

                var slots = _commandSlots.Values.ToArray();
                _commandSlots.Clear();

                foreach (var slot in slots)
                {
                    CleanupSlot(slot);

                    var regHandler = AgentRegistrationChanged;
                    regHandler?.Invoke(this, new AgentRegistrationEventArgs(slot.AgentId, RegistrationType.Unregistered));
                }

                var stateHandler = ManagerStateChanged;
                stateHandler?.Invoke(this, new ManagerStateChangedEventArgs(_isRunning));
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new CommandErrorEventArgs(ex, "Error stopping DriveCommandManager", 0, "Drive Control"));
            }
        }

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }

        #endregion

        #region Control Methods

        public void AssignCommand(DriveCommand command)
        {
            try
            {
                if (!_isRunning) {
                    throw new InvalidOperationException("DriveCommandManager is not running.");
                }

                if (!_commandSlots.TryGetValue(command.AgentId, out var slot))
                {
                    var newSlot = CreateSlot(command.AgentId);
                    if (_commandSlots.TryAdd(command.AgentId, newSlot))
                    {
                        slot = newSlot;

                        // Notify that an agent slot was created on demand
                        AgentRegistrationChanged?.Invoke(this, new AgentRegistrationEventArgs(command.AgentId, RegistrationType.Registered));
                    }
                    else
                    {
                        CleanupSlot(newSlot);
                        _commandSlots.TryGetValue(command.AgentId, out slot);
                    }
                }

                slot?.Assign(command);
            }
            catch (InvalidOperationException ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Cannot assign command when manager is stopped.", command.AgentId, "Drive Control"));

            }
            catch (Exception ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Failed to assign command in DriveManager.", command.AgentId, "Drive Control"));
            }
        }

        public void ClearCommand(byte agentId)
        {
            try
            {
                if (!_isRunning)
                    throw new InvalidOperationException("DriveCommandManager is not running.");

                if (_commandSlots.TryGetValue(agentId, out var slot))
                {
                    slot.Clear();
                }
            }
            catch (InvalidOperationException ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Cannot clear command when manager is stopped.", agentId, "Drive Control"));
            }
        }

        public bool RegisterAgent(byte agentId)
        {
            try
            {
                if (!IsRunning)
                    throw new InvalidOperationException("DriveCommandManager is not running.");

                if (_commandSlots.ContainsKey(agentId)) return false;

                var slot = CreateSlot(agentId);

                if (_commandSlots.TryAdd(agentId, slot))
                {
                    var handler = AgentRegistrationChanged;
                    handler?.Invoke(this, new AgentRegistrationEventArgs(agentId, RegistrationType.Registered));
                    return true;
                }

                CleanupSlot(slot);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Cannot register agent when manager is stopped.", agentId, "Drive Control"));
                return false;
            }
        }

        public bool UnregisterAgent(byte agentId)
        {
            try
            {
                if (!IsRunning)
                    throw new InvalidOperationException("DriveCommandManager is not running.");

                if (_commandSlots.TryRemove(agentId, out var slot))
                {
                    CleanupSlot(slot);

                    var handler = AgentRegistrationChanged;
                    handler?.Invoke(this, new AgentRegistrationEventArgs(agentId, RegistrationType.Unregistered));
                    return true;
                }

                return false;
            }
            catch (InvalidOperationException ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Cannot unregister agent when manager is stopped.", agentId, "Drive Control"));
                return false;
            }
        }

        #endregion

        #region Query Methods

        public IReadOnlyDictionary<byte, DriveCommand> GetCommands()
        {
            return _commandSlots
                .Where(kvp => kvp.Value.Command.HasValue)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Command!.Value);
        }

        public IReadOnlyCollection<byte> GetRegisteredAgents()
        {
            return [.. _commandSlots.Keys];
        }

        public AgentSlotState? GetAgentSlotState(byte agentId)
        {
            if (_commandSlots.TryGetValue(agentId, out var slot))
            {
                return new AgentSlotState(slot.AgentId, slot.Command, slot.AssignedAt);
            }

            return null;
        }

        public IReadOnlyList<AgentSlotState> GetAllSlotStates()
        {
            return [.. _commandSlots.Values.Select(slot => new AgentSlotState(slot.AgentId, slot.Command, slot.AssignedAt))];
        }

        #endregion

        #region Private Helpers

        private AgentCommandSlot CreateSlot(byte agentId)
        {
            var slot = new AgentCommandSlot(agentId);
            slot.CommandAssigned += OnSlotCommandAssigned;
            slot.CommandCleared += OnSlotCommandCleared;
            return slot;
        }

        private void CleanupSlot(AgentCommandSlot slot)
        {
            slot.CommandAssigned -= OnSlotCommandAssigned;
            slot.CommandCleared -= OnSlotCommandCleared;
        }

        private void OnSlotCommandAssigned(object? sender, CommandAssignedEventArgs e)
        {
            var handler = CommandAssigned;
            handler?.Invoke(this, e);
        }

        private void OnSlotCommandCleared(object? sender, CommandClearedEventArgs e)
        {
            var handler = CommandCleared;
            handler?.Invoke(this, e);
        }

        #endregion
    }
}
