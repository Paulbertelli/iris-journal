using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.Support.Fakes;

public sealed class FakeArticleRepository(
    params Article[] initialArticles) : IArticleRepository
{
    private readonly List<Article> _articles = initialArticles.ToList();

    public IReadOnlyList<Article> Articles => _articles;

    public Task<bool> ExistsByArticleUrlAsync(
        string articleUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            _articles.Any(article => article.ArticleUrl == articleUrl));
    }

    public Task AddAsync(
        Article article,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _articles.Add(article);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Article>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<Article>>(
            _articles
                .OrderByDescending(article => article.CollectedAt)
                .ThenByDescending(article => article.Id)
                .Take(limit)
                .ToList());
    }
}