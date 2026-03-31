using SwarmIntelligence.Logic.Communication.USB;
using System;
using System.Collections.Generic;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    public interface IAgentStateStore
    {
        void UpdateAgentState(AgentGeoData vehicleState);

        bool TryGetAgentState(byte agentId, out AgentGeoData agentData);

        IReadOnlyCollection<AgentGeoData> GetAllAgents();

        bool RemoveAgent(byte agentId);
        
        bool ContainsAgent(byte agentId);
    }
}