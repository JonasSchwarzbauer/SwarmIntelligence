using SwarmIntelligence.Logic.DriveControl.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.Interfaces
{
    public interface IDriveCommandDispatcher
    {
        public event EventHandler<CommandDispatchedEventArgs>? CommandDispatched;
        public Task DispatchCommand(DriveCommand command);
    }
}
