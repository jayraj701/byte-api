using Byte.Domain.Services;
using Byte.Domain.Entities;
using Byte.Domain.Interfaces;

namespace Byte.Tests.Payroll;

public class ApprovalServiceTests
{
    private readonly Mock<IPayrollBatchRepository> _batchRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();

    private ApprovalService BuildService() => new(_batchRepo.Object, _auditRepo.Object);

    [Fact]
    public async Task ApproveBatch_ShouldSetStatus_ToApproved()
    {
        var batch = new PayrollBatch { Id = Guid.NewGuid(), BatchStatus = "Pending" };
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, default)).ReturnsAsync(batch);
        _batchRepo.Setup(r => r.UpdateAsync(It.IsAny<PayrollBatch>(), default)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService();
        var result = await svc.ApproveBatchAsync(batch.Id, "admin", default);

        Assert.Equal("Approved", result.BatchStatus);
    }

    [Fact]
    public async Task ApproveBatch_ShouldSetApprovedBy_ToActor()
    {
        var batch = new PayrollBatch { Id = Guid.NewGuid(), BatchStatus = "Pending" };
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, default)).ReturnsAsync(batch);
        _batchRepo.Setup(r => r.UpdateAsync(It.IsAny<PayrollBatch>(), default)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService();
        var result = await svc.ApproveBatchAsync(batch.Id, "payroll-manager", default);

        Assert.Equal("payroll-manager", result.ApprovedBy);
    }

    [Fact]
    public async Task ApproveBatch_ShouldSetApprovedAt_ToUtcNow()
    {
        var batch = new PayrollBatch { Id = Guid.NewGuid(), BatchStatus = "Pending" };
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, default)).ReturnsAsync(batch);
        _batchRepo.Setup(r => r.UpdateAsync(It.IsAny<PayrollBatch>(), default)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var before = DateTime.UtcNow;
        var svc = BuildService();
        var result = await svc.ApproveBatchAsync(batch.Id, "admin", default);
        var after = DateTime.UtcNow;

        Assert.NotNull(result.ApprovedAt);
        Assert.InRange(result.ApprovedAt!.Value, before, after);
    }

    [Fact]
    public async Task ApproveBatch_ShouldThrowKeyNotFoundException_WhenBatchNotFound()
    {
        _batchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((PayrollBatch?)null);

        var svc = BuildService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ApproveBatchAsync(Guid.NewGuid(), "admin", default));
    }

    [Fact]
    public async Task ApproveBatch_ShouldWriteAuditLog_WithBatchApprovedEventType()
    {
        var batch = new PayrollBatch { Id = Guid.NewGuid(), BatchStatus = "Pending" };
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, default)).ReturnsAsync(batch);
        _batchRepo.Setup(r => r.UpdateAsync(It.IsAny<PayrollBatch>(), default)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService();
        await svc.ApproveBatchAsync(batch.Id, "admin", default);

        _auditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a => a.EventType == "BatchApproved"), default), Times.Once);
    }

    [Fact]
    public async Task ApproveBatch_ShouldWriteAuditLog_WithBatchId()
    {
        var batch = new PayrollBatch { Id = Guid.NewGuid(), BatchStatus = "Pending" };
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, default)).ReturnsAsync(batch);
        _batchRepo.Setup(r => r.UpdateAsync(It.IsAny<PayrollBatch>(), default)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService();
        await svc.ApproveBatchAsync(batch.Id, "admin", default);

        _auditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a => a.BatchId == batch.Id), default), Times.Once);
    }
}
