using Iris.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iris.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration: IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");

        builder.HasKey(article => article.Id);
        builder.Property(article => article.Id).ValueGeneratedNever();

        builder.Property(article => article.Title).IsRequired();
        
        builder.Property(article => article.ArticleUrl).IsRequired();
        builder.HasIndex(article => article.ArticleUrl).IsUnique();
        
        builder.Property(article => article.PublishedAt)
            .HasConversion(
                value => value.HasValue
                    ? (long?)value.Value.UtcDateTime.Ticks
                    : null,
                value => value.HasValue
                    ? (DateTimeOffset?)new DateTimeOffset(value.Value, TimeSpan.Zero)
                    : null);

        builder.Property(article => article.CollectedAt)
            .HasConversion(
                value => value.UtcDateTime.Ticks,
                value => new DateTimeOffset(value, TimeSpan.Zero));
        
        builder.HasOne<RssSource>()
            .WithMany()
            .HasForeignKey(article => article.RssSourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}