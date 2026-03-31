using SwarmIntelligence.Logic.Setup;
using Webserver.Models;
using Webserver.Models.AgentRelated;
using Webserver.Models.SystemRelated;

namespace Webserver.Interfaces
{
    /// <summary>
    /// General-purpose, type-safe cache used by the application to store
    /// domain objects (for example: vehicle, command and obstacle states).
    /// Implementations provide per-type storage and a mechanism to observe
    /// updates for reactive consumers such as SignalR hubs.
    /// </summary>
    public interface ISwarmCache
    {
        IKeyedCache<AgentState> Agents { get; }
        IKeyedCache<CommandState> Commands { get; }
        IKeyedCache<AgentRegistrationState> Registrations { get; }
        IKeyedCache<AgentError> AgentErrors { get; }

        ISingletonCache<ManagerState> ManagerState { get; }
        ISingletonCache<MapWorkerState> MapWorkerState { get; }
        ISingletonCache<UsbStatus> UsbStatus { get; }
        ISingletonCache<BufferInformation> BufferInformation { get; }
        ISingletonCache<MapError> MapError { get; }
        ISingletonCache<SwarmModeState> SwarmMode { get; }
        ISingletonCache<SwarmSettings> SwarmSettings { get; }

        IObstacleGrid ObstacleGrid { get; }

        void InitializeCache(SwarmSettings settings);
    }
}
