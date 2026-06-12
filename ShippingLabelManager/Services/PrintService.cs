using ShippingLabelManager.Models;
using System.Drawing;
using System.Drawing.Printing;

namespace ShippingLabelManager.Services;

public static class PrintService
{
    public static void PrintLabels(List<LabelRow> rows, LabelSize size, string printerName = "")
    {
        int currentIndex = 0;

        var doc = new PrintDocument();
        if (!string.IsNullOrEmpty(printerName))
            doc.PrinterSettings.PrinterName = printerName;

        // Paper size in hundredths of an inch
        var (pageW, pageH) = size == LabelSize.Size2x4
            ? (200, 400)   // 2" x 4"
            : (400, 600);  // 4" x 6"

        doc.DefaultPageSettings.PaperSize = new PaperSize("Custom", pageW, pageH);
        doc.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
        doc.DocumentName = "Shipping Labels";

        doc.PrintPage += (sender, e) =>
        {
            if (e.Graphics == null || currentIndex >= rows.Count) return;
            DrawLabel(e.Graphics, rows[currentIndex], size, e.MarginBounds);
            currentIndex++;
            e.HasMorePages = currentIndex < rows.Count;
        };

        doc.Print();
    }

    public static void PreviewLabels(List<LabelRow> rows, LabelSize size, Form owner)
    {
        int currentIndex = 0;
        var doc = new PrintDocument();
        var (pageW, pageH) = size == LabelSize.Size2x4 ? (200, 400) : (400, 600);
        doc.DefaultPageSettings.PaperSize = new PaperSize("Custom", pageW, pageH);
        doc.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
        doc.DocumentName = "Shipping Labels";
        doc.PrintPage += (sender, e) =>
        {
            if (e.Graphics == null || currentIndex >= rows.Count) return;
            DrawLabel(e.Graphics, rows[currentIndex], size, e.MarginBounds);
            currentIndex++;
            e.HasMorePages = currentIndex < rows.Count;
        };

        var preview = new PrintPreviewDialog
        {
            Document = doc,
            WindowState = FormWindowState.Maximized,
            Text = "Label Preview"
        };
        preview.ShowDialog(owner);
    }

    private static void DrawLabel(Graphics g, LabelRow row, LabelSize size, Rectangle bounds)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        bool is4x6 = size == LabelSize.Size4x6;
        float scale = is4x6 ? 1.8f : 1.0f;

        var fontBig    = new Font("Arial", 11 * scale, FontStyle.Bold);
        var fontMed    = new Font("Arial", 9  * scale, FontStyle.Regular);
        var fontSmall  = new Font("Arial", 7  * scale, FontStyle.Regular);
        var fontMono   = new Font("Courier New", 8 * scale, FontStyle.Regular);
        var black      = Brushes.Black;
        var gray       = Brushes.DimGray;
        var pen        = new Pen(Color.Black, 1f);
        var thinPen    = new Pen(Color.LightGray, 0.5f);

        float x = bounds.Left, y = bounds.Top;
        float w = bounds.Width;

        // ── Border ──────────────────────────────────────────
        g.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

        // ── Serial number (top right) ────────────────────────
        var serialStr = row.SerialInterleaved.ToString("D9");
        var serialSize = g.MeasureString(serialStr, fontBig);
        g.DrawString(serialStr, fontBig, Brushes.DarkBlue,
            x + w - serialSize.Width - 4, y + 4);

        // ── LIBCODE (top left) ───────────────────────────────
        g.DrawString(row.LIBCODE, fontMed, gray, x + 4, y + 4);

        float lineY = y + serialSize.Height + 8;
        g.DrawLine(thinPen, x + 2, lineY, x + w - 2, lineY);
        lineY += 4;

        // ── Address block ────────────────────────────────────
        var addrLines = new[] { row.SHIP_ADD_1, row.SHIP_ADD_2, row.SHIP_ADD_3, row.SHIP_ADD_4 }
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        foreach (var line in addrLines)
        {
            g.DrawString(line, is4x6 ? fontMed : fontSmall, black, x + 6, lineY);
            lineY += g.MeasureString(line, is4x6 ? fontMed : fontSmall).Height + 1;
        }

        // ── Separator ────────────────────────────────────────
        lineY += 4;
        g.DrawLine(thinPen, x + 2, lineY, x + w - 2, lineY);
        lineY += 4;

        // ── Barcode data fields ──────────────────────────────
        var barcodeFields = new (string Label, string Value)[]
        {
            ("MID9",    row.MID9),
            ("TC",      row.TrackingCore),
            ("HR",      row.HR_Text),
        };

        foreach (var (label, value) in barcodeFields)
        {
            var labelStr = label + ": ";
            var labelW   = g.MeasureString(labelStr, fontSmall).Width;
            g.DrawString(labelStr, fontSmall, gray, x + 4, lineY);
            g.DrawString(value, fontMono, black, x + 4 + labelW, lineY);
            lineY += g.MeasureString(labelStr, fontSmall).Height + 1;
        }

        // ── ZIP / STC / ChannelAI footer ─────────────────────
        lineY += 2;
        g.DrawLine(thinPen, x + 2, lineY, x + w - 2, lineY);
        lineY += 3;
        var footer = $"ZIP: {row.ZIP9}   STC: {row.STC}   ChannelAI: {row.ChannelAI}   MOD10: {row.MOD10}";
        g.DrawString(footer, fontSmall, gray, x + 4, lineY);

        fontBig.Dispose(); fontMed.Dispose(); fontSmall.Dispose(); fontMono.Dispose(); pen.Dispose(); thinPen.Dispose();
    }

    public static List<string> GetInstalledPrinters()
    {
        var list = new List<string>();
        foreach (string p in PrinterSettings.InstalledPrinters)
            list.Add(p);
        return list;
    }
}
