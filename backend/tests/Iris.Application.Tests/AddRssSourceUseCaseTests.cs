using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.RssSources;
using Iris.Domain.Entities;

namespace Iris.Application.Tests;

public class AddRssSourceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSourceIsNew_AddsItAndReturnsId()
    {
        var repository = new FakeRssSourceRepository();
        var useCase = new AddRssSourceUseCase(repository);

        var result = await useCase.ExecuteAsync(
            "Le Monde",
            "https://example.com/feed",
            "FR");

        var source = Assert.Single(repository.Sources);
        
        Assert.NotNull(result);
        Assert.Equal(source.Id, result.Value);
        Assert.Equal("Le Monde", source.Name);
        Assert.Equal("FR", source.CountryCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceAlreadyExists_ReturnsNull()
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

        Assert.Null(result);
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