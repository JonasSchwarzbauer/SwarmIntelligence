namespace Webserver.Models.AgentRelated
{
    public record AgentError
    {
        public required byte AgentId { get; init; }
        public required string Message { get; init; }
        public required string Source { get; init; }
        public required string SourceContext { get; init; }
        public required string ExceptionMessage { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
