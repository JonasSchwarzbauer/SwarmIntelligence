using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO representation of a DriveCommand for SignalR transmission.
    /// </summary>
    public record class DriveCommandDto
    {
        public required byte AgentId { get; init; }
        public required List<string> DriveFlags { get; init; }
        public required WaypointDto[] Waypoints { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
