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
}