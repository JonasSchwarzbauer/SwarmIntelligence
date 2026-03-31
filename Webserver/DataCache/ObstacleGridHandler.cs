using SwarmIntelligence.Logic.MapControl;
using Webserver.Interfaces;
using Webserver.Models;

namespace Webserver.DataCache
{
    /// <summary>
    /// Wraps the underlying <see cref="ObstacleStore"/> and exposes it through
    /// the <see cref="IObstacleGrid"/> contract. The grid is the single source
    /// of truth; <see cref="CellInfo"/> is produced only as a DTO projection.
    /// </summary>
    public class ObstacleGridHandler : IObstacleGrid
    {
        private readonly ObstacleStore _obstacleGrid;

        public ObstacleGridHandler(ObstacleStore obstacleGrid)
        {
            _obstacleGrid = obstacleGrid;
        }

        public bool IsInitialized => true;
        public int Width => _obstacleGrid.GridWidth;
        public int Height => _obstacleGrid.GridHeight;

        public void UpdateCell(int x, int y, ObstacleType type)
        {
            _obstacleGrid.UpdateCell(x, y, type);
        }

        public bool[][] GetGrid()
        {
            var grid = _obstacleGrid.GetGridSnapshot();
            var rows = grid.GetLength(0);
            var cols = grid.GetLength(1);
            var jagged = new bool[rows][];

            for (var r = 0; r < rows; r++)
            {
                var row = new bool[cols];
                for (var c = 0; c < cols; c++)
                    row[c] = grid[r, c];

                jagged[r] = row;
            }

            return jagged;
        }

        public IEnumerable<CellInfo> GetCells()
        {
            var grid = _obstacleGrid.GetTypeGridSnapshot();

            for (int x = 0; x < _obstacleGrid.GridHeight; x++)
            {
                for (int y = 0; y < _obstacleGrid.GridWidth; y++)
                {
                    yield return new CellInfo(x, y, grid[x, y]);
                }
            }
        }
    }
}