using SwarmIntelligence.Logic.Communication.DTOs.UserInputs;
using SwarmIntelligence.Logic.Communication.USB;
using SwarmIntelligence.Logic.MapControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.MapControl.Interfaces
{
    public interface IMapUpdateCoordinator
    {
        void Update(AgentGeoData vehicleState);
        void UpdateGrid(ObstacleCellDto cell);
        event EventHandler<MapUpdatedEventArgs> MapUpdated;
        event EventHandler<AgentDataUpdatedEventArgs> AgentStateUpdated;
    }
}
