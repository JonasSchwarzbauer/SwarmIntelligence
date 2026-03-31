using SwarmIntelligence.Logic.DriveControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.DriveControl
{
    public class AgentCommandSlot
    {
        private readonly object _sync = new();

        public byte AgentId { get; }
        public DriveCommand? Command { get; private set; }
        public DateTime? AssignedAt { get; private set; }

        public event EventHandler<CommandAssignedEventArgs>? CommandAssigned;
        public event EventHandler<CommandClearedEventArgs>? CommandCleared;

        public AgentCommandSlot(byte agentId)
        {
            AgentId = agentId;
        }

        public void Assign(DriveCommand command)
        {
            EventHandler<CommandAssignedEventArgs>? handler;
            DateTime assignedAt;

            lock (_sync)
            {
                Command = command;
                AssignedAt = DateTime.UtcNow;
                assignedAt = AssignedAt.Value;
                handler = CommandAssigned;
            }

            handler?.Invoke(this, new CommandAssignedEventArgs(command, assignedAt));
        }

        public void Clear()
        {
            EventHandler<CommandClearedEventArgs>? handler;

            lock (_sync)
            {
                Command = null;
                AssignedAt = null;
                handler = CommandCleared;
            }

            handler?.Invoke(this, new CommandClearedEventArgs(AgentId));
        }
    }
}
