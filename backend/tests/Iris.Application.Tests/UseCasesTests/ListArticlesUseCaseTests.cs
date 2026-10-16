using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.Articles;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.UseCasesTests;

public class ListArticlesUseCaseTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ExecuteAsync_WithInvalidLimit_ThrowsBeforeAccessingRepository(
        int limit)
    {
        var useCase = new ListArticlesUseCase(new UnusedArticleRepository());

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await useCase.ExecuteAsync(limit));

        Assert.Equal("limit", exception.ParamName);
    }

    private sealed class UnusedArticleRepository : IArticleRepository
    {
        public Task<IReadOnlyList<ArticleListItem>> GetLatestAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Le dépôt ne doit pas être appelé.");
        }

        public Task<bool> ExistsByArticleUrlAsync(
            string articleUrl,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Le dépôt ne doit pas être appelé.");
        }

        public Task AddAsync(
            Article article,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Le dépôt ne doit pas être appelé.");
        }
    }
}