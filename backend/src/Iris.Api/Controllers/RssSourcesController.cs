using Iris.Api.Contracts.RssSources;
using Iris.Application.UseCases.RssSources;
using Microsoft.AspNetCore.Mvc;

namespace Iris.Api.Controllers;

public sealed class RssSourcesController(AddRssSourceUseCase addRssSourceUseCase) : ControllerBase
{
    [HttpPost("api/rss-sources")]
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