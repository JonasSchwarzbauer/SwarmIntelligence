namespace SwarmIntelligence.Logic.Communication.DTOs.UserInputs
{
    public record class VirtualObstaclesDto
    {
        public required IReadOnlyCollection<ObstacleCellDto> Obstacles { get; init; }
    }
}
