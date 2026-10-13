using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Entities;

namespace Iris.Application.UseCases.RssSources;

public sealed class ListRssSourcesUseCase(IRssSourceRepository rssSourceRepository)
{
    
    public Task<IReadOnlyList<RssSource>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return rssSourceRepository.GetAllAsync(cancellationToken);
    }
}