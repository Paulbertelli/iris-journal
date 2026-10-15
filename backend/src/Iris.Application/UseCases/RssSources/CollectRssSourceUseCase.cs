using Iris.Application.Abstractions.Feeds;
using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public class CollectRssSourceUseCase(
    IRssSourceRepository rssSourceRepository, 
    IRssFeedReader feedReader, 
    IArticleRepository articleRepository)
{
    public async Task<int?> ExecuteAsync(Guid rssSourceId, CancellationToken cancellationToken = default)
    {
        var rssSource = await rssSourceRepository.GetByIdAsync(rssSourceId, cancellationToken);
        
        if (rssSource is null)
            return null;
        
        var items = await feedReader.ReadAsync(rssSource.FeedUrl, cancellationToken);
            
        var counter = 0;
        
        foreach (var item in items)
        {
            var article = new Article(
                rssSource.Id, 
                item.Title, 
                item.ArticleUrl, 
                item.PublishedAt);
                
                if (await articleRepository.ExistsByArticleUrlAsync(article.ArticleUrl, cancellationToken)) 
                    continue;
                
                await articleRepository.AddAsync(article, cancellationToken);
                counter++;
        }

        return counter;
    }
}