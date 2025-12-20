using eNote.API.Extensions;
using eNote.Infrastructure.Data.Seed;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using eNote.Infrastructure.Data.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ENoteContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("eNote.Infrastructure")
    ));

builder.Services
    .AddAppIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()
    .AddOpenApiDocumentation()
    .AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMapster();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;
    await IdentitySeed.SeedAsync(services);

    var context = services.GetRequiredService<ENoteContext>();
    await DevelopmentDataSeed.SeedAsync(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
