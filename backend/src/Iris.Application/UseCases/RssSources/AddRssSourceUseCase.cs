using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public sealed class AddRssSourceUseCase(IRssSourceRepository rssSourceRepository)
{
    public async Task<Guid?> ExecuteAsync(
        string name,
        string feedUrl,
        string countryCode,
        string? description = null,
        string? logoUrl = null,
        CancellationToken cancellationToken = default)
    {
        var source = new RssSource(name, feedUrl, countryCode, description: description, logoUrl: logoUrl);

        var alreadyExists = await rssSourceRepository.ExistsByFeedUrlAsync(
            source.FeedUrl,
            cancellationToken);

        if (alreadyExists)
            return null;    

        await rssSourceRepository.AddAsync(source, cancellationToken);
        return source.Id;
    }
}