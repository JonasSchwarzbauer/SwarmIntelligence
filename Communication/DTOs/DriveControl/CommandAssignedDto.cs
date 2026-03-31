using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for command assignment events.
    /// </summary>
    public record class CommandAssignedDto
    {
        public required DriveCommandDto Command { get; init; }
        public required byte AgentId { get; init; }
        public required DateTime AssignedAt { get; init; }
    }
}
