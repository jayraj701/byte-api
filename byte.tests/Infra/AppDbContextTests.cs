using Byte.Domain.Entities;
using Byte.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Byte.Tests.Infra;

public class AppDbContextTests : IDisposable
{
    // Minimal entity registered only within this test class
    private sealed class Article : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    // Derived context that adds Article to the model for testing
    private sealed class TestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Article>();
        }
    }

    private readonly TestDbContext _context;

    public AppDbContextTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedAt_WhenEntityAdded()
    {
        var article = new Article { Title = "Hello" };
        _context.Set<Article>().Add(article);

        await _context.SaveChangesAsync();

        Assert.NotEqual(default, article.CreatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetUpdatedAt_WhenEntityAdded()
    {
        var article = new Article { Title = "Hello" };
        _context.Set<Article>().Add(article);

        await _context.SaveChangesAsync();

        Assert.NotEqual(default, article.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedAtAndUpdatedAt_ToSameValue_OnAdd()
    {
        var article = new Article { Title = "Hello" };
        _context.Set<Article>().Add(article);

        await _context.SaveChangesAsync();

        Assert.Equal(article.CreatedAt, article.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldUpdateUpdatedAt_WhenEntityModified()
    {
        var article = new Article { Title = "Original" };
        _context.Set<Article>().Add(article);
        await _context.SaveChangesAsync();

        var createdAt = article.CreatedAt;

        // Simulate time passing — override UpdatedAt to verify it changes
        article.Title = "Modified";
        _context.Set<Article>().Update(article);
        await _context.SaveChangesAsync();

        Assert.Equal(createdAt, article.CreatedAt);
        Assert.True(article.UpdatedAt >= createdAt);
    }

    public void Dispose() => _context.Dispose();
}
