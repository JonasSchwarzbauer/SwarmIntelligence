using System;
using System.Numerics;

namespace SwarmIntelligence.Logic.MapControl.EventArgs
{
    public class MapUpdatedEventArgs : System.EventArgs
    {
        public MapUpdatedEventArgs(Vector2 position, (int, int) cell, bool occupied)
        {
            Position = position;
            Cell = cell;
            Occupied = occupied;
        }

        public Vector2 Position { get; }
        public (int, int) Cell { get; }
        public bool Occupied { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
