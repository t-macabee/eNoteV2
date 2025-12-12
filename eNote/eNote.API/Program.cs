using eNote.API.Extensions;
using eNote.Infrastructure.Persistence;
using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ENoteContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddAppIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()    
    .AddOpenApiDocumentation()
    .AddControllers();

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