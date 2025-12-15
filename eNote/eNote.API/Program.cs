using eNote.API.Extensions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<eNote.Infrastructure.Data.Context.ENoteContext>(options =>
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.SeedDevelopmentDataAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
