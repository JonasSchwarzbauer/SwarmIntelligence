using Microsoft.AspNetCore.SignalR;
using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.Setup;
using Webserver.Interfaces;
using Webserver.Models;
using Webserver.Models.AgentRelated;

namespace Webserver.Hubs
{
    public class UiHub(ISwarmCache cache, IHubContext<DataHub, IDataHub> dataHubContext) : Hub<IUiHub>
    {
        private readonly ISwarmCache _cache = cache;
        private readonly IHubContext<DataHub, IDataHub> _dataHubContext = dataHubContext;

        #region Subscription Management

        public async Task Subscribe(string topic)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, topic);
            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        public async Task Unsubscribe(string topic)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, topic);
            Console.WriteLine($"Unsubscribed from topic: {topic}");
        }

        #endregion

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public Task<AgentState?> GetAgentState(string agentId)
        {
            return Task.FromResult(_cache.Agents.Get(agentId));
        }

        #region Init Data and User Inputs

        public Task OnInitDataReceived(InitDataWebserver data)
        {
            Console.WriteLine($"Received init data from Webserver: {data}");

            // Check if init data is valid
            if (data.AnchorPositions == null || data.AnchorPositions.Length != 3 || data.RequiredSlavesAmount < 0 || (data.CompassOffset < 0 || data.CompassOffset > 360))
            {
                throw new ArgumentException("Invalid init data received from Webserver");
            }

            // Initialize obstacle store with init data
            var swarmSettings = new SwarmSettings()
            {
                XFieldSize = data.AnchorPositions[2].X,
                YFieldSize = data.AnchorPositions[1].Y,
                NumberOfAgents = Convert.ToByte(data.RequiredSlavesAmount + 1),
                CompassOffset = data.CompassOffset,
                AnchorPositions = [.. data.AnchorPositions.Select(ap => new AnchorPosition(ap.X, ap.Y))]
            };

            _cache.InitializeCache(swarmSettings);

            return _dataHubContext.Clients.All.SendInitData(swarmSettings);
        }

        public Task OnFormationShapeReceived(FormationShapeDto shapeSettings)
        {
            Console.WriteLine($"Received formation shape from Webserver: {shapeSettings}");
            return _dataHubContext.Clients.All.SendFormationShape(shapeSettings);
        }

        public Task OnFormationPathReceived(FormationPathDto pathArgs)
        {
            var currentMode = _cache.SwarmMode.Get()?.Mode;
            var modeChanged = currentMode != SwarmMode.Formation;
            pathArgs = pathArgs with { ModeChanged = modeChanged };

            _cache.SwarmMode.Update(new SwarmModeState(SwarmMode.Formation));

            Console.WriteLine($"Received formation path from Webserver: {pathArgs}");
            return _dataHubContext.Clients.All.SendFormationPath(pathArgs);
        }

        public Task OnVehicleTargetsReceived(VehicleTargetsDto targetArgs)
        {
            var currentMode = _cache.SwarmMode.Get()?.Mode;
            var modeChanged = currentMode != SwarmMode.Manual;
            targetArgs = targetArgs with { ModeChanged = modeChanged };

            _cache.SwarmMode.Update(new SwarmModeState(SwarmMode.Manual));

            Console.WriteLine($"Received vehicle targets from Webserver: {targetArgs}");
            return _dataHubContext.Clients.All.SendVehicleTargets(targetArgs);
        }

        public Task OnVirtualObstaclesReceived(VirtualObstaclesDto obstaclesArgs)
        {
            Console.WriteLine($"Received virtual obstacles from Webserver: {obstaclesArgs}");
            foreach (var obstacle in obstaclesArgs.Obstacles)
            {
                _cache.ObstacleGrid.UpdateCell(obstacle.X, obstacle.Y, obstacle.Type);
            }

            // Notify both the core and the UI about the updated obstacles
            var notifyCore = _dataHubContext.Clients.All.SendVirtualObstacles(obstaclesArgs);
            var notifyUi = Clients.OthersInGroup(UiTopics.AwarenessMap).VirtualObstaclesUpdated(obstaclesArgs);

            return Task.WhenAll(notifyCore, notifyUi);
        }

        public Task OnManagerStateToggled()
        {
            Console.WriteLine("Manager State changed");
            return _dataHubContext.Clients.All.SendManagerStateChange();
        }

        public Task OnWorkerStateToggled()
        {
            Console.WriteLine("Worker State changed");
            return _dataHubContext.Clients.All.SendWorkerStateChange();
        }

        #endregion
    }
}