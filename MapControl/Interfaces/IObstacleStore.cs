using System;
using SwarmIntelligence.Logic.MapControl;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    /// <summary>
    /// Thread-safe obstacle grid store for map/pathfinding.
    /// </summary>
    public interface IObstacleStore : IDisposable
    {
        float CellSize { get; }
        int GridHeight { get; }
        int GridWidth { get; }

        void Clear();
        bool[,] GetGridSnapshot();
        ObstacleType[,] GetTypeGridSnapshot();
        bool IsOccupied(int x, int y);
        void UpdateCell(int x, int y, ObstacleType type);
    }
}