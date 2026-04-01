using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.DriveControl;
using SwarmIntelligence.Logic.DriveControl.Interfaces;
using SwarmIntelligence.Logic.FormationControl;
using SwarmIntelligence.Logic.FormationControl.Factories;
using SwarmIntelligence.Logic.FormationControl.ImplementedFormationShapes;
using SwarmIntelligence.Logic.FormationControl.Interfaces;
using SwarmIntelligence.Logic.FormationControl.ObstacleAvoiders;
using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.MapControl.Interfaces;
using SwarmIntelligence.Logic.Navigation;
using SwarmIntelligence.Logic.Navigation.Interfaces;
using SwarmIntelligence.Logic.TargetBuffer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Setup
{
    public class SwarmSetup
    {
        private readonly DataHubCommunicator hubCommunication;
        private readonly HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        private readonly TaskCompletionSource<IHost> hostReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IDisposable? initDataSubscription;

        public SwarmSetup(DataHubCommunicator communication)
        {
            hubCommunication = communication ?? throw new ArgumentNullException(nameof(communication), "Communication instance is required in Setup.");
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await hubCommunication.StartAsync();
            RegisterForInitData();
            var host = await hostReady.Task.ConfigureAwait(false);
            await host.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private void RegisterForInitData()
        {
            initDataSubscription = hubCommunication.ReceiveInitData(OnInitDataReceivedAsync);
        }

        private Task OnInitDataReceivedAsync(SwarmSettings settings)
        {
            initDataSubscription?.Dispose();
            initDataSubscription = null;

            if (!hostReady.Task.IsCompleted)
            {
                var host = BuildHost(settings);
                var set = hostReady.TrySetResult(host);
                if (!set)
                {
                    host.Dispose();
                }
            }

            return Task.CompletedTask;
        }

        private IHost BuildHost(SwarmSettings settings)
        {
            // Load settings and subscribe object
            builder.Services.AddSingleton(settings);

            var usbInitData = new InitData()
            {
                CompassOffset = settings.CompassOffset,
                AnchorPositions = [.. settings.AnchorPositions.Select(ap => (ap.X, ap.Y))],
                RequiredSlavesAmount = Convert.ToByte(settings.NumberOfAgents - 1),
                DefaultTimesToMeasure = settings.DefaultTimesToMeasureDwm
            };
            // Use Container to be able to inject record struct
            builder.Services.AddSingleton(new InitDataContainer(usbInitData));

            // Register stores and data providers
            builder.Services.AddSingleton<IAgentStateStore, AgentStateStore>();
            builder.Services.AddSingleton<IObstacleStore, ObstacleStore>();
            builder.Services.AddSingleton<IDataProvider, USBDataProvider>();

            // Register internal logic services
            builder.Services.AddSingleton<IMapUpdateCoordinator, MapUpdateCoordinator>();
            builder.Services.AddSingleton<IMapQueryService, MapQueryService>();
            builder.Services.AddSingleton<IMapWorker, MapWorker>();
            builder.Services.AddSingleton<MapController>();

            builder.Services.AddSingleton<IDriveCommandDispatcher, DriveCommandDispatcher>();
            builder.Services.AddSingleton<IDriveCommandGenerator, DriveCommandGenerator>();
            builder.Services.AddSingleton<IDriveCommandManager, DriveCommandManager>();
            builder.Services.AddSingleton<DriveController>();

            builder.Services.AddSingleton<INavigationAlgorithm, AStarNavigationAlgo>();
            builder.Services.AddSingleton<IObstacleAvoider, BreathFirstObstacleAvoider>();
            builder.Services.AddSingleton<TargetBuffer.TargetBuffer>();

            // Register main controllers
            builder.Services.AddSingleton<Navigator>();
            builder.Services.AddSingleton<FormationController>();
            builder.Services.AddSingleton(new Dictionary<FormationShapes, FormationShape>
            {
                { FormationShapes.Square, new SquareShape() },
                { FormationShapes.Line, new LineShape() }
                // More shapes can be added here
            });

            builder.Services.AddSingleton(hubCommunication);

            // Register the startup orchestrator
            builder.Services.AddHostedService<SwarmSystemInitializer>();

            var host = builder.Build();

            return host;
        }
    }
}
