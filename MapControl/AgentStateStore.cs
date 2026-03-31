using Microsoft.Extensions.Logging;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SwarmIntelligence.Logic.MapControl
{
    public class AgentStateStore : IAgentStateStore
    {
        private readonly ConcurrentDictionary<byte, AgentGeoData> _agentStates;

        public AgentStateStore()
        {
            _agentStates = new ConcurrentDictionary<byte, AgentGeoData>();
        }

        /// <inheritdoc />
        public void UpdateAgentState(AgentGeoData data)
        {
            _agentStates.AddOrUpdate(data.AgentId, data, (key, existingValue) => data);
        }

        /// <inheritdoc />
        public bool TryGetAgentState(byte agentId, out AgentGeoData agentData)
        {
            return _agentStates.TryGetValue(agentId, out agentData);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<AgentGeoData> GetAllAgents()
        {
            // Return a snapshot of the values
            return [.. _agentStates.Values];
        }

        /// <inheritdoc />
        public bool RemoveAgent(byte agentId)
        {
            return _agentStates.TryRemove(agentId, out _);
        }

        /// <inheritdoc />
        public bool ContainsAgent(byte agentId)
        {
            return _agentStates.ContainsKey(agentId);
        }
    }
}
