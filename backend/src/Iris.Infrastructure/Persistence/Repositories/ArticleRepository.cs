using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.Articles;
using Iris.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Iris.Infrastructure.Persistence.Repositories;

public sealed class ArticleRepository(IrisDbContext dbContext) : IArticleRepository
{
    
    public Task<bool> ExistsByArticleUrlAsync(string articleUrl, CancellationToken cancellationToken)
    {
        return dbContext.Articles.AnyAsync(article => article.ArticleUrl == articleUrl, cancellationToken);
    }
    public async Task AddAsync(Article article, CancellationToken cancellationToken)
    {
        dbContext.Articles.Add(article); 
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleListItem>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var query =
            from article in dbContext.Articles.AsNoTracking()
            join source in dbContext.RssSources.AsNoTracking()
                on article.RssSourceId equals source.Id
            orderby article.CollectedAt descending, article.Id descending
            select new ArticleListItem
            {
                Id = article.Id,
                RssSourceId = source.Id,
                SourceName = source.Name,
                SourceLogoUrl = source.LogoUrl,
                Title = article.Title,
                ArticleUrl = article.ArticleUrl,
                PublishedAt = article.PublishedAt,
                CollectedAt = article.CollectedAt
            };

        return await query.Take(limit).ToListAsync(cancellationToken);
    }
}