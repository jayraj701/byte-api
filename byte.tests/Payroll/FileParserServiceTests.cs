using Byte.Api.Services;
using Byte.Api.Services.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Byte.Tests.Payroll;

public class FileParserServiceTests
{
    private static readonly string CsvContent =
        "WorkerId,WorkerName,Site,DaysPresent,DayRate,AdvanceDeduction\n" +
        "W001,John Smith,SiteA,22,85.00,200.00\n" +
        "W002,Jane Doe,SiteB,18,90.50,0.00\n" +
        "W003,Bob Jones,SiteC,20,75.00,500.00\n";

    private static IFormFile MakeCsvFile(string content, string fileName = "attendance.csv")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(bytes.Length);
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        return file.Object;
    }

    private static IFormFile MakeExcelFile(string fileName = "attendance.xlsx")
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Attendance");
            ws.Cell(1, 1).Value = "WorkerId";
            ws.Cell(1, 2).Value = "WorkerName";
            ws.Cell(1, 3).Value = "Site";
            ws.Cell(1, 4).Value = "DaysPresent";
            ws.Cell(1, 5).Value = "DayRate";
            ws.Cell(1, 6).Value = "AdvanceDeduction";
            ws.Cell(2, 1).Value = "W001";
            ws.Cell(2, 2).Value = "John Smith";
            ws.Cell(2, 3).Value = "SiteA";
            ws.Cell(2, 4).Value = 22;
            ws.Cell(2, 5).Value = 85.00;
            ws.Cell(2, 6).Value = 200.00;
            ws.Cell(3, 1).Value = "W002";
            ws.Cell(3, 2).Value = "Jane Doe";
            ws.Cell(3, 3).Value = "SiteB";
            ws.Cell(3, 4).Value = 18;
            ws.Cell(3, 5).Value = 90.50;
            ws.Cell(3, 6).Value = 0.00;
            ws.Cell(4, 1).Value = "W003";
            ws.Cell(4, 2).Value = "Bob Jones";
            ws.Cell(4, 3).Value = "SiteC";
            ws.Cell(4, 4).Value = 20;
            ws.Cell(4, 5).Value = 75.00;
            ws.Cell(4, 6).Value = 500.00;
            workbook.SaveAs(stream);
        }
        stream.Position = 0;
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(stream.Length);
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        return file.Object;
    }

    [Fact]
    public void ParseCsv_ShouldReturn_CorrectRowCount()
    {
        var file = MakeCsvFile(CsvContent);
        var svc = new FileParserService();

        var rows = svc.Parse(file);

        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void ParseCsv_ShouldMap_AllFields_Correctly()
    {
        var file = MakeCsvFile(CsvContent);
        var svc = new FileParserService();

        var rows = svc.Parse(file);
        var first = rows[0];

        Assert.Equal("W001", first.WorkerId);
        Assert.Equal("John Smith", first.WorkerName);
        Assert.Equal("SiteA", first.Site);
        Assert.Equal(22, first.DaysPresent);
        Assert.Equal(85.00m, first.DayRate);
        Assert.Equal(200.00m, first.AdvanceDeduction);
    }

    [Fact]
    public void ParseCsv_ShouldHandle_DecimalDayRate()
    {
        var file = MakeCsvFile(CsvContent);
        var svc = new FileParserService();

        var rows = svc.Parse(file);

        Assert.Equal(90.50m, rows[1].DayRate);
    }

    [Fact]
    public void ParseExcel_ShouldReturn_CorrectRowCount()
    {
        var file = MakeExcelFile();
        var svc = new FileParserService();

        var rows = svc.Parse(file);

        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void ParseExcel_ShouldMap_AllFields_Correctly()
    {
        var file = MakeExcelFile();
        var svc = new FileParserService();

        var rows = svc.Parse(file);
        var first = rows[0];

        Assert.Equal("W001", first.WorkerId);
        Assert.Equal("John Smith", first.WorkerName);
        Assert.Equal("SiteA", first.Site);
        Assert.Equal(22, first.DaysPresent);
        Assert.Equal(85.00m, first.DayRate);
        Assert.Equal(200.00m, first.AdvanceDeduction);
    }

    [Fact]
    public void Parse_ShouldThrow_ForUnsupportedExtension()
    {
        var file = MakeCsvFile("data", "report.pdf");
        var svc = new FileParserService();

        Assert.Throws<InvalidOperationException>(() => svc.Parse(file));
    }
}
