namespace TaskManager.Domain.Tasks;

// Shared by the validators and the EF configuration so both stay in sync.
public static class TaskItemConstraints
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int AssigneeMaxLength = 100;
}
