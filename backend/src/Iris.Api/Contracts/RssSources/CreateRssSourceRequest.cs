using System.ComponentModel.DataAnnotations;

namespace Iris.Api.Contracts.RssSources;

public sealed class CreateRssSourceRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string FeedUrl { get; init; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; init; } = string.Empty;
}