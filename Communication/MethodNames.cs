using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication
{
    public enum MethodNames
    {
        // Drive Control Methods
        OnAgentRegistration,
        OnCommandAssigned,
        OnCommandCleared,
        OnCommandDispatched,
        OnCommandGenerated,
        OnCommandError,
        OnManagerStateChanged,

        // Map Control Methods
        OnMapUpdated,
        OnWorkerStateChanged,
        OnUsbStarted,
        OnMapError,
        OnAgentUpdated,
        OnBufferInformation,

        // Setup
        SendInitData,

        // User Inputs
        SendFormationPath,
        SendFormationShape,
        SendVehicleTargets,
        SendVirtualObstacles,
        SendWorkerStateChange,
        SendManagerStateChange
    }
}