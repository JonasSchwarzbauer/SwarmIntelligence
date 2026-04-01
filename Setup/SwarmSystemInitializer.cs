using Microsoft.Extensions.Hosting;
using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.DriveControl;
using SwarmIntelligence.Logic.FormationControl;
using SwarmIntelligence.Logic.FormationControl.Factories;
using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.Navigation;
using SwarmIntelligence.Logic.TargetBuffer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Setup
{
    public class SwarmSystemInitializer : IHostedService
    {
        private readonly MapController mapController;
        private readonly DriveController driveController;
        private readonly Navigator navigator;
        private readonly FormationController formationController;
        private readonly DataHubCommunicator hubCommunication;
        private readonly TargetBuffer.TargetBuffer targetBuffer;
        private readonly InitData initData;

        public SwarmSystemInitializer(MapController mC, DriveController dC, Navigator nG, FormationController fC, DataHubCommunicator hubCom, InitDataContainer initDataC, TargetBuffer.TargetBuffer tB)
        {
            mapController = mC;
            driveController = dC;
            navigator = nG;
            formationController = fC;
            hubCommunication = hubCom;
            targetBuffer = tB;
            initData = initDataC.Data;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Perform Event subscriptions
            WireSendingEvents();
            WireReceivingEvents();

            // Start worker tasks
            mapController.Start();
            driveController.Start();
            navigator.StartNavigatorTask(cancellationToken);
            formationController.StartFormationControllerTask(cancellationToken);

            // Send Init Data to LeadESP via USB
            USBInitDataSender sender = new();
            sender.SendStructViaUSB(initData);

            return Task.CompletedTask;
        }

        private void WireSendingEvents()
        {
            // Map Controller events
            mapController.USBStarted += async (s, e) =>
            {
                await hubCommunication.SendUsbStartedAsync(e);
            };

            mapController.WorkerStateChanged += async (s, e) =>
            {
                await hubCommunication.SendWorkerStateChangedAsync(e);
            };

            mapController.BufferInformationEvent += async (s, e) =>
            {
                await hubCommunication.SendBufferInformationAsync(e);
            };

            mapController.MapUpdated += async (s, e) =>
            {
                await hubCommunication.SendMapUpdatedAsync(e);
            };

            mapController.AgentStateUpdated += async (s, e) =>
            {
                await hubCommunication.SendAgentDataUpdatedAsync(e);
            };

            mapController.ErrorOccurred += async (s, e) =>
            {
                await hubCommunication.SendMapErrorAsync(e);
            };

            // Drive Controller events
            driveController.AgentRegistrationChanged += async (s, e) =>
            {
                await hubCommunication.SendAgentRegistrationAsync(e);
            };

            driveController.CommandAssigned += async (s, e) =>
            {
                await hubCommunication.SendCommandAssignedAsync(e);
            };

            driveController.CommandCleared += async (s, e) =>
            {
                await hubCommunication.SendCommandClearedAsync(e);
            };

            driveController.CommandDispatched += async (s, e) =>
            {
                await hubCommunication.SendCommandDispatchedAsync(e);
            };

            driveController.CommandGenerated += async (s, e) =>
            {
                await hubCommunication.SendCommandGeneratedAsync(e);
            };

            driveController.ErrorOccurred += async (s, e) =>
            {
                await hubCommunication.SendCommandErrorAsync(e);
            };

            driveController.ManagerStateChanged += async (s, e) =>
            {
                await hubCommunication.SendManagerStateAsync(e);
            };
        }

        private void WireReceivingEvents()
        {
            hubCommunication.ReceiveFormationShape(async shapeSettings => formationController.TrySetFormationShape(shapeSettings.Shape, shapeSettings.Size));

            hubCommunication.ReceiveFormationPath(async pathArgs =>
            {
                if (pathArgs.ModeChanged)
                    targetBuffer.EmptyAndDeactivateBuffer();

                formationController.TrySetFormationPath([.. pathArgs.Waypoints]);
            });

            hubCommunication.ReceiveVehicleTargets(async targetArgs =>
            {
                if (targetArgs.ModeChanged)
                {
                    formationController.TrySetFormationPath([]);
                    targetBuffer.EnqueueTargets(targetArgs.AgentId, [.. targetArgs.Waypoints], true); // always override when mode changed to ensure buffer is updated with new path immediately
                }
                else
                {
                    targetBuffer.EnqueueTargets(targetArgs.AgentId, [.. targetArgs.Waypoints], targetArgs.OverRide);
                }
                    
            });

            hubCommunication.ReceiveVirtualObstacles(async obstacleArgs =>
            {
                foreach(var virtualObstacle in obstacleArgs.Obstacles)
                {
                    mapController.UpdateGrid(virtualObstacle);
                }
            });

            hubCommunication.ReceiveManagerStateChange(async () =>
            {
                if (driveController.IsRunning)
                    {
                    driveController.Stop();
                    // Console.WriteLine("Manager stop");
                }
                else
                {
                    driveController.Start();
                    // Console.WriteLine("Manager start");
                }
            });

            hubCommunication.ReceiveWorkerStateChange(async () =>
            {
                if (mapController.IsRunning)
                {
                    await mapController.Stop();
                    // Console.WriteLine("Worker stop");
                }
                else
                {
                    await mapController.Start();
                    // Console.WriteLine("Worker start");
                }
            });

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
