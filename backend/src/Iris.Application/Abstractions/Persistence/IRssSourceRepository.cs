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
}