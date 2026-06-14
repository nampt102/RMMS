using ClosedXML.Excel;
using Rmms.Application.Common.Abstractions;

namespace Rmms.Infrastructure.Reports;

/// <summary>
/// ClosedXML implementation of <see cref="IReportExporter"/> (M15). Renders a string sheet to a
/// single-worksheet .xlsx with a bold header row and auto-fit columns. Synchronous + in-memory —
/// suitable for the Phase 1 report sizes (≤ ~100k rows; async/Hangfire export is a Phase 2 item).
/// </summary>
internal sealed class ClosedXmlReportExporter : IReportExporter
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ReportFile ToXlsx(ReportSheet sheet, string fileName)
    {
        using var wb = new XLWorkbook();
        var name = string.IsNullOrWhiteSpace(sheet.Name) ? "Report" : Sanitize(sheet.Name);
        var ws = wb.AddWorksheet(name);

        for (var c = 0; c < sheet.Headers.Count; c++)
        {
            ws.Cell(1, c + 1).Value = sheet.Headers[c];
        }
        ws.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var row = sheet.Rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                ws.Cell(r + 2, c + 1).Value = row[c];
            }
        }

        if (sheet.Headers.Count > 0)
        {
            ws.Columns(1, sheet.Headers.Count).AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var safeName = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.xlsx";
        return new ReportFile(safeName, XlsxContentType, ms.ToArray());
    }

    // Excel worksheet names cannot exceed 31 chars or contain : \ / ? * [ ].
    private static string Sanitize(string name)
    {
        var cleaned = new string(name.Where(ch => ch is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray());
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
