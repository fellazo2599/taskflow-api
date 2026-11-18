using TaskFlow.Api.Constants;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.Middlewares;
using TaskFlow.Api.Settings;
using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Extensions;

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
app.UseApiRequestLogging();
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
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TaskFlow API v1"));
}

app.UseCors(CorsSettings.PolicyName);
app.UseRateLimiter();
app.MapControllers();
app.MapApiHealthChecks();

var databaseSettings = app.Configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>();
if (databaseSettings?.ApplyMigrationsOnStartup == true)
{
    await app.Services.ApplyMigrationsAsync();
}

app.Run();
