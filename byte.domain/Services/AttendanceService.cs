using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Byte.Domain.Services;

public class AttendanceService(
    IPayrollBatchRepository batchRepo,
    IPayrollRecordRepository recordRepo,
    IAuditLogRepository auditRepo,
    FileParserService parser)
{
    public async Task<PayrollBatch> IngestAsync(IFormFile file, string actor, CancellationToken ct = default)
    {
        var rows = parser.Parse(file);

        var batch = new PayrollBatch
        {
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            BatchStatus = "Pending",
            CreatedBy = actor
        };
        await batchRepo.AddAsync(batch, ct);

        foreach (var row in rows)
        {
            var record = new PayrollRecord
            {
                WorkerId = row.WorkerId,
                WorkerName = row.WorkerName,
                Site = row.Site,
                DaysPresent = row.DaysPresent,
                DayRate = row.DayRate,
                AdvanceDeduction = row.AdvanceDeduction,
                BatchId = batch.Id,
                CreatedBy = actor
            };
            await recordRepo.AddAsync(record, ct);
        }

        await auditRepo.AddAsync(new AuditLog
        {
            EventType = "FileUploaded",
            BatchId = batch.Id,
            Actor = actor,
            Detail = $"Uploaded '{file.FileName}' with {rows.Count} records",
            OccurredAt = DateTime.UtcNow,
            CreatedBy = actor
        }, ct);

        return batch;
    }
}
