using Byte.Domain.Entities;
using Byte.Domain.Interfaces;

namespace Byte.Tests.Infra;

// Moq uses Castle DynamicProxy which requires the entity type to be public
public sealed class Order : AuditableEntity
{
    public string Reference { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

/// <summary>
/// Demonstrates how to mock IRepository when testing application services.
/// Copy this pattern: create a service, inject IRepository via constructor, mock it with Moq.
/// </summary>
public class RepositoryMockTests
{
    // Example application service that depends on IRepository
    private sealed class OrderService(IRepository<Order> repository)
    {
        public Task<Order?> GetOrderAsync(Guid id) =>
            repository.GetByIdAsync(id);

        public async Task<Order> PlaceOrderAsync(string reference, decimal total)
        {
            var order = new Order { Reference = reference, Total = total };
            return await repository.AddAsync(order);
        }

        public async Task<bool> CancelOrderAsync(Guid id)
        {
            var order = await repository.GetByIdAsync(id);
            if (order is null) return false;

            await repository.DeleteAsync(id);
            return true;
        }
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnOrder_WhenItExists()
    {
        var orderId = Guid.NewGuid();
        var expected = new Order { Reference = "ORD-001", Total = 99.99m };

        var mockRepo = new Mock<IRepository<Order>>();
        mockRepo
            .Setup(r => r.GetByIdAsync(orderId, default))
            .ReturnsAsync(expected);

        var service = new OrderService(mockRepo.Object);

        var result = await service.GetOrderAsync(orderId);

        Assert.Equal(expected, result);
        mockRepo.Verify(r => r.GetByIdAsync(orderId, default), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnNull_WhenOrderNotFound()
    {
        var mockRepo = new Mock<IRepository<Order>>();
        mockRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Order?)null);

        var service = new OrderService(mockRepo.Object);

        var result = await service.GetOrderAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldCallAddAsync_WithCorrectData()
    {
        var mockRepo = new Mock<IRepository<Order>>();
        mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), default))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        var service = new OrderService(mockRepo.Object);

        var result = await service.PlaceOrderAsync("ORD-002", 150m);

        Assert.Equal("ORD-002", result.Reference);
        Assert.Equal(150m, result.Total);
        mockRepo.Verify(r => r.AddAsync(It.Is<Order>(o => o.Reference == "ORD-002"), default), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldReturnTrue_AndDeleteOrder_WhenOrderExists()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Reference = "ORD-003" };

        var mockRepo = new Mock<IRepository<Order>>();
        mockRepo.Setup(r => r.GetByIdAsync(orderId, default)).ReturnsAsync(order);
        mockRepo.Setup(r => r.DeleteAsync(orderId, default)).Returns(Task.CompletedTask);

        var service = new OrderService(mockRepo.Object);

        var result = await service.CancelOrderAsync(orderId);

        Assert.True(result);
        mockRepo.Verify(r => r.DeleteAsync(orderId, default), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_ShouldReturnFalse_WhenOrderDoesNotExist()
    {
        var mockRepo = new Mock<IRepository<Order>>();
        mockRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Order?)null);

        var service = new OrderService(mockRepo.Object);

        var result = await service.CancelOrderAsync(Guid.NewGuid());

        Assert.False(result);
        mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
