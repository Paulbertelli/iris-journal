using Iris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iris.Infrastructure.Persistence.Configurations;

public sealed class RssSourceConfiguration : IEntityTypeConfiguration<RssSource>
{
    public void Configure(EntityTypeBuilder<RssSource> builder)
    {
        builder.ToTable("RssSources");

        builder.HasKey(source => source.Id);
        builder.Property(source => source.Id).ValueGeneratedNever();

        builder.Property(source => source.Name).IsRequired();
        builder.Property(source => source.FeedUrl).IsRequired();
        builder.Property(source => source.CountryCode)
            .IsRequired()
            .HasMaxLength(2);
        builder.Property(source => source.IsEnabled).IsRequired();

        // La base protège aussi contre les doublons, même si deux ajouts arrivent en même temps.
        builder.HasIndex(source => source.FeedUrl).IsUnique();
    }
}