using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl
{
    public class MapUpdateCoordinator : IMapUpdateCoordinator
    {
        private readonly IAgentStateStore _agentStateStore;
        private readonly IObstacleStore _obstacleStore;

        private const float MaxObstacleDetectionDistanceM = 1.0f;
        private const float MinObstacleDetectionDistanceM = 0.5f;
        private const float MinDistanceToAgentM = 0.5f;

        public event EventHandler<MapUpdatedEventArgs>? MapUpdated;
        public event EventHandler<AgentDataUpdatedEventArgs>? AgentStateUpdated;

        public MapUpdateCoordinator(IAgentStateStore agentStateStore, IObstacleStore obstacleStore)
        {
            _agentStateStore = agentStateStore ?? throw new ArgumentNullException(nameof(agentStateStore));
            _obstacleStore = obstacleStore ?? throw new ArgumentNullException(nameof(obstacleStore));
        }

        public void Update(AgentGeoData vehicleData)
        {
            _agentStateStore.UpdateAgentState(vehicleData);
            AgentStateUpdated?.Invoke(this, new AgentDataUpdatedEventArgs(vehicleData));

            //var obstaclePosition = CalculateObstaclePosition(vehicleData);

            //if (IsCloseToAgent(obstaclePosition))
            //{
            //    return;
            //}

            //if (vehicleData.FrontalDistance >= MinObstacleDetectionDistanceM && vehicleData.FrontalDistance <= MaxObstacleDetectionDistanceM
            //    && obstaclePosition.TryToGridIndices(_obstacleStore.CellSize, _obstacleStore.GridWidth, _obstacleStore.GridHeight, out var cell))
            //{
            //    _obstacleStore.UpdateCell(cell.X, cell.Y, ObstacleType.Physical);
            //    MapUpdated?.Invoke(this, new MapUpdatedEventArgs(obstaclePosition, cell, true));
            //}
        }

        public void UpdateGrid(ObstacleCellDto cell)
        {
            _obstacleStore.UpdateCell(cell.X, cell.Y, cell.Type);
            Console.WriteLine($"Updated cell {cell.X}, {cell.Y} to {cell.Type}");
        }

        private static Vector2 CalculateObstaclePosition(AgentGeoData vehicleData)
        {
            var position = vehicleData.Position;
            var distance = vehicleData.FrontalDistance;
            var heading = vehicleData.Orientation; // in radians

            // Old position + unit vector in heading direction * distance
            var obstaclePosition = position + new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * distance;
            return obstaclePosition;
        }

        private bool IsCloseToAgent(Vector2 position)
        {
            foreach (var agent in _agentStateStore.GetAllAgents())
            {
                if (Vector2.Distance(agent.Position, position) < MinDistanceToAgentM)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
