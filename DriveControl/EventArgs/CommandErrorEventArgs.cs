using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.EventArgs
{
    public class CommandErrorEventArgs : SwarmErrorEventArgs
    {
        public CommandErrorEventArgs(Exception exception, string message, byte agentId, string sourceContext = "Drive Control") : base(exception, message, sourceContext)
        {
            AgentId = agentId;
        }

        public byte AgentId { get; }

    }
}
