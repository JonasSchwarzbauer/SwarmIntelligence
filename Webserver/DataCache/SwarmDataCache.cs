using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.Setup;
using Webserver.Interfaces;
using Webserver.Models;
using Webserver.Models.AgentRelated;
using Webserver.Models.SystemRelated;

namespace Webserver.DataCache
{
    /// <summary>
    /// Application-level cache that provides per-type storage and a unified API
    /// for retrieving and observing domain objects used by the swarm system.
    /// It registers and exposes concrete <see cref="ICacheTypeHandler{T}"/>
    /// </summary>
    public class SwarmDataCache : ISwarmCache
    {
        private const string SINGLETON_KEY = "singleton";

        public IKeyedCache<AgentState> Agents { get; private set; }
        public IKeyedCache<CommandState> Commands { get; private set; }
        public IKeyedCache<AgentRegistrationState> Registrations { get; private set; }
        public IKeyedCache<AgentError> AgentErrors { get; private set; }

        public ISingletonCache<ManagerState> ManagerState { get; private set; }
        public ISingletonCache<MapWorkerState> MapWorkerState { get; private set; }
        public ISingletonCache<UsbStatus> UsbStatus { get; private set; }
        public ISingletonCache<BufferInformation> BufferInformation { get; private set; }
        public ISingletonCache<MapError> MapError { get; private set; }
        public ISingletonCache<SwarmModeState> SwarmMode { get; private set; }
        public ISingletonCache<SwarmSettings> SwarmSettings { get; private set; }

        public IObstacleGrid ObstacleGrid { get; private set; } = UninitializedObstacleGrid.Instance;

        public SwarmDataCache()
        {
            ManagerState = new SingletonCacheAdapter<ManagerState>(new CacheTypeHandler<ManagerState>(), SINGLETON_KEY);
            Commands = new KeyedCacheAdapter<CommandState>(new CacheTypeHandler<CommandState>());
            Registrations = new KeyedCacheAdapter<AgentRegistrationState>(new CacheTypeHandler<AgentRegistrationState>());
            AgentErrors = new KeyedCacheAdapter<AgentError>(new CacheTypeHandler<AgentError>());

            MapWorkerState = new SingletonCacheAdapter<MapWorkerState>(new CacheTypeHandler<MapWorkerState>(), SINGLETON_KEY);
            Agents = new KeyedCacheAdapter<AgentState>(new CacheTypeHandler<AgentState>());
            UsbStatus = new SingletonCacheAdapter<UsbStatus>(new CacheTypeHandler<UsbStatus>(), SINGLETON_KEY);
            BufferInformation = new SingletonCacheAdapter<BufferInformation>(new CacheTypeHandler<BufferInformation>(), SINGLETON_KEY);
            MapError = new SingletonCacheAdapter<MapError>(new CacheTypeHandler<MapError>(), SINGLETON_KEY);

            SwarmMode = new SingletonCacheAdapter<SwarmModeState>(new CacheTypeHandler<SwarmModeState>(), SINGLETON_KEY);
            SwarmSettings = new SingletonCacheAdapter<SwarmSettings>(new CacheTypeHandler<SwarmSettings>(), SINGLETON_KEY);
        }

        /// <summary>
        /// Construct a cache and register per-type handlers that share the
        /// specific storage dictionaries and subjects for update notifications.
        /// </summary>
        public void InitializeCache(SwarmSettings settings)
        {
            var obstacleGrid = new ObstacleStore(settings);
            ObstacleGrid = new ObstacleGridHandler(obstacleGrid);

            // Add settings to cache for UI
            SwarmSettings.Update(settings);
            SwarmMode.Update(new SwarmModeState(Webserver.Models.SwarmMode.Manual));
        }
    }
}