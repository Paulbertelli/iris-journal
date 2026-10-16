using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Iris.Infrastructure.Persistence.Repositories;

public sealed class RssSourceRepository(IrisDbContext dbContext) : IRssSourceRepository
{
    public Task<bool> ExistsByFeedUrlAsync(
        string feedUrl,
        CancellationToken cancellationToken)
    {
        return dbContext.RssSources
            .AnyAsync(source => source.FeedUrl == feedUrl, cancellationToken);
    }

    public async Task AddAsync(
        RssSource source,
        CancellationToken cancellationToken)
    {
        dbContext.RssSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RssSource>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RssSources.AsNoTracking().OrderBy(source => source.Name).ToListAsync(cancellationToken);
    }

    public async Task<RssSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.RssSources.SingleOrDefaultAsync(source => source.Id == id, cancellationToken);
    }
    
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}