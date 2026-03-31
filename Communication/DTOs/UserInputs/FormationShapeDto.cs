using SwarmIntelligence.Logic.FormationControl.Factories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwarmIntelligence.Logic.Communication.DTOs.UserInputs
{
    public record class FormationShapeDto
    {
        public required FormationShapes Shape { get; init; }
        public required float Size { get; init; }
    }
}