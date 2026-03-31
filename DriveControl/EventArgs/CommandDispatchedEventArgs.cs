using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class CommandDispatchedEventArgs : System.EventArgs
    {
        public CommandDispatchedEventArgs(DriveCommand command)
        {
            Command = command;
            AgentId = command.AgentId;
        }

        /// <summary>
        /// Gets the command that was dispatched.
        /// </summary>
        public DriveCommand Command { get; }
        
        /// <summary>
        /// Gets the agent ID this command was dispatched for.
        /// </summary>
        public byte AgentId { get; }
        
        /// <summary>
        /// Gets the UTC timestamp when the command was dispatched.
        /// </summary>
        public DateTime DispatchedAt { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the time elapsed since the command was generated.
        /// </summary>
        public TimeSpan DispatchLatency => DispatchedAt - Command.TimestampCreated;
    }
}
