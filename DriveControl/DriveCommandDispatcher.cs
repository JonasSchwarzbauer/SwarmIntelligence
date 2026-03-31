using SwarmIntelligence.Logic.Communication;
using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.DriveControl.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl
{
    internal class DriveCommandDispatcher : IDriveCommandDispatcher
    {
        private readonly USBDriveCommandSender commandSender = new();
        public event EventHandler<CommandDispatchedEventArgs>? CommandDispatched;

        public Task DispatchCommand(DriveCommand command)
        {
            commandSender.SendStructViaUSB(command);

            var handler = CommandDispatched;
            handler?.Invoke(this, new CommandDispatchedEventArgs(command));

            return Task.CompletedTask;
        }
    }
}
