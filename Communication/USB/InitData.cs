using SwarmIntelligence.Logic.Communication.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmIntelligence.Logic.Communication
{
    public record struct InitData : IUSBSendable
    {
        public float CompassOffset {  get; init; }

        // 3 Anchor-Positions (first index) and 2 floats per Position
        public (float X, float Y)[] AnchorPositions { get; init; }
        public byte DefaultTimesToMeasure { get; init; }
        public byte RequiredSlavesAmount { get; init; }
    }
}
