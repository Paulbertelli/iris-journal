using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public sealed class AddRssSourceUseCase(IRssSourceRepository rssSourceRepository)
{
    public async Task<bool> ExecuteAsync(
        string name,
        string feedUrl,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        var source = new RssSource(name, feedUrl, countryCode);

        var alreadyExists = await rssSourceRepository.ExistsByFeedUrlAsync(
            source.FeedUrl,
            cancellationToken);

        if (alreadyExists)
            return false;

        await rssSourceRepository.AddAsync(source, cancellationToken);
        return true;
    }
}