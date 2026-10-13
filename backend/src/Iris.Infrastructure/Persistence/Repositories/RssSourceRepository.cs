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
}