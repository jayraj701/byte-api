using Byte.Domain.Interfaces;
using Byte.Domain.Services.Models;

namespace Byte.Domain.Services;

public class DashboardService(
    IPayrollBatchRepository batchRepo,
    IPayrollRecordRepository recordRepo,
    IPayrollCalculationRepository calcRepo)
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var batches = (await batchRepo.GetAllAsync(ct)).ToList();
        var records = (await recordRepo.GetAllAsync(ct)).ToList();
        var calculations = (await calcRepo.GetAllAsync(ct)).ToList();

        var batchCounts = new BatchStatusCountsDto(
            Pending: batches.Count(b => b.BatchStatus == "Pending"),
            Approved: batches.Count(b => b.BatchStatus == "Approved"),
            Rejected: batches.Count(b => b.BatchStatus == "Rejected"),
            Total: batches.Count
        );

        var recordSiteMap = records.ToDictionary(r => r.Id, r => r.Site);

        var calcsBySite = calculations
            .Where(c => recordSiteMap.ContainsKey(c.PayrollRecordId))
            .GroupBy(c => recordSiteMap[c.PayrollRecordId])
            .ToDictionary(g => g.Key, g => g.ToList());

        var siteSummaries = records
            .GroupBy(r => r.Site)
            .Select(siteGroup =>
            {
                var site = siteGroup.Key;
                var siteCalcs = calcsBySite.TryGetValue(site, out var calcs) ? calcs : [];

                return new SiteSummaryDto(
                    Site: site,
                    BatchCount: siteGroup.Select(r => r.BatchId).Distinct().Count(),
                    EmployeeCount: siteGroup.Select(r => r.WorkerId).Distinct().Count(),
                    Calculations: new CalculationStatusCountsDto(
                        Ready: siteCalcs.Count(c => c.Status == "Ready"),
                        Disputed: siteCalcs.Count(c => c.Status == "Disputed"),
                        Flagged: siteCalcs.Count(c => c.Status == "Flagged")
                    )
                );
            })
            .OrderBy(s => s.Site)
            .ToList();

        var recordCountByBatch = records
            .GroupBy(r => r.BatchId)
            .ToDictionary(g => g.Key, g => g.Count());

        var recentBatches = batches
            .OrderByDescending(b => b.UploadedAt)
            .Take(10)
            .Select(b => new RecentBatchDto(
                Id: b.Id,
                FileName: b.FileName,
                UploadedAt: b.UploadedAt,
                Status: b.BatchStatus,
                RecordCount: recordCountByBatch.TryGetValue(b.Id, out var count) ? count : 0,
                ApprovedBy: b.ApprovedBy,
                ApprovedAt: b.ApprovedAt
            ))
            .ToList();

        return new DashboardSummaryDto(batchCounts, siteSummaries, recentBatches);
    }
}
