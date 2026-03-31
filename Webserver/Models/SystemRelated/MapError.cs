namespace Webserver.Models.SystemRelated
{
    public record MapError
    {
        public required string Message { get; init; }
        public required string Source { get; init; }
        public required string SourceContext { get; init; }
        public required string ExceptionMessage { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
