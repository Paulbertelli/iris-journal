using Iris.Application.Abstractions.Feeds;
using Iris.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Iris.Api.Tests;

public sealed class IrisApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    
    public IReadOnlyList<RssFeedItem> FeedItems { get; set; } = Array.Empty<RssFeedItem>();

    public int FeedReadCount { get; private set; }

    public IrisApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IrisDbContext>();
            services.RemoveAll<DbContextOptions<IrisDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<IrisDbContext>>();

            services.AddDbContext<IrisDbContext>(options =>
                options.UseSqlite(_connection));
            
            services.RemoveAll<IRssFeedReader>();
            services.AddSingleton<IRssFeedReader>(
                new StubRssFeedReader(this));
        });
    }

    public void CreateDatabase()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        dbContext.Database.Migrate();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
    
    private sealed class StubRssFeedReader(
        IrisApiFactory factory) : IRssFeedReader
    {
        public Task<IReadOnlyList<RssFeedItem>> ReadAsync(
            string feedUrl,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            factory.FeedReadCount++;

            return Task.FromResult(factory.FeedItems);
        }
    }
}