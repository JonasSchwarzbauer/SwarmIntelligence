using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for command cleared events.
    /// </summary>
    public record class CommandClearedDto
    {
        public required byte AgentId { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
