using Microsoft.Extensions.Logging;
using SwarmIntelligence.Logic.Communication.DTOs;
using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.FormationControl.Interfaces;
using SwarmIntelligence.Logic.MapControl;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using SwarmIntelligence.Logic.Navigation;
using SwarmIntelligence.Logic.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication
{
    public class DataHubCommunicator : WebserverCommunicationHandler
    {
        #region Constructors

        public DataHubCommunicator() : base() { }
        public DataHubCommunicator(ILogger<WebserverCommunicationHandler> logger) : base(logger) { }
        public DataHubCommunicator(ILogger<WebserverCommunicationHandler> logger, string hubUrl) : base(logger, hubUrl) { }

        #endregion

        #region Drive Control Event Sending

        public async Task SendAgentRegistrationAsync(AgentRegistrationEventArgs args)
        {
            await SendDataAsync(MethodNames.OnAgentRegistration, args.ToDto());
        }

        public async Task SendCommandAssignedAsync(CommandAssignedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnCommandAssigned, args.ToDto());
        }

        public async Task SendCommandClearedAsync(CommandClearedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnCommandCleared, args.ToDto());
        }

        public async Task SendCommandDispatchedAsync(CommandDispatchedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnCommandDispatched, args.ToDto());
        }

        public async Task SendCommandGeneratedAsync(CommandGeneratedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnCommandGenerated, args.ToDto());
        }

        public async Task SendCommandErrorAsync(CommandErrorEventArgs args)
        {
            await SendDataAsync(MethodNames.OnCommandError, args.ToDto());
        }

        public async Task SendManagerStateAsync(ManagerStateChangedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnManagerStateChanged, args.ToDto());
        }

        #endregion

        #region Map Control Event Sending

        public async Task SendMapUpdatedAsync(MapUpdatedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnMapUpdated, args.ToDto());
        }

        public async Task SendAgentDataUpdatedAsync(AgentDataUpdatedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnAgentUpdated, args.ToDto());
        }

        public async Task SendWorkerStateChangedAsync(WorkerStateChangedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnWorkerStateChanged, args.ToDto());
        }

        public async Task SendUsbStartedAsync(UsbStartedEventArgs args)
        {
            await SendDataAsync(MethodNames.OnUsbStarted, args.ToDto());
        }

        public async Task SendMapErrorAsync(MapErrorEventArgs args)
        {
            await SendDataAsync(MethodNames.OnMapError, args.ToDto());
        }

        public async Task SendBufferInformationAsync(BufferInformationEventArgs args)
        {
            await SendDataAsync(MethodNames.OnBufferInformation, args.ToDto());
        }

        #endregion

        #region User Inputs Receiving

        public IDisposable ReceiveFormationShape(Func<FormationShapeDto, Task> handler)
        {
            return ReceiveData(MethodNames.SendFormationShape, handler);
        }

        public IDisposable ReceiveFormationPath(Func<FormationPathDto, Task> handler)
        {
            return ReceiveData(MethodNames.SendFormationPath, handler);
        }


        public IDisposable ReceiveVehicleTargets(Func<VehicleTargetsDto, Task> handler)
        {
            return ReceiveData(MethodNames.SendVehicleTargets, handler);
        }

        #endregion

        #region Setup

        public IDisposable ReceiveInitData(Func<SwarmSettings, Task> handler)
        {
            return ReceiveData(MethodNames.SendInitData, handler);
        }

        public IDisposable ReceiveVirtualObstacles(Func<VirtualObstaclesDto, Task> handler)
        {
            return ReceiveData(MethodNames.SendVirtualObstacles, handler);
        }

        public IDisposable ReceiveManagerStateChange(Func<Task> handler)
        {
            return ReceiveData(MethodNames.SendManagerStateChange, handler);
        }

        public IDisposable ReceiveWorkerStateChange(Func<Task> handler)
        {
            return ReceiveData(MethodNames.SendWorkerStateChange, handler);
        }

        #endregion
    }
}
