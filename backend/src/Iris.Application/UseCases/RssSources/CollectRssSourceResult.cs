namespace Iris.Application.UseCases.RssSources;

public enum CollectRssSourceStatus
{
    Collected,
    NotFound,
    Disabled
}

public sealed record CollectRssSourceResult(
    CollectRssSourceStatus Status,
    int AddedCount = 0);