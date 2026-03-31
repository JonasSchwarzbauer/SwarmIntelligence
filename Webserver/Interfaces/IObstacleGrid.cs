using SwarmIntelligence.Logic.MapControl;
using Webserver.Models;

namespace Webserver.Interfaces
{
    /// <summary>
    /// Dedicated cache for the obstacle grid. Provides direct cell-level
    /// updates and both dense (<c>bool[][]</c>) and sparse (<see cref="CellInfo"/>)
    /// retrieval without relying on string-keyed semantics.
    /// </summary>
    public interface IObstacleGrid
    {
        bool IsInitialized { get; }
        int Width { get; }
        int Height { get; }

        void UpdateCell(int x, int y, ObstacleType type);
        bool[][] GetGrid();
        IEnumerable<CellInfo> GetCells();
    }
}
