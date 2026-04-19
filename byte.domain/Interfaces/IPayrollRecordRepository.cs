using Byte.Domain.Entities;

namespace Byte.Domain.Interfaces;

public interface IPayrollRecordRepository : IRepository<PayrollRecord>
{
    Task<IEnumerable<PayrollRecord>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);
}
