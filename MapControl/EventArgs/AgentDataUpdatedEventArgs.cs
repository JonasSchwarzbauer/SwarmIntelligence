using SwarmIntelligence.Logic.Communication.USB;
using System;

namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    public class AgentDataUpdatedEventArgs : System.EventArgs
    {
        public AgentDataUpdatedEventArgs(AgentGeoData agentData)
        {
            AgentData = agentData;
        }

        public AgentGeoData AgentData { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
