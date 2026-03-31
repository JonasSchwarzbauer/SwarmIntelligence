using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for command generation events.
    /// </summary>
    public record class CommandGeneratedDto
    {
        public required DriveCommandDto Command { get; init; }
        public required DateTime GeneratedAt { get; init; }
        public required byte AgentId { get; init; }
    }
}
