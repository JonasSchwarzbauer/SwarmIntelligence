using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;

namespace Webserver.Models.AgentRelated
{
    public record CommandState
    {
        public required byte AgentId { get; init; }
        public DriveCommandDto? CurrentCommand { get; init; }
        public required CommandPhase Phase { get; init; }
        public required DateTime GeneratedAt { get; init; }
        public required DateTime? AssignedAt { get; init; }
        public required DateTime? DispatchedAt { get; init; }
        public double? DispatchLatencyMs { get; init; }
        public required DateTime LastUpdated { get; init; }
    }

    public enum CommandPhase
    {
        Generated,
        Assigned,
        Dispatched,
        Completed,
        Cleared
    }
}
