using System.Text.Json.Serialization;
using IndustrialOps.Api.ErrorHandling;
using IndustrialOps.Application;
using IndustrialOps.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services (dependency injection container) ----------

builder.Services
    .AddControllers(options =>
    {
        // Input validation is done by FluentValidation in the Application layer.
        // This stops ASP.NET from adding its own implicit [Required] checks, so there is one source of truth.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // Enums travel as text ("InProgress") instead of numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Malformed JSON returns a generic message instead of internal type names.
        options.AllowInputFormatterExceptionMessages = false;
    });

// Global error handling: every error response follows RFC 9457 (application/problem+json).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}");
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ---------- HTTP pipeline (middlewares, in order) ----------

var app = builder.Build();

app.UseExceptionHandler(); // catches exceptions from everything registered after it
app.UseStatusCodePages();  // empty error responses (e.g. unknown route 404) also become Problem Details

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar  (interactive UI to try the API)
}

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
}

app.MapControllers();

app.Run();

// Makes the auto-generated Program class visible to integration tests (WebApplicationFactory<Program>).
public partial class Program;
