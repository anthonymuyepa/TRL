using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Alias away the clashes between OpenXml and QuestPDF/System.Drawing
using OxFont   = DocumentFormat.OpenXml.Spreadsheet.Font;
using OxFonts  = DocumentFormat.OpenXml.Spreadsheet.Fonts;
using OxColor  = DocumentFormat.OpenXml.Spreadsheet.Color;
using PdfColors = QuestPDF.Helpers.Colors;

namespace ShippingLabelManager.Services;

public static class ReportExportService
{
    // ── CSV ──────────────────────────────────────────────────

    public static void ExportToCsv<T>(
        IEnumerable<T> data,
        string[] headers,
        Func<T, string[]> rowMapper,
        string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(h => "\"" + h + "\"")));
        foreach (var item in data)
        {
            var cols = rowMapper(item);
            sb.AppendLine(string.Join(",",
                cols.Select(c => "\"" + (c ?? "").Replace("\"", "\"\"") + "\"")));
        }
        File.WriteAllText(filePath, sb.ToString());
    }

    // ── Excel ─────────────────────────────────────────────────

    public static void ExportToExcel<T>(
        IEnumerable<T> data,
        string[] headers,
        Func<T, string[]> rowMapper,
        string filePath,
        string sheetName = "Report")
    {
        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);

        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = BuildStylesheet();
        stylesPart.Stylesheet.Save();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData     = new SheetData();
        worksheetPart.Worksheet = new Worksheet(sheetData);

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id      = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name    = sheetName
        });

        // Header row
        var headerRow = new Row { RowIndex = 1 };
        for (int i = 0; i < headers.Length; i++)
            headerRow.AppendChild(MakeCell(CellRef(i, 1), headers[i], styleIndex: 1));
        sheetData.AppendChild(headerRow);

        // Data rows
        uint rowIdx = 2;
        foreach (var item in data)
        {
            var cols  = rowMapper(item);
            var style = (uint)(rowIdx % 2 == 0 ? 2 : 0);
            var row   = new Row { RowIndex = rowIdx };
            for (int i = 0; i < cols.Length; i++)
                row.AppendChild(MakeCell(CellRef(i, (int)rowIdx), cols[i] ?? "", styleIndex: style));
            sheetData.AppendChild(row);
            rowIdx++;
        }

        workbookPart.Workbook.Save();
    }

    private static Cell MakeCell(string cellRef, string value, uint styleIndex = 0) => new Cell
    {
        CellReference = cellRef,
        DataType      = CellValues.InlineString,
        InlineString  = new InlineString(new Text(value)),
        StyleIndex    = styleIndex
    };

    private static string CellRef(int colIndex, int rowIndex)
    {
        string col = "";
        int c = colIndex;
        while (c >= 0) { col = (char)('A' + c % 26) + col; c = c / 26 - 1; }
        return col + rowIndex;
    }

    private static Stylesheet BuildStylesheet()
    {
        return new Stylesheet(
            new OxFonts(
                new OxFont(),                                                                        // 0 — normal
                new OxFont(new Bold(), new OxColor { Rgb = new HexBinaryValue("FFFFFFFF") })        // 1 — bold white
            ),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),                     // 0 — none
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),                   // 1 — required
                new Fill(new PatternFill(                                                            // 2 — blue header
                    new ForegroundColor { Rgb = new HexBinaryValue("FF005AA0") })
                    { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(                                                            // 3 — alt row
                    new ForegroundColor { Rgb = new HexBinaryValue("FFF8FAFC") })
                    { PatternType = PatternValues.Solid })
            ),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0, FormatId = 0 },             // 0 — normal
                new CellFormat { FontId = 1, FillId = 2, BorderId = 0, FormatId = 0, ApplyFont = true, ApplyFill = true }, // 1 — header
                new CellFormat { FontId = 0, FillId = 3, BorderId = 0, FormatId = 0, ApplyFill = true }  // 2 — alt row
            )
        );
    }

    // ── PDF ───────────────────────────────────────────────────

    public static void ExportToPdf<T>(
        IEnumerable<T> data,
        string[] headers,
        Func<T, string[]> rowMapper,
        string filePath,
        string reportTitle,
        string subtitle)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var rows = data.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontFamily("Segoe UI").FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("TRL (Track Return Labels)  —  " + reportTitle)
                        .FontSize(14).Bold().FontColor("#005AA0");
                    col.Item().Text(subtitle)
                        .FontSize(9).FontColor("#505050");
                    col.Item().Text("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") +
                                    "  by " + Environment.UserName)
                        .FontSize(8).FontColor("#808080");
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#D2D7E1");
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < headers.Length; i++)
                            cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                            header.Cell().Background("#005AA0").Padding(5)
                                .Text(h).FontSize(9).Bold().FontColor(PdfColors.White);
                    });

                    int rowIdx = 0;
                    foreach (var item in rows)
                    {
                        var cols = rowMapper(item);
                        var bg   = rowIdx % 2 == 0 ? "#FFFFFF" : "#F8FAFC";
                        foreach (var c in cols)
                            table.Cell().Background(bg).Padding(4).Text(c ?? "").FontSize(8);
                        rowIdx++;
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                    x.Span(" of ").FontSize(8);
                    x.TotalPages().FontSize(8);
                    x.Span("  —  Tadala Technologies").FontSize(8).FontColor("#808080");
                });
            });
        }).GeneratePdf(filePath);
    }
}
