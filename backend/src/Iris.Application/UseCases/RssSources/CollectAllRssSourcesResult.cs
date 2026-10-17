namespace Iris.Application.UseCases.RssSources;

public enum SourceCollectionStatus
{
    Collected,
    NotFound,
    Disabled,
    Failed,
    Timeout
}

public sealed record SourceCollectionResult(
    Guid SourceId,
    string SourceName,
    SourceCollectionStatus Status,
    int AddedCount = 0,
    string? Error = null);

public sealed record CollectAllRssSourcesResult(
    IReadOnlyList<SourceCollectionResult> Sources)
{
    public int TotalAddedCount => Sources.Sum(source => source.AddedCount);
}