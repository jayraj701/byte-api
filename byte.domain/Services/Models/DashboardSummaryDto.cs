namespace Byte.Domain.Services.Models;

public record DashboardSummaryDto(
    BatchStatusCountsDto BatchCounts,
    IEnumerable<SiteSummaryDto> SiteSummaries,
    IEnumerable<RecentBatchDto> RecentBatches
);

public record BatchStatusCountsDto(
    int Pending,
    int Approved,
    int Rejected,
    int Total
);

public record SiteSummaryDto(
    string Site,
    int BatchCount,
    int EmployeeCount,
    CalculationStatusCountsDto Calculations
);

public record CalculationStatusCountsDto(
    int Ready,
    int Disputed,
    int Flagged
);

public record RecentBatchDto(
    Guid Id,
    string FileName,
    DateTime UploadedAt,
    string Status,
    int RecordCount,
    string? ApprovedBy,
    DateTime? ApprovedAt
);
