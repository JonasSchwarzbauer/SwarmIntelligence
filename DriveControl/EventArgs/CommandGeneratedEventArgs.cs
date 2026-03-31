using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class CommandGeneratedEventArgs : System.EventArgs
    {
        public CommandGeneratedEventArgs(DriveCommand command)
        {
            Command = command;
            AgentId = command.AgentId;
        }

        /// <summary>
        /// Gets the generated command.
        /// </summary>
        public DriveCommand Command { get; }
        
        /// <summary>
        /// Gets the agent ID this command is for.
        /// </summary>
        public byte AgentId { get; }
        
        /// <summary>
        /// Gets the UTC timestamp when the command was generated.
        /// </summary>
        public DateTime GeneratedAt { get; } = DateTime.UtcNow;
    }
}
