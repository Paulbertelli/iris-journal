using Iris.Domain.Entities;
using Iris.Infrastructure.Persistence;
using Iris.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Iris.Infrastructure.Tests.RepositoriesTests;

public class ArticleRepositoryTests
{
    [Fact]
    public async Task AddAsync_WithValidArticle_PersistsArticleAndUtcDates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var source = await CreateSourceAsync(dbContext);
        var repository = new ArticleRepository(dbContext);

        var publishedAt = new DateTimeOffset(
            2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(2));

        var article = new Article(
            source.Id,
            "Article test",
            "https://example.com/article",
            publishedAt);

        await repository.AddAsync(article, CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Articles.SingleAsync();

        Assert.Equal(article.Id, saved.Id);
        Assert.Equal(source.Id, saved.RssSourceId);
        Assert.Equal("Article test", saved.Title);
        Assert.Equal("https://example.com/article", saved.ArticleUrl);

        Assert.NotNull(saved.PublishedAt);
        Assert.Equal(
            new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero),
            saved.PublishedAt.Value);
        Assert.Equal(TimeSpan.Zero, saved.PublishedAt.Value.Offset);

        Assert.Equal(article.CollectedAt, saved.CollectedAt);
        Assert.Equal(TimeSpan.Zero, saved.CollectedAt.Offset);
    }

    [Fact]
    public async Task AddAsync_WithoutPublishedAt_PersistsNullDate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var source = await CreateSourceAsync(dbContext);
        var repository = new ArticleRepository(dbContext);

        var article = new Article(
            source.Id,
            "Sans date",
            "https://example.com/without-date");

        await repository.AddAsync(article, CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Articles.SingleAsync();

        Assert.Null(saved.PublishedAt);
    }

    [Theory]
    [InlineData("https://example.com/article", true)]
    [InlineData("https://example.com/unknown", false)]
    public async Task ExistsByArticleUrlAsync_ReturnsExpectedResult(
        string articleUrl,
        bool expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var source = await CreateSourceAsync(dbContext);
        var repository = new ArticleRepository(dbContext);

        await repository.AddAsync(
            new Article(
                source.Id,
                "Article test",
                "https://example.com/article"),
            CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var result = await repository.ExistsByArticleUrlAsync(
            articleUrl,
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateUrl_ThrowsDbUpdateException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var source = await CreateSourceAsync(dbContext);
        var repository = new ArticleRepository(dbContext);

        await repository.AddAsync(
            new Article(
                source.Id,
                "Premier article",
                "https://example.com/article"),
            CancellationToken.None);

        var duplicate = new Article(
            source.Id,
            "Doublon",
            "https://example.com/article");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(duplicate, CancellationToken.None));

        var sqliteException =
            Assert.IsType<SqliteException>(exception.InnerException);

        // SQLITE_CONSTRAINT_UNIQUE
        Assert.Equal(2067, sqliteException.SqliteExtendedErrorCode);

        dbContext.ChangeTracker.Clear();
        Assert.Equal(1, await dbContext.Articles.CountAsync());
    }

    [Fact]
    public async Task AddAsync_WithUnknownSource_ThrowsDbUpdateException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new ArticleRepository(dbContext);

        var article = new Article(
            Guid.NewGuid(),
            "Source inexistante",
            "https://example.com/article");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(article, CancellationToken.None));

        var sqliteException =
            Assert.IsType<SqliteException>(exception.InnerException);

        // SQLITE_CONSTRAINT_FOREIGNKEY
        Assert.Equal(787, sqliteException.SqliteExtendedErrorCode);

        dbContext.ChangeTracker.Clear();
        Assert.Empty(await dbContext.Articles.ToListAsync());
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsLatestArticlesUpToLimit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var source = await CreateSourceAsync(dbContext);
        var repository = new ArticleRepository(dbContext);

        var oldest = new Article(
            source.Id, "Ancien", "https://example.com/oldest");
        var middle = new Article(
            source.Id, "Intermédiaire", "https://example.com/middle");
        var newest = new Article(
            source.Id, "Récent", "https://example.com/newest");

        var date = new DateTimeOffset(
            2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

        dbContext.Articles.AddRange(middle, newest, oldest);

        dbContext.Entry(oldest)
            .Property(article => article.CollectedAt).CurrentValue = date;
        dbContext.Entry(middle)
            .Property(article => article.CollectedAt).CurrentValue = date.AddMinutes(1);
        dbContext.Entry(newest)
            .Property(article => article.CollectedAt).CurrentValue = date.AddMinutes(2);

        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var result = await repository.GetLatestAsync(
            2, CancellationToken.None);

        Assert.Equal(
            [newest.Id, middle.Id],
            result.Select(article => article.Id));
    }

    private async static Task<RssSource> CreateSourceAsync(
        IrisDbContext dbContext)
    {
        var source = new RssSource(
            "Source test",
            "https://example.com/feed",
            "FR");

        dbContext.RssSources.Add(source);
        await dbContext.SaveChangesAsync();

        return source;
    }
}