using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public sealed class UpdateRssSourceUseCase(
    IRssSourceRepository rssSourceRepository)
{
    public async Task<UpdateRssSourceResult> ExecuteAsync(
        Guid id,
        string name,
        string feedUrl,
        string countryCode,
        bool isEnabled,
        string? description,
        string? logoUrl,
        CancellationToken cancellationToken = default)
    {
        var source = await rssSourceRepository.GetByIdAsync(id, cancellationToken);

        if (source is null)
            return UpdateRssSourceResult.NotFound;

        // Valide et normalise avant de modifier l’entité suivie par EF.
        var validated = new RssSource(
            name,
            feedUrl,
            countryCode,
            isEnabled,
            description,
            logoUrl);

        if (validated.FeedUrl != source.FeedUrl &&
            await rssSourceRepository.ExistsByFeedUrlAsync(
                validated.FeedUrl, cancellationToken))
        {
            return UpdateRssSourceResult.DuplicateFeedUrl;
        }

        source.Update(
            validated.Name,
            validated.FeedUrl,
            validated.CountryCode,
            validated.IsEnabled,
            validated.Description,
            validated.LogoUrl);

        await rssSourceRepository.SaveChangesAsync(cancellationToken);

        return UpdateRssSourceResult.Updated;
    }
}