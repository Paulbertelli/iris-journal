using Iris.Application.Abstractions.Persistence;

namespace Iris.Application.UseCases.RssSources;

public sealed class DeleteRssSourceUseCase(
    IRssSourceRepository rssSourceRepository)
{
    public async Task<bool> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var source = await rssSourceRepository.GetByIdAsync(
            id, cancellationToken);

        if (source is null)
            return false;

        await rssSourceRepository.DeleteAsync(source, cancellationToken);

        return true;
    }
}