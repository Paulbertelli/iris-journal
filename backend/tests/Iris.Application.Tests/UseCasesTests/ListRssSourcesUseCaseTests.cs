using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.RssSources;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.UseCasesTests;

public class ListRssSourcesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsRepositoryResultAndForwardsCancellationToken()
    {
        IReadOnlyList<RssSource> sources =
        [
            new("Alpha", "https://example.com/alpha", "FR"),
            new("Zeta", "https://example.com/zeta", "GB")
        ];

        var repository = new StubRssSourceRepository(sources);
        var useCase = new ListRssSourcesUseCase(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(cancellation.Token);

        Assert.Same(sources, result);
        Assert.Equal(cancellation.Token, repository.ReceivedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryIsEmpty_ReturnsEmptyList()
    {
        var repository = new StubRssSourceRepository(
            Array.Empty<RssSource>());
        var useCase = new ListRssSourcesUseCase(repository);

        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }

    private sealed class StubRssSourceRepository(
        IReadOnlyList<RssSource> sources) : IRssSourceRepository
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<RssSource>> GetAllAsync(CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(sources);
        }

        public Task<RssSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(RssSource source, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(RssSource source, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}