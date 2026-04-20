using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Byte.Infra.Repositories;

public class AuditLogRepository(AppDbContext context)
    : BaseRepository<AuditLog>(context), IAuditLogRepository
{
    public async Task<IEnumerable<AuditLog>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => await DbSet.AsNoTracking()
            .Where(a => a.BatchId == batchId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);
}
