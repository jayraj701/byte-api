using Byte.Domain.Entities;

namespace Byte.Domain.Interfaces;

public interface IPayrollCalculationRepository : IRepository<PayrollCalculation>
{
    Task<IEnumerable<PayrollCalculation>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);
}
