using Byte.Domain.Services.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Byte.Domain.Services;

public class FileParserService
{
    private static readonly string[] RequiredColumns =
        ["WorkerId", "WorkerName", "Site", "DaysPresent", "DayRate", "AdvanceDeduction"];

    public List<AttendanceRow> Parse(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext switch
        {
            ".csv"  => ParseCsv(file.OpenReadStream(), file.FileName),
            ".xlsx" => ParseExcel(file.OpenReadStream(), file.FileName),
            _       => throw new InvalidOperationException(
                           $"Unsupported file type '{ext}'. Upload a .csv or .xlsx file.")
        };
    }

    private static List<AttendanceRow> ParseCsv(Stream stream, string fileName)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<AttendanceRowMap>();

            if (!csv.Read())
                throw new InvalidOperationException("The CSV file is empty.");
            csv.ReadHeader();

            var missing = RequiredColumns
                .Where(col => !(csv.HeaderRecord ?? []).Contains(col, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"CSV is missing required columns: {string.Join(", ", missing)}. " +
                    $"Expected: {string.Join(", ", RequiredColumns)}.");

            var rows = new List<AttendanceRow>();
            while (csv.Read())
            {
                try
                {
                    rows.Add(csv.GetRecord<AttendanceRow>()!);
                }
                catch (TypeConverterException ex)
                {
                    var field = ex.MemberMapData?.Member?.Name ?? "unknown";
                    throw new InvalidOperationException(
                        $"Row {csv.Context.Parser.Row}: Cannot parse '{ex.Text}' for field '{field}'. " +
                        $"Ensure DaysPresent is a whole number and DayRate/AdvanceDeduction are decimals.", ex);
                }
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("The CSV file contains no data rows.");

            return rows;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse CSV '{fileName}': {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    private static List<AttendanceRow> ParseExcel(Stream stream, string fileName)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();
            var rows = ws.RowsUsed().ToList();

            if (rows.Count < 2)
                throw new InvalidOperationException(
                    "Excel file must have a header row and at least one data row.");

            var headers = rows[0].Cells().ToDictionary(
                c => c.Value.ToString().Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

            var missing = RequiredColumns.Where(col => !headers.ContainsKey(col)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"Excel file is missing required columns: {string.Join(", ", missing)}. " +
                    $"Expected: {string.Join(", ", RequiredColumns)}.");

            string Cell(IXLRow row, string col) =>
                headers.TryGetValue(col, out var idx) ? row.Cell(idx).Value.ToString().Trim() : string.Empty;

            var result = new List<AttendanceRow>();
            foreach (var row in rows.Skip(1))
            {
                var rowNum = row.RowNumber();
                try
                {
                    result.Add(new AttendanceRow
                    {
                        WorkerId          = Cell(row, "WorkerId"),
                        WorkerName        = Cell(row, "WorkerName"),
                        Site              = Cell(row, "Site"),
                        DaysPresent       = int.Parse(Cell(row, "DaysPresent"), CultureInfo.InvariantCulture),
                        DayRate           = decimal.Parse(Cell(row, "DayRate"), CultureInfo.InvariantCulture),
                        AdvanceDeduction  = decimal.Parse(Cell(row, "AdvanceDeduction"), CultureInfo.InvariantCulture)
                    });
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException(
                        $"Excel row {rowNum}: Cannot parse a numeric value. " +
                        $"Ensure DaysPresent is a whole number and DayRate/AdvanceDeduction are decimals. Detail: {ex.Message}", ex);
                }
            }

            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to read Excel file '{fileName}': {ex.Message}", ex);
        }
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
