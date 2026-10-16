using Iris.Domain.Entities;

namespace Iris.Application.Abstractions.Persistence;

public interface IRssSourceRepository
{
    Task<bool> ExistsByFeedUrlAsync(
        string feedUrl,
        CancellationToken cancellationToken);

    Task AddAsync(
        RssSource source,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<RssSource>> GetAllAsync(
        CancellationToken cancellationToken);
    
    Task<RssSource?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    Task DeleteAsync(RssSource source, CancellationToken cancellationToken);
}