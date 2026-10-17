using Iris.Application.Abstractions.Feeds;
using Iris.Application.Abstractions.Persistence;

namespace Iris.Application.UseCases.RssSources;

public sealed class CollectAllRssSourcesUseCase(
    IRssSourceRepository rssSourceRepository,
    CollectRssSourceUseCase collectRssSourceUseCase)
{
    public async Task<CollectAllRssSourcesResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await rssSourceRepository.GetAllAsync(
            cancellationToken);

        var results = new List<SourceCollectionResult>();

        foreach (var source in sources.Where(source => source.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await collectRssSourceUseCase.ExecuteAsync(
                    source.Id, cancellationToken);

                var status = result.Status switch
                {
                    CollectRssSourceStatus.Collected =>
                        SourceCollectionStatus.Collected,

                    CollectRssSourceStatus.NotFound =>
                        SourceCollectionStatus.NotFound,

                    CollectRssSourceStatus.Disabled =>
                        SourceCollectionStatus.Disabled,

                    _ => throw new InvalidOperationException(
                        "Résultat de collecte inconnu.")
                };

                results.Add(new SourceCollectionResult(
                    source.Id,
                    source.Name,
                    status,
                    result.AddedCount));
            }
            catch (FeedReadException exception)
            {
                results.Add(new SourceCollectionResult(
                    source.Id,
                    source.Name,
                    exception.IsTimeout
                        ? SourceCollectionStatus.Timeout
                        : SourceCollectionStatus.Failed,
                    Error: exception.Message));
            }
        }

        return new CollectAllRssSourcesResult(results);
    }
}