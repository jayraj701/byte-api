using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Byte.Infra.Repositories;

public class PayrollCalculationRepository(AppDbContext context)
    : BaseRepository<PayrollCalculation>(context), IPayrollCalculationRepository
{
    public async Task<IEnumerable<PayrollCalculation>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => await DbSet.AsNoTracking().Where(c => c.BatchId == batchId).ToListAsync(ct);
}
