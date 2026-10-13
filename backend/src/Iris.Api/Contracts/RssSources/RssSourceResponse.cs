namespace Iris.Api.Contracts.RssSources;

public sealed class RssSourceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FeedUrl { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}