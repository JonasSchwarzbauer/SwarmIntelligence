namespace SwarmIntelligence.Logic.DriveControl
{
    public record AgentSlotState(
        byte AgentId,
        DriveCommand? CurrentCommand,
        DateTime? CommandAssignedAt
    );
}