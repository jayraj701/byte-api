using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;

namespace Byte.Infra.Repositories;

public class AuditLogRepository(AppDbContext context)
    : BaseRepository<AuditLog>(context), IAuditLogRepository
{
}
