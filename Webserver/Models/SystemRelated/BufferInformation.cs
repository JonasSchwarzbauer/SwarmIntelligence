namespace Webserver.Models.SystemRelated
{
    public record BufferInformation
    {
        public required bool Success { get; init; }
        public required int BufferCount { get; init; }
        public required int BufferCapacity { get; init; }
        public required float BufferUsagePercentage { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
