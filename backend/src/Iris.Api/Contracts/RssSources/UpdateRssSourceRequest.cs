using System.ComponentModel.DataAnnotations;

namespace Iris.Api.Contracts.RssSources;

public sealed class UpdateRssSourceRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string FeedUrl { get; init; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; init; } = string.Empty;

    [Required]
    public bool? IsEnabled { get; init; }

    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
}