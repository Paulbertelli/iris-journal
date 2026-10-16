using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.Support.Fakes;

public sealed class FakeRssSourceRepository(params RssSource[] initialSources) : IRssSourceRepository
{
    private readonly List<RssSource> _sources = initialSources.ToList();

    public IReadOnlyCollection<RssSource> Sources => _sources;

    public Task<bool> ExistsByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken)
    {
        var exists = _sources.Any(source => source.FeedUrl == feedUrl);
        return Task.FromResult(exists);
    }

    public Task AddAsync(RssSource source, CancellationToken cancellationToken)
    {
        _sources.Add(source);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RssSource>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<RssSource>>(_sources.OrderBy(source => source.Name).ToList());
    }

    public Task<RssSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult<RssSource?>(_sources.SingleOrDefault(source => source.Id == id));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RssSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _sources.Remove(source);
        return Task.CompletedTask;
    }
}