using Byte.Domain.Services.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Byte.Domain.Services;

public class FileParserService
{
    public List<AttendanceRow> Parse(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => ParseCsv(file.OpenReadStream()),
            ".xlsx" => ParseExcel(file.OpenReadStream()),
            _ => throw new InvalidOperationException($"Unsupported file type '{ext}'. Upload a .csv or .xlsx file.")
        };
    }

    private static List<AttendanceRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<AttendanceRowMap>();
        return csv.GetRecords<AttendanceRow>().ToList();
    }

    private static List<AttendanceRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var rows = ws.RowsUsed().ToList();

        // Row 1 is headers — build column index map
        var headers = rows[0].Cells().ToDictionary(
            c => c.Value.ToString().Trim(),
            c => c.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);

        string Cell(IXLRow row, string col) =>
            headers.TryGetValue(col, out var idx) ? row.Cell(idx).Value.ToString().Trim() : string.Empty;

        return rows.Skip(1).Select(row => new AttendanceRow
        {
            WorkerId = Cell(row, "WorkerId"),
            WorkerName = Cell(row, "WorkerName"),
            Site = Cell(row, "Site"),
            DaysPresent = int.Parse(Cell(row, "DaysPresent")),
            DayRate = decimal.Parse(Cell(row, "DayRate"), CultureInfo.InvariantCulture),
            AdvanceDeduction = decimal.Parse(Cell(row, "AdvanceDeduction"), CultureInfo.InvariantCulture)
        }).ToList();
    }
}

public sealed class AttendanceRowMap : ClassMap<AttendanceRow>
{
    public AttendanceRowMap()
    {
        Map(m => m.WorkerId).Name("WorkerId");
        Map(m => m.WorkerName).Name("WorkerName");
        Map(m => m.Site).Name("Site");
        Map(m => m.DaysPresent).Name("DaysPresent");
        Map(m => m.DayRate).Name("DayRate");
        Map(m => m.AdvanceDeduction).Name("AdvanceDeduction");
    }
}
