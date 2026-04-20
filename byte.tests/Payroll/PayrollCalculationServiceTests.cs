using Byte.Domain.Configuration;
using Byte.Domain.Services;
using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Byte.Tests.Payroll;

public class PayrollCalculationServiceTests
{
    private readonly Mock<IPayrollRecordRepository> _recordRepo = new();
    private readonly Mock<IPayrollCalculationRepository> _calcRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();

    private PayrollCalculationService BuildService(decimal disputeThreshold, Dictionary<string, decimal>? siteAllowances = null)
    {
        var options = Options.Create(new PayrollRulesOptions
        {
            DisputeThreshold = disputeThreshold,
            SiteAllowances = siteAllowances ?? new Dictionary<string, decimal> { ["Default"] = 0m }
        });
        return new PayrollCalculationService(_recordRepo.Object, _calcRepo.Object, _auditRepo.Object, options);
    }

    private static PayrollRecord MakeRecord(string site = "SiteA", int days = 22, decimal dayRate = 85m, decimal advance = 0m)
        => new() { Id = Guid.NewGuid(), BatchId = Guid.NewGuid(), Site = site, DaysPresent = days, DayRate = dayRate, AdvanceDeduction = advance };

    [Fact]
    public async Task BasePay_ShouldEqual_DaysPresent_Times_DayRate()
    {
        var record = MakeRecord(days: 22, dayRate: 85m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m);
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(1870m, results[0].BasePay);
    }

    [Fact]
    public async Task SiteAllowance_ShouldApply_ConfiguredRate()
    {
        var record = MakeRecord(site: "SiteA", days: 22);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(330m, results[0].SiteAllowance); // 22 × 15
    }

    [Fact]
    public async Task SiteAllowance_ShouldFallbackToDefault_WhenSiteNotConfigured()
    {
        var record = MakeRecord(site: "UnknownSite", days: 10);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m, ["Default"] = 5m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(50m, results[0].SiteAllowance); // 10 × 5
    }

    [Fact]
    public async Task SiteAllowance_ShouldBeZero_WhenDefaultNotConfigured()
    {
        var record = MakeRecord(site: "UnknownSite", days: 10);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(0m, results[0].SiteAllowance);
    }

    [Fact]
    public async Task GrossPay_ShouldEqual_BasePay_Plus_SiteAllowance()
    {
        var record = MakeRecord(site: "SiteA", days: 22, dayRate: 85m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(2200m, results[0].GrossPay); // 1870 + 330
    }

    [Fact]
    public async Task NetPay_ShouldDeduct_AdvanceDeduction()
    {
        var record = MakeRecord(site: "SiteA", days: 22, dayRate: 85m, advance: 200m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(2000m, results[0].NetPay); // 2200 - 200
    }

    [Fact]
    public async Task Status_ShouldBeReady_WhenAdvanceDeduction_BelowGrossPay()
    {
        var record = MakeRecord(days: 22, dayRate: 85m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(disputeThreshold: 100m);
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal("Ready", results[0].Status);
    }

    [Fact]
    public async Task Status_ShouldBeDisputed_WhenAdvanceDeduction_ExceedsGrossPay()
    {
        var record = MakeRecord(days: 1, dayRate: 10m, advance: 200m); // NetPay = 10 - 200 = -190
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(disputeThreshold: 100m);
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal("Disputed", results[0].Status);
    }

    [Fact]
    public async Task Status_ShouldBeReady_WhenAdvanceDeduction_EqualsGrossPay()
    {
        var record = MakeRecord(days: 1, dayRate: 100m, advance: 100m); // NetPay = 0, not negative
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(disputeThreshold: 100m);
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal("Ready", results[0].Status);
    }

    [Fact]
    public async Task CalculateBatch_ShouldPersistOneCalculation_PerRecord()
    {
        var batchId = Guid.NewGuid();
        var records = new List<PayrollRecord>
        {
            new() { Id = Guid.NewGuid(), BatchId = batchId, Site = "SiteA", DaysPresent = 10, DayRate = 50m },
            new() { Id = Guid.NewGuid(), BatchId = batchId, Site = "SiteA", DaysPresent = 15, DayRate = 60m },
            new() { Id = Guid.NewGuid(), BatchId = batchId, Site = "SiteB", DaysPresent = 20, DayRate = 70m },
        };
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync(records);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m);
        await svc.CalculateBatchAsync(batchId, "system", default);

        _calcRepo.Verify(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default), Times.Exactly(3));
    }

    [Fact]
    public async Task CalculateBatch_ShouldWriteAuditLog_WithCorrectEventType()
    {
        var record = MakeRecord();
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService(0m);
        await svc.CalculateBatchAsync(batchId, "system", default);

        _auditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a => a.EventType == "CalculationRun"), default), Times.Once);
    }

    [Fact]
    public async Task CalculateBatch_ShouldWriteAuditLog_WithActor()
    {
        var record = MakeRecord();
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), default)).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var svc = BuildService(0m);
        await svc.CalculateBatchAsync(batchId, "admin-user", default);

        _auditRepo.Verify(r => r.AddAsync(
            It.Is<AuditLog>(a => a.Actor == "admin-user"), default), Times.Once);
    }

    [Fact]
    public async Task CalculateBatch_ShouldHandleZeroAdvanceDeduction()
    {
        var record = MakeRecord(days: 10, dayRate: 50m, advance: 0m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        PayrollCalculation? saved = null;
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default))
            .Callback<PayrollCalculation, CancellationToken>((c, _) => saved = c)
            .ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m);
        await svc.CalculateBatchAsync(batchId, "system", default);

        Assert.NotNull(saved);
        Assert.Equal(saved!.GrossPay, saved.NetPay);
    }

    [Fact]
    public async Task CalculateBatch_ShouldHandleZeroDaysPresent()
    {
        var record = MakeRecord(days: 0, dayRate: 85m, advance: 0m);
        var batchId = record.BatchId;
        _recordRepo.Setup(r => r.GetByBatchIdAsync(batchId, default)).ReturnsAsync([record]);
        _calcRepo.Setup(r => r.AddAsync(It.IsAny<PayrollCalculation>(), default)).ReturnsAsync((PayrollCalculation c, CancellationToken _) => c);

        var svc = BuildService(0m, new Dictionary<string, decimal> { ["SiteA"] = 15m });
        var results = (await svc.CalculateBatchAsync(batchId, "system", default)).ToList();

        Assert.Equal(0m, results[0].BasePay);
        Assert.Equal(0m, results[0].SiteAllowance);
    }
}
