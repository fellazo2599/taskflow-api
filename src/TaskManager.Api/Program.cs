using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

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
