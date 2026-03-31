using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl
{
    [Flags]
    public enum DriveFlags
    {
        OverrideWaypoints = 1,
        OverrideMinDistanceStop = 2
    }
}
