using eNote.API.Extensions;
using Serilog;
using System.Text.Json.Serialization;

eNote.API.Extensions.ConfigurationExtensions.LoadDotEnv();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseApplicationLogging();
builder.Configuration.ValidateRequiredSettings();

builder.Services
    .AddApplicationDatabase(builder.Configuration)
    .AddApplicationIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()
    .AddApplicationMessaging(builder.Configuration)
    .AddApplicationCors(builder.Configuration, builder.Environment)
    .AddApplicationRateLimiting()
    .AddResponseCompression(opts => opts.EnableForHttps = true)
    .AddMapsterMappings()
    .AddApplicationValidation()
    .AddScalarDocumentation();

builder.Services
    .AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}
app.UseStaticFiles();
app.UseCors(CorsExtensions.PolicyName);
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    await app.MigrateAsync();
    app.MapScalarDocumentation();
    await app.SeedDevelopmentData();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
