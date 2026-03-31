using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for map update events.
    /// </summary>
    public record class MapUpdatedDto
    {
        public required float X { get; init; }
        public required float Y { get; init; }
        public required int CellX { get; init; }
        public required int CellY { get; init; }
        public required bool Occupied { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
