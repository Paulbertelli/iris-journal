using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.RssSources;
using Iris.Infrastructure.Persistence;
using Iris.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("IrisDatabase")
                       ?? throw new InvalidOperationException(
                           "La chaîne de connexion IrisDatabase est absente.");

builder.Services.AddDbContext<IrisDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IRssSourceRepository, RssSourceRepository>();
builder.Services.AddScoped<AddRssSourceUseCase>();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();