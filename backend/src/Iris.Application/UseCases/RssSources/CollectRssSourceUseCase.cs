using Iris.Application.Abstractions.Feeds;
using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public sealed class CollectRssSourceUseCase(
    IRssSourceRepository rssSourceRepository,
    IRssFeedReader feedReader,
    IArticleRepository articleRepository)
{
    public async Task<CollectRssSourceResult> ExecuteAsync(
        Guid rssSourceId,
        CancellationToken cancellationToken = default)
    {
        var source = await rssSourceRepository.GetByIdAsync(
            rssSourceId, cancellationToken);

        if (source is null)
            return new(CollectRssSourceStatus.NotFound);

        if (!source.IsEnabled)
            return new(CollectRssSourceStatus.Disabled);

        var items = await feedReader.ReadAsync(
            source.FeedUrl, 
            cancellationToken);

        var counter = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var article = new Article(
                source.Id,
                item.Title,
                item.ArticleUrl,
                item.PublishedAt,
                item.ImageUrl);

            if (await articleRepository.ExistsByArticleUrlAsync(
                    article.ArticleUrl, cancellationToken))
                continue;

            await articleRepository.AddAsync(article, cancellationToken);
            
            counter++;
        }

        return new(CollectRssSourceStatus.Collected, counter);
    }
}