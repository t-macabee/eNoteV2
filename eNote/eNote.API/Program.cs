using eNote.API.Extensions;
using eNote.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

eNote.API.Extensions.ConfigurationExtensions.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ValidateRequiredSettings();

builder.Services.AddDbContext<ENoteContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("eNote.Infrastructure")));

builder.Services
    .AddApplicationIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()
    .AddApplicationCors(builder.Configuration, builder.Environment);

builder.Services
    .AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMapsterMappings();
builder.Services.AddScalarDocumentation();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseCors(CorsExtensions.PolicyName);
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();
        await context.Database.MigrateAsync();
    }

    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
    app.MapOpenApi();
    app.MapScalarApiReference();

    await app.SeedDevelopmentData();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
