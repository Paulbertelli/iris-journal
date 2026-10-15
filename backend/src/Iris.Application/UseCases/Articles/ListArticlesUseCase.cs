using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.Articles;

public sealed class ListArticlesUseCase(
    IArticleRepository articleRepository)
{
    public Task<IReadOnlyList<Article>> ExecuteAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "La limite doit être comprise entre 1 et 100.");
        }

        return articleRepository.GetLatestAsync(limit, cancellationToken);
    }
}