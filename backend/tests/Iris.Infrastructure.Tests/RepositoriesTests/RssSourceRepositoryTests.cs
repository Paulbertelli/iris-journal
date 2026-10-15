using Iris.Domain.Entities;
using Iris.Infrastructure.Persistence;
using Iris.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Iris.Infrastructure.Tests.RepositoriesTests;

public class RssSourceRepositoryTests
{
    [Fact]
    public async Task AddAsync_WithValidSource_PersistsSource()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new RssSourceRepository(dbContext);
        var source = new RssSource(
            "Source test",
            "https://example.com/feed",
            "FR");

        await repository.AddAsync(source, CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.RssSources.SingleAsync();

        Assert.Equal(source.Id, saved.Id);
        Assert.Equal("Source test", saved.Name);
        Assert.Equal("https://example.com/feed", saved.FeedUrl);
        Assert.Equal("FR", saved.CountryCode);
        Assert.True(saved.IsEnabled);
    }

    [Theory]
    [InlineData("https://example.com/feed", true)]
    [InlineData("https://example.com/unknown", false)]
    public async Task ExistsByFeedUrlAsync_ReturnsExpectedResult(
        string feedUrl,
        bool expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new RssSourceRepository(dbContext);

        await repository.AddAsync(
            new RssSource("Source test", "https://example.com/feed", "FR"),
            CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var result = await repository.ExistsByFeedUrlAsync(
            feedUrl,
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new RssSourceRepository(dbContext);

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithSources_ReturnsSourcesOrderedByName()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new RssSourceRepository(dbContext);

        var zeta = new RssSource(
            "Zeta", "https://example.com/zeta", "FR");
        var alpha = new RssSource(
            "Alpha", "https://example.com/alpha", "GB");

        await repository.AddAsync(zeta, CancellationToken.None);
        await repository.AddAsync(alpha, CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(new[] { alpha.Id, zeta.Id },
            result.Select(source => source.Id));
    }

    [Fact]
    public async Task AddAsync_WithDuplicateFeedUrl_ThrowsDbUpdateException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new IrisDbContext(options);
        await dbContext.Database.MigrateAsync();

        var repository = new RssSourceRepository(dbContext);

        await repository.AddAsync(
            new RssSource("Source test", "https://example.com/feed", "FR"),
            CancellationToken.None);

        var duplicate = new RssSource(
            "Autre source", "https://example.com/feed", "GB");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(duplicate, CancellationToken.None));

        var sqliteException =
            Assert.IsType<SqliteException>(exception.InnerException);

        Assert.Equal(2067, sqliteException.SqliteExtendedErrorCode);

        dbContext.ChangeTracker.Clear();
        Assert.Equal(1, await dbContext.RssSources.CountAsync());
    }
}