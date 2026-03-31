namespace Webserver.Models.SystemRelated
{
    public record ManagerState
    {
        public required bool IsRunning { get; init; }
        public required DateTime LastUpdated { get; init; }
    }
}
