namespace Webserver.Models.SystemRelated
{
    public record MapWorkerState
    {
        public required string CurrentState { get; init; }
        public required DateTime LastUpdated { get; init; }
    }
}
