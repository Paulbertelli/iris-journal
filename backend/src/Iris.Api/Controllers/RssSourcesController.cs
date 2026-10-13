using Iris.Api.Contracts.RssSources;
using Iris.Application.UseCases.RssSources;
using Microsoft.AspNetCore.Mvc;

namespace Iris.Api.Controllers;

[ApiController]
[Route("api/rss-sources")]
public sealed class RssSourcesController(
    ListRssSourcesUseCase listRssSourcesUseCase,
    AddRssSourceUseCase addRssSourceUseCase
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var sources = await listRssSourcesUseCase.ExecuteAsync(cancellationToken);

        var response = sources.Select(source => new RssSourceResponse
        {
            Id = source.Id,
            Name = source.Name,
            FeedUrl = source.FeedUrl,
            CountryCode = source.CountryCode,
            IsEnabled = source.IsEnabled
        }).ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRssSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await addRssSourceUseCase.ExecuteAsync(
                request.Name,
                request.FeedUrl,
                request.CountryCode,
                cancellationToken);

            if (id is null)
                return Conflict(new ProblemDetails
                {
                    Title = "Source RSS déjà enregistrée",
                    Detail = "Une source utilise déjà cette URL.",
                    Status = StatusCodes.Status409Conflict
                });

            return StatusCode(StatusCodes.Status201Created, new { id });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Données invalides",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}