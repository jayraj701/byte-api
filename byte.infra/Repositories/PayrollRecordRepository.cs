using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Byte.Infra.Repositories;

public class PayrollRecordRepository(AppDbContext context)
    : BaseRepository<PayrollRecord>(context), IPayrollRecordRepository
{
    public async Task<IEnumerable<PayrollRecord>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => await DbSet.AsNoTracking().Where(r => r.BatchId == batchId).ToListAsync(ct);
}
