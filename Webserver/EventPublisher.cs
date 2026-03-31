using Microsoft.AspNetCore.SignalR;
using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;
using SwarmIntelligence.Logic.Communication.DTOs.MapControl;
using Webserver.Hubs;
using Webserver.Interfaces;

namespace Webserver
{
    public class EventPublisher(IHubContext<UiHub, IUiHub> hub)
    {
        private readonly IHubContext<UiHub, IUiHub> _hub = hub;

        public async Task AgentRegistration(AgentRegistrationDto data) => await _hub.Clients.Groups([UiTopics.AgentRegistration]).AgentRegistration(data);
        public async Task CommandGenerated(CommandGeneratedDto data) => await _hub.Clients.Groups([UiTopics.CommandGenerated]).CommandGenerated(data);
        public async Task CommandAssigned(CommandAssignedDto data) => await _hub.Clients.Groups([UiTopics.CommandAssigned]).CommandAssigned(data);
        public async Task CommandDispatched(CommandDispatchedDto data) => await _hub.Clients.Groups([UiTopics.CommandDispatched]).CommandDispatched(data);
        public async Task CommandCleared(CommandClearedDto data) => await _hub.Clients.Groups([UiTopics.CommandCleared]).CommandCleared(data);
        public async Task DriveControlError(DriveControlErrorDto data) => await _hub.Clients.Groups([UiTopics.DriveControlError]).DriveControlError(data);
        public async Task ManagerState(ManagerStateDto data) => await _hub.Clients.Groups([UiTopics.ManagerState]).ManagerState(data);

        public async Task MapUpdated(MapUpdatedDto data) => await _hub.Clients.Groups([UiTopics.MapUpdated, UiTopics.AwarenessMap]).MapUpdated(data);
        public async Task WorkerState(WorkerStateDto data) => await _hub.Clients.Groups([UiTopics.WorkerState]).WorkerState(data);          
        public async Task UsbStarted(UsbStartedDto data) => await _hub.Clients.Groups([UiTopics.UsbStarted]).UsbStarted(data);
        public async Task MapControlError(MapControlErrorDto data) => await _hub.Clients.Groups([UiTopics.MapControlError]).MapControlError(data);
        public async Task AgentData(AgentDataDto data) => await _hub.Clients.Groups([UiTopics.AgentData, UiTopics.AwarenessMap, UiTopics.FleetManagement]).AgentData(data);
        public async Task BufferInformation(BufferInformationDto data) => await _hub.Clients.Groups([UiTopics.BufferInformation]).BufferInformation(data);
    }
}