using Iris.Application.Abstractions.Persistence;
using Iris.Application.UseCases.Articles;
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

    public Task<IReadOnlyList<ArticleListItem>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Ce faux dépôt sert aux tests de collecte, pas à la lecture des articles.");
    }
}