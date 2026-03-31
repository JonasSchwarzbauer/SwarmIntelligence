namespace Webserver.Models.AgentRelated
{
    public record AgentRegistrationState
    {
        public required byte AgentId { get; init; }
        public required string RegistrationType { get; init; }
        public required DateTime RegisteredAt { get; init; }
    }
}
