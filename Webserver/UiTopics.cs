namespace Webserver
{
    public static class UiTopics
    {
        // Drive Control
        public const string CommandGenerated = "CommandGenerated";
        public const string CommandAssigned = "CommandAssigned";
        public const string CommandDispatched = "CommandDispatched";
        public const string CommandCleared = "CommandCleared";
        public const string AgentRegistration = "AgentRegistration";
        public const string DriveControlError = "DriveControlError";

        // Map Control
        public const string MapUpdated = "MapUpdated";
        public const string AgentData = "AgentData";
        public const string MapControlError = "MapControlError";
        public const string BufferInformation = "BufferInformation";

        // System
        public const string WorkerState = "WorkerState";
        public const string ManagerState = "ManagerState";
        public const string UsbStarted = "UsbStarted";

        // Tab Collections
        public const string FleetManagement = "FleetManagement";
        public const string AwarenessMap = "AwarenessMap";
    }
}
