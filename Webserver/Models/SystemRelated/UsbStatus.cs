namespace Webserver.Models.SystemRelated
{
    public record UsbStatus
    {
        public required bool Success { get; init; }
        public required string PortName { get; init; }
        public required int BaudRate { get; init; }
        public required string Message { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
