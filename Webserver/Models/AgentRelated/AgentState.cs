using SwarmIntelligence.Logic.Communication.DTOs;

namespace Webserver.Models.AgentRelated
{
    public record AgentState
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
        public required DateTime Timestamp { get; init; }
        public required DateTime DataReceived { get; init; }
    }
}