using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.DriveControl.Interfaces;
using SwarmIntelligence.Logic.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl
{
    public class DriveController : IAsyncDisposable
    {
        private readonly IDriveCommandGenerator _generator;
        private readonly IDriveCommandManager _manager;
        private readonly IDriveCommandDispatcher _dispatcher;

        private bool _disposed;

        #region Events

        public event EventHandler<AgentRegistrationEventArgs>? AgentRegistrationChanged;

        public event EventHandler<CommandAssignedEventArgs>? CommandAssigned;

        public event EventHandler<CommandClearedEventArgs>? CommandCleared;

        public event EventHandler<ManagerStateChangedEventArgs>? ManagerStateChanged;

        public event EventHandler<CommandErrorEventArgs>? ErrorOccurred;

        public event EventHandler<CommandGeneratedEventArgs>? CommandGenerated;

        public event EventHandler<CommandDispatchedEventArgs>? CommandDispatched;

        #endregion

        public DriveController(
            IDriveCommandGenerator generator,
            IDriveCommandManager manager,
            IDriveCommandDispatcher dispatcher)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            // Subscribe to generator events for relaying
            _generator.CommandGenerated += OnCommandGenerated;

            // Subscribe to manager events for relaying
            _manager.CommandAssigned += OnCommandAssigned;
            _manager.CommandCleared += OnCommandCleared;
            _manager.AgentRegistrationChanged += OnAgentRegistrationChanged;
            _manager.ManagerStateChanged += OnManagerStateChanged;
            _manager.ErrorOccurred += OnManagerErrorOccurred;

            // Subscribe to dispatcher events for relaying
            _dispatcher.CommandDispatched += OnCommandDispatched;
        }

        public bool IsRunning => _manager.IsRunning;

        #region Lifecycle Methods

        public void Start() => _manager.Start();

        public void Stop() => _manager.Stop();

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                // Detach event handlers from generator
                _generator.CommandGenerated -= OnCommandGenerated;

                // Detach event handlers from manager
                _manager.CommandAssigned -= OnCommandAssigned;
                _manager.CommandCleared -= OnCommandCleared;
                _manager.AgentRegistrationChanged -= OnAgentRegistrationChanged;
                _manager.ManagerStateChanged -= OnManagerStateChanged;
                _manager.ErrorOccurred -= OnManagerErrorOccurred;

                // Detach event handlers from dispatcher
                _dispatcher.CommandDispatched -= OnCommandDispatched;

                // Also dispose the manager when the controller is disposed
                try
                {
                    await _manager.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Swallow exceptions during manager dispose
                }
            }
            catch
            {
                // Swallow exceptions during dispose
            }
            finally
            {
                // Clear public event subscribers
                CommandGenerated = null;
                AgentRegistrationChanged = null;
                CommandAssigned = null;
                CommandCleared = null;
                CommandDispatched = null;
                ManagerStateChanged = null;
                ErrorOccurred = null;
            }
        }

        #endregion

        #region Control Methods

        public Task ProcessNavResult(NavResult navResult)
        {
            var driveCommand = _generator.GenerateCommand(navResult);
            _manager.AssignCommand(driveCommand);
            DispatchSafe(driveCommand);

            return Task.CompletedTask;
        }

        public Task ClearCommand(byte agentId)
        {
            _manager.ClearCommand(agentId);

            return Task.CompletedTask;
        }

        public bool RegisterAgent(byte agentId)
        {
            return _manager.RegisterAgent(agentId);
        }

        public bool UnregisterAgent(byte agentId)
        {
            return _manager.UnregisterAgent(agentId);
        }

        #endregion

        #region State Inspection

        public IReadOnlyDictionary<byte, DriveCommand> GetCommands()
        {
            return _manager.GetCommands();
        }

        public IReadOnlyCollection<byte> GetRegisteredAgents()
        {
            return _manager.GetRegisteredAgents();
        }

        public AgentSlotState? GetAgentSlotState(byte agentId)
        {
            return _manager.GetAgentSlotState(agentId);
        }

        public IReadOnlyList<AgentSlotState> GetAllSlotStates()
        {
            return _manager.GetAllSlotStates();
        }

        #endregion

        #region Event Invokers

        private void OnCommandGenerated(object? sender, CommandGeneratedEventArgs e)
        {
            var handler = CommandGenerated;
            handler?.Invoke(this, e);
        }

        private void OnCommandAssigned(object? sender, CommandAssignedEventArgs e)
        {
            var handler = CommandAssigned;
            handler?.Invoke(this, e);
        }

        private void OnCommandCleared(object? sender, CommandClearedEventArgs e)
        {
            var handler = CommandCleared;
            handler?.Invoke(this, e);
        }

        private void OnCommandDispatched(object? sender, CommandDispatchedEventArgs e)
        {
            var handler = CommandDispatched;
            handler?.Invoke(this, e);
        }

        private void OnAgentRegistrationChanged(object? sender, AgentRegistrationEventArgs e)
        {
            var handler = AgentRegistrationChanged;
            handler?.Invoke(this, e);
        }

        private void OnManagerStateChanged(object? sender, ManagerStateChangedEventArgs e)
        {
            var handler = ManagerStateChanged;
            handler?.Invoke(this, e);
        }

        private void OnManagerErrorOccurred(object? sender, CommandErrorEventArgs e)
        {
            var handler = ErrorOccurred;
            handler?.Invoke(this, e);
        }

        private void DispatchSafe(DriveCommand command)
        {
            try
            {
                _dispatcher.DispatchCommand(command);
            }
            catch (Exception ex)
            {
                var handler = ErrorOccurred;
                handler?.Invoke(this, new CommandErrorEventArgs(ex, "Dispatching command failed.", command.AgentId, "Drive Control"));
            }
        }

        #endregion
    }
}
