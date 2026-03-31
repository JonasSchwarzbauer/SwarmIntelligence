using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.Communication.DTOs.DriveControl;
using SwarmIntelligence.Logic.Communication.DTOs.MapControl;
using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.Setup;

namespace Webserver.Interfaces
{
    /// <summary>
    /// Client-side interface for SignalR DataHub.
    /// Defines methods that the server can invoke on connected clients.
    /// </summary>
    public interface IDataHub
    {
        // methods to send data to console app
        // Init Data
        Task SendInitData(SwarmSettings settings);

        // User inputs
        Task SendFormationShape(FormationShapeDto shapeSettings);
        Task SendFormationPath(FormationPathDto pathArgs);
        Task SendVehicleTargets(VehicleTargetsDto targetArgs);
        Task SendVirtualObstacles(VirtualObstaclesDto targetArgs);

        Task SendManagerStateChange();
        Task SendWorkerStateChange();
    }
}
