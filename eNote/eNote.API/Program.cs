using eNote.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using eNote.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ENoteContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("eNote.Infrastructure")
    ));

builder.Services
    .AddApplicationIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()
    .AddSwaggerDocumentation();

builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMapsterMappings();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();

    await app.SeedDevelopmentData();   
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
