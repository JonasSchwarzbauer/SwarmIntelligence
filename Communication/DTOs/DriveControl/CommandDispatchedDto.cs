using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for command dispatch events.
    /// </summary>
    public record class CommandDispatchedDto
    {
        public required DriveCommandDto Command { get; init; }
        public required byte AgentId { get; init; }
        public required double DispatchLatencyMs { get; init; }
        public required DateTime DispatchedAt { get; init; }
    }
}
