using System.ComponentModel.DataAnnotations;
using Iris.Api.Contracts.Articles;
using Iris.Application.UseCases.Articles;
using Microsoft.AspNetCore.Mvc;

namespace Iris.Api.Controllers;

[ApiController]
[Route("api/articles")]
public sealed class ArticlesController(
    ListArticlesUseCase listArticlesUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLatest(
        CancellationToken cancellationToken,
        [FromQuery, Range(1, 100)] int limit = 50,
        [FromQuery] Guid? sourceId = null)
    {
        var articles = await listArticlesUseCase.ExecuteAsync(
            limit, cancellationToken, sourceId);

        var response = articles.Select(article => new ArticleResponse
        {
            Id = article.Id,
            RssSourceId = article.RssSourceId,
            Title = article.Title,
            ArticleUrl = article.ArticleUrl,
            PublishedAt = article.PublishedAt,
            CollectedAt = article.CollectedAt,
            SourceName = article.SourceName,
            SourceLogoUrl = article.SourceLogoUrl,
            ImageUrl = article.ImageUrl
        }).ToList();

        return Ok(response);
    }
}