using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Byte.Infra.Data;

namespace Byte.Infra.Repositories;

public class PayrollBatchRepository(AppDbContext context)
    : BaseRepository<PayrollBatch>(context), IPayrollBatchRepository
{
}
