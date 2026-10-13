using Iris.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Iris.Infrastructure.Persistence;

public sealed class IrisDbContext(DbContextOptions<IrisDbContext> options) : DbContext(options)
{
    public DbSet<RssSource> RssSources => Set<RssSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IrisDbContext).Assembly);
    }
}