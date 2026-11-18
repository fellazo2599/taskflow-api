using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.Property(task => task.Title)
            .HasMaxLength(TaskItemConstraints.TitleMaxLength)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(TaskItemConstraints.DescriptionMaxLength);

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        builder.Property(task => task.Assignee)
            .HasMaxLength(TaskItemConstraints.AssigneeMaxLength)
            .IsRequired();

        // Supports the status filter combined with the default ordering by due date.
        builder.HasIndex(task => new { task.Status, task.DueDate });

        builder.HasQueryFilter(task => !task.IsDeleted);
    }
}
