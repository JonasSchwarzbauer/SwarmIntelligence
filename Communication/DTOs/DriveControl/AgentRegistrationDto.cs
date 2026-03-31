using System;

namespace SwarmIntelligence.Logic.Communication.DTOs.DriveControl
{
    /// <summary>
    /// DTO for agent registration events.
    /// </summary>
    public record class AgentRegistrationDto
    {
        public required byte AgentId { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string RegistrationType { get; init; }
    }
}
