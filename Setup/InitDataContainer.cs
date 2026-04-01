using SwarmIntelligence.Logic.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Setup
{
    public class InitDataContainer(InitData data)
    {
        public InitData Data { get; init; } = data;
    }
}
