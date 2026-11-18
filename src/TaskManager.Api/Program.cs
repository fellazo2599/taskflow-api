using TaskManager.Api.Extensions;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

var databaseSettings = app.Configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>();
if (databaseSettings?.ApplyMigrationsOnStartup == true)
{
    await app.Services.ApplyMigrationsAsync();
}

app.Run();
