using SwarmIntelligence.Logic.DriveControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.OutOfUse
{
    /// <summary>
    /// Concrete mailbox for a single agent. Implements the specific command processing logic for the agent.
    /// </summary>
    public class AgentMailbox : AbstractMailbox
    {
        /// <summary>
        /// Creates a new mailbox for the specified agent with the given capacity.
        /// </summary>
        /// <param name="agentId">Identifier for the agent owning this mailbox.</param>
        /// <param name="capacity">Bounded capacity for the mailbox channel.</param>
        public AgentMailbox(byte agentId, int capacity) : base(agentId, capacity)
        {
        }

        /// <summary>
        /// Processes a single drive command. This implementation currently writes a diagnostic line to the console.
        /// Replace with real agent handling logic as needed.
        /// </summary>
        /// <param name="command">The command to process.</param>
        public override async Task ProcessCommand(DriveCommand command)
        {
            // Wait for 5s
            await Task.Delay(5000);
        }
    }
}
