using Byte.Domain.Entities;
using Byte.Domain.Interfaces;

namespace Byte.Api.Services;

public class ApprovalService(
    IPayrollBatchRepository batchRepo,
    IAuditLogRepository auditRepo)
{
    public async Task<PayrollBatch> ApproveBatchAsync(Guid batchId, string actor, CancellationToken ct = default)
    {
        var batch = await batchRepo.GetByIdAsync(batchId, ct)
            ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

        batch.BatchStatus = "Approved";
        batch.ApprovedBy = actor;
        batch.ApprovedAt = DateTime.UtcNow;
        batch.UpdatedBy = actor;

        await batchRepo.UpdateAsync(batch, ct);

        await auditRepo.AddAsync(new AuditLog
        {
            EventType = "BatchApproved",
            BatchId = batchId,
            Actor = actor,
            Detail = $"Batch {batchId} approved",
            OccurredAt = DateTime.UtcNow,
            CreatedBy = actor
        }, ct);

        return batch;
    }
}
