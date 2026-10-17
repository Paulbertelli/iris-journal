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
    UpdateRssSourceUseCase updateRssSourceUseCase,
    DeleteRssSourceUseCase deleteRssSourceUseCase,
    IHostEnvironment environment
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
    
    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(
        [FromBody] List<BatchRssSourceRequest?> requests,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (requests.Count is < 1 or > 100)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Liste invalide",
                detail: "La liste doit contenir entre 1 et 100 sources.");
        }

        var results = new List<object>();

        for (var index = 0; index < requests.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = requests[index];

            if (request is null)
            {
                results.Add(new
                {
                    index,
                    status = "invalid",
                    error = "La source ne peut pas être null."
                });

                continue;
            }

            try
            {
                var id = await addRssSourceUseCase.ExecuteAsync(
                    request.Name ?? string.Empty,
                    request.FeedUrl ?? string.Empty,
                    request.CountryCode ?? string.Empty,
                    description: request.Description,
                    logoUrl: request.LogoUrl,
                    cancellationToken: cancellationToken);

                results.Add(new
                {
                    index,
                    request.Name,
                    status = id.HasValue ? "created" : "alreadyExists",
                    id
                });
            }
            catch (ArgumentException exception)
            {
                results.Add(new
                {
                    index,
                    request.Name,
                    status = "invalid",
                    error = exception.Message
                });
            }
        }

        return Ok(new { results });
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
            var result = await collectRssSourceUseCase.ExecuteAsync(
                id, cancellationToken);

            return result.Status switch
            {
                CollectRssSourceStatus.Collected =>
                    Ok(new { addedCount = result.AddedCount }),

                CollectRssSourceStatus.NotFound =>
                    NotFound(),

                CollectRssSourceStatus.Disabled =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Source RSS désactivée",
                        detail: "Réactive cette source avant de lancer une collecte."),

                _ => throw new InvalidOperationException(
                    "Résultat de collecte inconnu.")
            };
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
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await deleteRssSourceUseCase.ExecuteAsync(
            id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}