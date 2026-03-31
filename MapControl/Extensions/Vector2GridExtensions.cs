using System;
using System.Numerics;

namespace SwarmIntelligence.Logic.MapControl
{
    public static class Vector2GridExtensions
    {
        extension(Vector2 position)
        {
            public (int X, int Y) ToGridIndicesChecked(float cellSize, int gridWidth, int gridHeight)
            {
                if (TryToGridIndices(position, cellSize, gridWidth, gridHeight, out var indices))
                {
                    return indices;
                }

                throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the grid bounds.");
            }

            public bool TryToGridIndices(float cellSize, int gridWidth, int gridHeight, out (int X, int Y) indices)
            {
                if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be positive.");

                indices = default;

                if (float.IsNaN(position.X) || float.IsNaN(position.Y) || float.IsInfinity(position.X) || float.IsInfinity(position.Y))
                {
                    return false;
                }

                var x = (int)MathF.Floor(position.X / cellSize);
                var y = (int)MathF.Floor(position.Y / cellSize);

                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    return false;
                }

                indices = (x, y);
                return true;
            }
        }
    }
}
