using System;
using System.Numerics;

namespace SwarmIntelligence.Logic.Communication.DTOs.MapControl
{
    /// <summary>
    /// DTO for agent geo data updates.
    /// </summary>
    public record class AgentDataDto
    {
        public required byte AgentId { get; init; }
        public required float X { get; init; }
        public required float Y { get; init; }
        public required float Orientation { get; init; }
        public required float Velocity { get; init; }
        public required float FrontalDistance { get; init; }
        public required WaypointDto Target { get; init; }
        public required List<string> Flags { get; init; }
        public required float DwmSuccessRate { get; init; }
        public required DateTime DataReceived { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}