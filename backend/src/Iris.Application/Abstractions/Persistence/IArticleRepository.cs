using Iris.Domain.Entities;

namespace Iris.Application.Abstractions.Persistence;

public interface IArticleRepository
{
    Task<bool> ExistsByArticleUrlAsync(
        string articleUrl,
        CancellationToken cancellationToken);

    Task AddAsync(
        Article article,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<Article>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken);
}