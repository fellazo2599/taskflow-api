using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Tasks.Interfaces;
using TaskFlow.Infrastructure.HealthChecks;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Interceptors;
using TaskFlow.Infrastructure.Persistence.Repositories;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseNpgsql(configuration.GetConnectionString(DatabaseSettings.ConnectionStringName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<ITaskRepository, TaskRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(HealthCheckNames.Database, tags: [HealthCheckTags.Ready]);

        return services;
    }
}
