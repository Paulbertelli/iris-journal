using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.RssSources;
using Iris.Domain.Entities;

namespace Iris.Application.Tests;

public class AddRssSourceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSourceIsNew_AddsItAndReturnsTrue()
    {
        var repository = new FakeRssSourceRepository();
        var useCase = new AddRssSourceUseCase(repository);

        var result = await useCase.ExecuteAsync(
            "Le Monde",
            "https://example.com/feed",
            "FR");

        Assert.True(result);

        var source = Assert.Single(repository.Sources);
        Assert.Equal("Le Monde", source.Name);
        Assert.Equal("FR", source.CountryCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceAlreadyExists_ReturnsFalse()
    {
        var repository = new FakeRssSourceRepository();
        var existingSource = new RssSource(
            "Le Monde",
            "https://example.com/feed",
            "FR");

        await repository.AddAsync(existingSource, CancellationToken.None);

        var useCase = new AddRssSourceUseCase(repository);

        var result = await useCase.ExecuteAsync(
            "Le Monde",
            "https://example.com/feed",
            "FR");

        Assert.False(result);
        Assert.Single(repository.Sources);
    }

    private sealed class FakeRssSourceRepository : IRssSourceRepository
    {
        private readonly List<RssSource> _sources = new();

        public IReadOnlyCollection<RssSource> Sources => _sources;

        public Task<bool> ExistsByFeedUrlAsync(
            string feedUrl,
            CancellationToken cancellationToken)
        {
            var exists = _sources.Any(source => source.FeedUrl == feedUrl);
            return Task.FromResult(exists);
        }

        public Task AddAsync(
            RssSource source,
            CancellationToken cancellationToken)
        {
            _sources.Add(source);
            return Task.CompletedTask;
        }
    }
}