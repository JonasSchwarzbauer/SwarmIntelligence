using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;
using SwarmIntelligence.Logic.Communication.DTOs.MapControl;
using SwarmIntelligence.Logic.MapControl;
using Webserver.Interfaces;
using Webserver.Models.AgentRelated;
using Webserver.Models.SystemRelated;

namespace Webserver.Hubs
{
    public class DataHub(ISwarmCache cache, ILogger<DataHub> logger, EventPublisher eventPublisher) : Hub<IDataHub>
    {
        private readonly ISwarmCache _cache = cache;
        private readonly ILogger<DataHub> _logger = logger;
        private readonly EventPublisher _eventPublisher = eventPublisher;

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(exception, "Client disconnected: {ConnectionId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        #region Drive Control Event Stubs

        public async Task OnAgentRegistration(AgentRegistrationDto data)
        {
            _logger.LogInformation("OnAgentRegistration payload: {@Payload}", data);
            _cache.Registrations.Update(data.AgentId.ToString(), new()
            {
                AgentId = data.AgentId,
                RegistrationType = data.RegistrationType,
                RegisteredAt = data.Timestamp
            });
            await _eventPublisher.AgentRegistration(data);
        }

        public async Task OnCommandGenerated(CommandGeneratedDto data)
        {
            _logger.LogInformation("OnCommandGenerated payload: {@Payload}", data);
            _cache.Commands.Update(data.Command.AgentId.ToString(), new()
            {
                AgentId = data.Command.AgentId,
                CurrentCommand = data.Command,
                Phase = CommandPhase.Generated,
                GeneratedAt = data.GeneratedAt,
                AssignedAt = null,
                DispatchedAt = null,
                DispatchLatencyMs = null,
                LastUpdated = DateTime.UtcNow
            });
            await _eventPublisher.CommandGenerated(data);
        }

        public async Task OnCommandAssigned(CommandAssignedDto data)
        {
            _logger.LogInformation("OnCommandAssigned payload: {@Payload}", data);
            var commandState = _cache.Commands.Get(data.AgentId.ToString());

            if (commandState != null)
            {
                _cache.Commands.Update(data.Command.AgentId.ToString(), commandState with
                {
                    Phase = CommandPhase.Assigned,
                    AssignedAt = data.AssignedAt,
                    LastUpdated = DateTime.UtcNow
                });
            }
            await _eventPublisher.CommandAssigned(data);
        }

        public async Task OnCommandDispatched(CommandDispatchedDto data)
        {
            _logger.LogInformation("OnCommandDispatched payload: {@Payload}", data);
            var commandState = _cache.Commands.Get(data.AgentId.ToString());

            if (commandState != null)
            {
                _cache.Commands.Update(data.AgentId.ToString(), commandState with
                {
                    Phase = CommandPhase.Dispatched,
                    DispatchedAt = data.DispatchedAt,
                    DispatchLatencyMs = data.DispatchLatencyMs,
                    LastUpdated = DateTime.UtcNow
                });
            }
            await _eventPublisher.CommandDispatched(data);
        }

        public async Task OnCommandCleared(CommandClearedDto data)
        {
            _logger.LogInformation("OnCommandCleared payload: {@Payload}", data);
            var commandState = _cache.Commands.Get(data.AgentId.ToString());

            if (commandState != null)
            {
                _cache.Commands.Update(data.AgentId.ToString(), commandState with
                {
                    Phase = CommandPhase.Cleared,
                    LastUpdated = DateTime.UtcNow,
                    CurrentCommand = null
                });
            }
            await _eventPublisher.CommandCleared(data);
        }

        public async Task OnCommandError(DriveControlErrorDto data)
        {
            _logger.LogWarning("OnCommandError payload: {@Payload}", data);
            _cache.AgentErrors.Update(data.AgentId.ToString(), new()
            {
                AgentId = data.AgentId,
                Source = data.Source,
                Message = data.Message,
                SourceContext = data.SourceContext,
                ExceptionMessage = data.ExceptionMessage,
                Timestamp = data.Timestamp
            });
            await _eventPublisher.DriveControlError(data);
        }

        public async Task OnManagerStateChanged(ManagerStateDto data)
        {
            _logger.LogInformation("OnManagerStateChanged payload: {@Payload}", data);
            _cache.ManagerState.Update(new()
            {
                IsRunning = data.IsRunning,
                LastUpdated = data.Timestamp
            });
            await _eventPublisher.ManagerState(data);
        }

        #endregion

        #region Map Control Event Stubs

        public async Task OnMapUpdated(MapUpdatedDto data)
        {
            _logger.LogInformation("OnMapUpdated payload: {@Payload}", data);
            _cache.ObstacleGrid.UpdateCell(data.CellX, data.CellY, data.Occupied ? ObstacleType.Physical : ObstacleType.Free);
            await _eventPublisher.MapUpdated(data);
        }

        public async Task OnWorkerStateChanged(WorkerStateDto data)
        {
            _logger.LogInformation("OnWorkerStateChanged payload: {@Payload}", data);
            _cache.MapWorkerState.Update(new()
            {
                CurrentState = data.State,
                LastUpdated = data.Timestamp
            });
            await _eventPublisher.WorkerState(data);
        }

        public async Task OnUsbStarted(UsbStartedDto data)
        {
            _logger.LogInformation("OnUsbStarted payload: {@Payload}", data);
            _cache.UsbStatus.Update(new()
            {
                Success = data.Success,
                PortName = data.PortName,
                BaudRate = data.BaudRate,
                Message = data.Message,
                Timestamp = data.Timestamp
            });
            await _eventPublisher.UsbStarted(data);
        }

        public async Task OnMapError(MapControlErrorDto data)
        {
            _logger.LogWarning("OnMapError payload: {@Payload}", data);
            _cache.MapError.Update(new()
            {
                Source = data.Source,
                Message = data.Message,
                SourceContext = data.SourceContext,
                ExceptionMessage = data.ExceptionMessage,
                Timestamp = data.Timestamp
            });
            await _eventPublisher.MapControlError(data);
        }

        public async Task OnAgentUpdated(AgentDataDto data)
        {
            _logger.LogInformation("OnAgentUpdated payload: {@Payload}", data);
            _cache.Agents.Update(data.AgentId.ToString(), new()
            {
                AgentId = data.AgentId,
                X = data.X,
                Y = data.Y,
                Orientation = data.Orientation,
                Velocity = data.Velocity,
                FrontalDistance = data.FrontalDistance,
                Target = data.Target,
                Flags = data.Flags,
                DwmSuccessRate = data.DwmSuccessRate,
                Timestamp = data.Timestamp,
                DataReceived = data.DataReceived,
            });
            await _eventPublisher.AgentData(data);
        }

        public async Task OnBufferInformation(BufferInformationDto data)
        {
            _logger.LogInformation("OnBufferInformation payload: {@Payload}", data);
            _cache.BufferInformation.Update(new()
            {
                Success = data.Success,
                BufferCount = data.BufferCount,
                BufferCapacity = data.BufferCapacity,
                BufferUsagePercentage = data.BufferUsagePercentage,
                Timestamp = data.Timestamp
            });
            await _eventPublisher.BufferInformation(data);
        }

        #endregion
    }
}
