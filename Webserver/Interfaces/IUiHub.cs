using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;
using SwarmIntelligence.Logic.Communication.DTOs.MapControl;
using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;

namespace Webserver.Interfaces
{
    public interface IUiHub
    {
        #region Event Broadcasts

        Task AgentRegistration(AgentRegistrationDto data);
        Task CommandGenerated(CommandGeneratedDto data);
        Task CommandAssigned(CommandAssignedDto data);
        Task CommandDispatched(CommandDispatchedDto data);
        Task CommandCleared(CommandClearedDto data);
        Task DriveControlError(DriveControlErrorDto data);
        Task ManagerState(ManagerStateDto data);

        Task MapUpdated(MapUpdatedDto data);
        Task WorkerState(WorkerStateDto data);
        Task UsbStarted(UsbStartedDto data);
        Task MapControlError(MapControlErrorDto data);
        Task AgentData(AgentDataDto data);
        Task BufferInformation(BufferInformationDto data);

        Task VirtualObstaclesUpdated(VirtualObstaclesDto data);

        #endregion
    }
}
