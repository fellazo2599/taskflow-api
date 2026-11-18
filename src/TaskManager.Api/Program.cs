using Serilog;
using TaskManager.Api.Constants;
using TaskManager.Api.Extensions;
using TaskManager.Api.Middlewares;
using TaskManager.Api.Settings;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = RequestLimits.MaxBodySizeBytes;
});

builder.Services
    .AddApiLogging(builder.Configuration, builder.Environment)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi()
    .AddApiSecurity(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TaskManager API v1"));
}

app.UseCors(CorsSettings.PolicyName);
app.UseRateLimiter();
app.MapControllers();

var databaseSettings = app.Configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>();
if (databaseSettings?.ApplyMigrationsOnStartup == true)
{
    await app.Services.ApplyMigrationsAsync();
}

app.Run();
