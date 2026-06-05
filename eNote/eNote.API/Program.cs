using eNote.API.Extensions;
using eNote.API.Filters;
using eNote.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

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
    .AddApplicationServices();

builder.Services.AddScoped<HttpResponseExceptionFilter>();

builder.Services
    .AddControllers(options => options.Filters.AddService<HttpResponseExceptionFilter>())
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMapsterMappings();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApiPatcher();
    app.MapOpenApi();
    app.MapScalarApiReference();

    await app.SeedDevelopmentData();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();