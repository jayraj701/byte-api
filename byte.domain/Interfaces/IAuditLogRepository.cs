using Byte.Domain.Entities;

namespace Byte.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByBatchIdAsync(Guid batchId, CancellationToken ct = default);
}
