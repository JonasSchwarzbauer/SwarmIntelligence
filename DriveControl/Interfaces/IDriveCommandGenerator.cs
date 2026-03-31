using SwarmIntelligence.Logic.DriveControl.EventArgs;
using SwarmIntelligence.Logic.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.DriveControl.Interfaces
{
    public interface IDriveCommandGenerator
    {
        public event EventHandler<CommandGeneratedEventArgs>? CommandGenerated;
        public DriveCommand GenerateCommand(NavResult navResult);
    }
}
