using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class CommandAssignedEventArgs : System.EventArgs
    {
        public CommandAssignedEventArgs(DriveCommand command, DateTime assignedAt)
        {
            Command = command;
            AgentId = command.AgentId;
            AssignedAt = assignedAt;
        }

        public DriveCommand Command { get; }
        public byte AgentId { get; }
        public DateTime AssignedAt { get; }
    }
}
