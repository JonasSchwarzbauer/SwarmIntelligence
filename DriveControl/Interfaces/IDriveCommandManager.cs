using SwarmIntelligence.Logic.DriveControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.Interfaces
{
    /// <summary>
    /// Represents a manager that coordinates drive commands across multiple agent mailboxes.
    /// </summary>
    public interface IDriveCommandManager : IAsyncDisposable
    {
        event EventHandler<AgentRegistrationEventArgs>? AgentRegistrationChanged;
        event EventHandler<CommandAssignedEventArgs>? CommandAssigned;
        event EventHandler<CommandClearedEventArgs>? CommandCleared;
        event EventHandler<ManagerStateChangedEventArgs>? ManagerStateChanged;
        event EventHandler<CommandErrorEventArgs>? ErrorOccurred;
        bool IsRunning { get; }
        void Start();

        void Stop();

        bool RegisterAgent(byte agentId);

        bool UnregisterAgent(byte agentId);

        void AssignCommand(DriveCommand command);

        void ClearCommand(byte agentId);

        IReadOnlyDictionary<byte, DriveCommand> GetCommands();

        IReadOnlyCollection<byte> GetRegisteredAgents();

        AgentSlotState? GetAgentSlotState(byte agentId);

        IReadOnlyList<AgentSlotState> GetAllSlotStates();
    }
}
