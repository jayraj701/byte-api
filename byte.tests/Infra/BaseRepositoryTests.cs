using Byte.Domain.Entities;
using Byte.Infra.Data;
using Byte.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Byte.Tests.Infra;

public class BaseRepositoryTests : IDisposable
{
    private sealed class Product : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ProductRepository(AppDbContext ctx) : BaseRepository<Product>(ctx);

    private sealed class TestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>();
        }
    }

    private readonly TestDbContext _context;
    private readonly ProductRepository _repository;

    public BaseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _repository = new ProductRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity_AndReturnIt()
    {
        var product = new Product { Name = "Widget" };

        var result = await _repository.AddAsync(product);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Widget", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenItExists()
    {
        var product = new Product { Name = "Gadget" };
        await _repository.AddAsync(product);

        var result = await _repository.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Gadget", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        await _repository.AddAsync(new Product { Name = "A" });
        await _repository.AddAsync(new Product { Name = "B" });
        await _repository.AddAsync(new Product { Name = "C" });

        var results = await _repository.GetAllAsync();

        Assert.Equal(3, results.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEntitiesExist()
    {
        var results = await _repository.GetAllAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyEntity()
    {
        var product = new Product { Name = "Old Name" };
        await _repository.AddAsync(product);

        product.Name = "New Name";
        await _repository.UpdateAsync(product);

        var updated = await _repository.GetByIdAsync(product.Id);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        var product = new Product { Name = "To Delete" };
        await _repository.AddAsync(product);

        await _repository.DeleteAsync(product.Id);

        var result = await _repository.GetByIdAsync(product.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenEntityDoesNotExist()
    {
        var exception = await Record.ExceptionAsync(() =>
            _repository.DeleteAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    public void Dispose() => _context.Dispose();
}
