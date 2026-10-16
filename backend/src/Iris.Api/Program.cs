using Iris.Application.Abstractions.Feeds;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.Articles;
using Iris.Application.UseCases.RssSources;
using Iris.Infrastructure.Feeds;
using Iris.Infrastructure.Persistence;
using Iris.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("IrisDatabase")
                       ?? throw new InvalidOperationException(
                           "La chaîne de connexion IrisDatabase est absente.");

builder.Services.AddDbContext<IrisDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IRssSourceRepository, RssSourceRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddHttpClient<IRssFeedReader, RssFeedReader>();
builder.Services.AddScoped<AddRssSourceUseCase>();
builder.Services.AddScoped<ListRssSourcesUseCase>();
builder.Services.AddScoped<CollectRssSourceUseCase>();
builder.Services.AddScoped<ListArticlesUseCase>();
builder.Services.AddScoped<UpdateRssSourceUseCase>();


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Erreur interne du serveur";
            context.ProblemDetails.Detail =
                "Une erreur inattendue est survenue.";
        }

        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }