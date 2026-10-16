using Iris.Api.Contracts.RssSources;
using Iris.Application.Abstractions.Feeds;
using Iris.Application.UseCases.RssSources;
using Microsoft.AspNetCore.Mvc;

namespace Iris.Api.Controllers;

[ApiController]
[Route("api/rss-sources")]
public sealed class RssSourcesController(
    ListRssSourcesUseCase listRssSourcesUseCase,
    AddRssSourceUseCase addRssSourceUseCase,
    CollectRssSourceUseCase collectRssSourceUseCase,
    UpdateRssSourceUseCase updateRssSourceUseCase
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
            IsEnabled = source.IsEnabled,
            Description = source.Description,
            LogoUrl = source.LogoUrl,
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
                description: request.Description,
                logoUrl: request.LogoUrl,
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

    [HttpPost("{id:guid}/collect")]
    public async Task<IActionResult> Collect(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await collectRssSourceUseCase.ExecuteAsync(id, cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(new { addedCount = result.Value });
        }
        catch (FeedReadException exception)
        {
            var status = exception.IsTimeout ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway;

            return Problem(statusCode: status, title: "Échec de la collecte RSS", detail: exception.Message);
        }
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRssSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await updateRssSourceUseCase.ExecuteAsync(
                id,
                request.Name,
                request.FeedUrl,
                request.CountryCode,
                request.IsEnabled!.Value,
                request.Description,
                request.LogoUrl,
                cancellationToken);

            return result switch
            {
                UpdateRssSourceResult.Updated => NoContent(),
                UpdateRssSourceResult.NotFound => NotFound(),
                UpdateRssSourceResult.DuplicateFeedUrl => Conflict(
                    new ProblemDetails
                    {
                        Title = "Source RSS déjà enregistrée",
                        Detail = "Une source utilise déjà cette URL.",
                        Status = StatusCodes.Status409Conflict
                    }),
                _ => throw new InvalidOperationException(
                    "Résultat de modification inconnu.")
            };
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