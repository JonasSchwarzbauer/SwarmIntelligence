using SwarmIntelligence.Logic.MapControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl
{
    public class MapQueryService : IMapQueryService
    {
        private readonly IObstacleStore _obstacleStore;
        private readonly IAgentStateStore _agentStateStore;

        public int GridWidth => _obstacleStore.GridWidth;
        public int GridHeight => _obstacleStore.GridHeight;
        public float CellSize => _obstacleStore.CellSize;

        public MapQueryService(IObstacleStore obstacleStore, IAgentStateStore agentStateStore)
        {
            _obstacleStore = obstacleStore ?? throw new ArgumentNullException(nameof(obstacleStore));
            _agentStateStore = agentStateStore ?? throw new ArgumentNullException(nameof(agentStateStore));
        }

        public AgentGeoData? GetAgentDataById(byte agentId) 
        {
            if (_agentStateStore.TryGetAgentState(agentId, out var agentData)){
                return agentData;
            }
            else
            {
                return null;
            }
        }

        public IReadOnlyCollection<AgentGeoData> GetAllAgentData()
        {
            return _agentStateStore.GetAllAgents();
        }

        public bool[,] GetMapGrid()
        {
            var grid = _obstacleStore.GetGridSnapshot();
            //foreach (var agent in _agentStateStore.GetAllAgents())
            //{
            //    var position = agent.Position;
            //    if (position.TryToGridIndices(_obstacleStore.CellSize, _obstacleStore.GridWidth, _obstacleStore.GridHeight, out var cell))
            //    {
            //        grid[cell.X, cell.Y] = true;
            //    }
            //}
            return grid;
        }

        public bool[,] GetObstacleGrid()
        {
            return _obstacleStore.GetGridSnapshot();
        }
    }
}
