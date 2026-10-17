namespace Iris.Api.Contracts.RssSources;

public sealed class BatchRssSourceRequest
{
    public string? Name { get; init; }
    public string? FeedUrl { get; init; }
    public string? CountryCode { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
}