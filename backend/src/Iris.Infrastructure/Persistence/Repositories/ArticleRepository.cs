using Iris.Application.Abstractions.Persistence;
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

    public async Task<IReadOnlyList<Article>> GetLatestAsync(int limit, CancellationToken cancellationToken)
    {
        return await dbContext.Articles
            .AsNoTracking()
            .OrderByDescending(article => article.CollectedAt)
            .ThenByDescending(article => article.Id )
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}