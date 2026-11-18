using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Errors.Adapters;

namespace TaskFlow.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services
            .AddControllers(options =>
            {
                // FluentValidation is the only validation mechanism; skip MVC's implicit [Required].
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(EnumNamesOnly()))
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                    new BadRequestObjectResult(context.ModelState.ToErrorResponse());
            });

        // The OpenAPI generator reads these options, not MVC's, so enums must be strings here too.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(EnumNamesOnly()));
        services.AddOpenApi();

        return services;
    }

    // Enums travel as names ("InProgress"); numbers are rejected so the contract matches the docs.
    private static JsonStringEnumConverter EnumNamesOnly() => new(namingPolicy: null, allowIntegerValues: false);
}
