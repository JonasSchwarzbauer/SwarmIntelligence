using System;
using System.Numerics;
using System.Threading;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using SwarmIntelligence.Logic.Setup;

namespace SwarmIntelligence.Logic.MapControl
{
    public class ObstacleStore : IObstacleStore
    {
        private readonly ObstacleType[,] _grid;
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public int GridWidth { get; }
        public int GridHeight { get; }
        public float CellSize { get; }

        public ObstacleStore(SwarmSettings settings)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.XFieldSize, nameof(settings.XFieldSize));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.YFieldSize, nameof(settings.YFieldSize));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.GridSize, nameof(settings.GridSize));

            CellSize = settings.GridSize;
            GridHeight = (int)MathF.Floor(settings.XFieldSize / settings.GridSize);
            GridWidth = (int)MathF.Floor(settings.YFieldSize / settings.GridSize);
            _grid = new ObstacleType[GridHeight, GridWidth];
        }

        public void UpdateCell(int x, int y, ObstacleType type)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_grid[x, y] != ObstacleType.Physical) // Prevent overwriting physical obstacles
                    _grid[x, y] = type;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsOccupied(int x, int y)
        {
            _lock.EnterReadLock();
            try
            {
                return _grid[x, y] != ObstacleType.Free;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public ObstacleType[,] GetTypeGridSnapshot()
        {
            _lock.EnterReadLock();
            try
            {
                var copy = new ObstacleType[GridHeight, GridWidth];
                Array.Copy(_grid, copy, _grid.Length);
                return copy;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool[,] GetGridSnapshot()
        {
            _lock.EnterReadLock();
            try
            {
                var boolGrid = new bool[GridHeight, GridWidth];
                for (var x = 0; x < GridHeight; x++)
                {
                    for (var y = 0; y < GridWidth; y++)
                    {
                        boolGrid[x, y] = _grid[x, y] != ObstacleType.Free;
                    }
                }
                return boolGrid;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                Array.Clear(_grid, 0, _grid.Length);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose() => _lock.Dispose();
    }
}
