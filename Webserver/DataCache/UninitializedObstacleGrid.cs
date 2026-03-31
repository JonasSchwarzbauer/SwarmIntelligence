using SwarmIntelligence.Logic.MapControl;
using Webserver.Interfaces;
using Webserver.Models;

namespace Webserver.DataCache
{
    /// <summary>
    /// Null-object implementation of <see cref="IObstacleGrid"/> used before
    /// the cache is initialized. Every query reports "not initialized" so
    /// callers can distinguish this state cleanly.
    /// </summary>
    public sealed class UninitializedObstacleGrid : IObstacleGrid
    {
        public static UninitializedObstacleGrid Instance { get; } = new();

        public bool IsInitialized => false;
        public int Width => 0;
        public int Height => 0;

        public void UpdateCell(int x, int y, ObstacleType type)
            => throw new InvalidOperationException("Obstacle grid has not been initialized.");

        public bool[][] GetGrid()
            => throw new InvalidOperationException("Obstacle grid has not been initialized.");

        public IEnumerable<CellInfo> GetCells()
            => throw new InvalidOperationException("Obstacle grid has not been initialized.");
    }
}
