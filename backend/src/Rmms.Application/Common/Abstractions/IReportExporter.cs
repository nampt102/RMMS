namespace Rmms.Application.Common.Abstractions;

/// <summary>A tabular dataset to render into a spreadsheet (M15 reports).</summary>
public sealed record ReportSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>A generated download (bytes + filename + content type).</summary>
public sealed record ReportFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Renders a <see cref="ReportSheet"/> to an .xlsx file (M15, ClosedXML in Infrastructure).
/// Reports build a sheet of strings and hand it here, so the export format is centralised.
/// </summary>
public interface IReportExporter
{
    ReportFile ToXlsx(ReportSheet sheet, string fileName);
}
