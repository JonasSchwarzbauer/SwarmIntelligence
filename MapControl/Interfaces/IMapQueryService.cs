using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    public interface IMapQueryService
    {
        AgentGeoData? GetAgentDataById(byte agentId);
        IReadOnlyCollection<AgentGeoData> GetAllAgentData();
        bool[,] GetMapGrid();
        bool[,] GetObstacleGrid();
        float CellSize { get; }
        int GridWidth { get; }
        int GridHeight { get; }
    }
}
