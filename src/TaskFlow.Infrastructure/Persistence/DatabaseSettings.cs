namespace TaskFlow.Infrastructure.Persistence;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "Default";

    public bool ApplyMigrationsOnStartup { get; init; }
}
