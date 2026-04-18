using ShippingLabelManager.Models;
using System.Text;

namespace ShippingLabelManager.Services;

public static class BarcodeService
{
    /// <summary>
    /// Remove dash from ZIP-9 → ZIP9
    /// </summary>
    public static string StripDash(string zip9Dash) =>
        (zip9Dash ?? "").Replace("-", "");

    /// <summary>
    /// Build TrackingCore = STC(2) + ZIP9(3) + Serial(9) + MID9(7)
    /// </summary>
    public static string BuildTrackingCore(Site site, int serial)
    {
        var stc    = (site.STC ?? "").PadLeft(2, '0')[..Math.Min(2, (site.STC ?? "").PadLeft(2,'0').Length)];
        var zip9   = StripDash(site.ZIP9Dash).PadLeft(3, '0');
        if (zip9.Length > 3) zip9 = zip9[^3..];
        var ser    = serial.ToString().PadLeft(9, '0');
        if (ser.Length > 9) ser = ser[^9..];
        var mid    = (site.MID9 ?? "").PadLeft(7, '0');
        if (mid.Length > 7) mid = mid[^7..];
        return stc + zip9 + ser + mid;
    }

    /// <summary>
    /// GS1 MOD10 check digit — matches your VBA MOD10CheckDigit function exactly
    /// </summary>
    public static int Mod10CheckDigit(string input)
    {
        long sum = 0;
        for (int i = input.Length - 1; i >= 0; i--)
        {
            if (!int.TryParse(input[i].ToString(), out int digit)) continue;
            int weight = ((input.Length - 1 - i) % 2 == 0) ? 3 : 1;
            sum += digit * weight;
        }
        return (int)((10 - (sum % 10)) % 10);
    }

    /// <summary>
    /// GS1_128_Raw = "420" + ZIP9 + "\F" + TrackingCore + MOD10
    /// </summary>
    public static string BuildGS1Raw(string zip9, string trackingCore, int mod10) =>
        "420" + zip9 + @"\F" + trackingCore + mod10;

    /// <summary>
    /// HR_Text = last 22 chars of GS1_128_Raw split into groups of 4 with spaces
    /// Matches your Excel: TEXTJOIN(" ",TRUE, MID(RIGHT(O2,22),1,4), ...
    /// </summary>
    public static string BuildHRText(string gs1Raw)
    {
        var last22 = gs1Raw.Length >= 22 ? gs1Raw[^22..] : gs1Raw.PadLeft(22);
        var parts = new List<string>();
        for (int i = 0; i < 22; i += 4)
        {
            var part = last22.Substring(i, Math.Min(4, 22 - i));
            if (!string.IsNullOrEmpty(part)) parts.Add(part);
        }
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Build a complete LabelRow for a given site + serial number
    /// </summary>
    public static LabelRow BuildLabelRow(Site site, int serial)
    {
        var zip9         = StripDash(site.ZIP9Dash);
        var trackingCore = BuildTrackingCore(site, serial);
        var mod10        = Mod10CheckDigit(trackingCore);
        var gs1Raw       = BuildGS1Raw(zip9, trackingCore, mod10);
        var hrText       = BuildHRText(gs1Raw);

        return new LabelRow
        {
            LIBCODE           = site.LIBCODE ?? "",
            SHIP_ADD_1        = site.SHIP_ADD_1 ?? "",
            SHIP_ADD_2        = site.SHIP_ADD_2 ?? "",
            SHIP_ADD_3        = site.SHIP_ADD_3 ?? "",
            SHIP_ADD_4        = site.SHIP_ADD_4 ?? "",
            ZIP9Dash          = site.ZIP9Dash ?? "",
            SerialInterleaved = serial,
            ChannelAI         = site.ChannelAI ?? "",
            STC               = site.STC ?? "",
            MID9              = site.MID9 ?? "",
            ZIP9              = zip9,
            TrackingCore      = trackingCore,
            MOD10             = mod10,
            GS1_128_Raw       = gs1Raw,
            HR_Text           = hrText
        };
    }

    /// <summary>
    /// Build all rows for a batch
    /// </summary>
    public static List<LabelRow> BuildBatch(Site site, int fromSerial, int count)
    {
        var rows = new List<LabelRow>(count);
        for (int i = 0; i < count; i++)
            rows.Add(BuildLabelRow(site, fromSerial + i));
        return rows;
    }

    /// <summary>
    /// Export rows to CSV string with exact column order
    /// </summary>
    public static string ToCsv(List<LabelRow> rows)
    {
        var headers = new[]
        {
            "LIBCODE","SHIP_ADD_1","SHIP_ADD_2","SHIP_ADD_3","SHIP_ADD_4",
            "ZIP-9","SerialInterleaved","ChannelAI","STC","MID9","ZIP9",
            "TrackingCore","MOD10","GS1_128_Raw","HR_Text"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(QuoteCsv)));

        foreach (var r in rows)
        {
            var cols = new object[]
            {
                r.LIBCODE, r.SHIP_ADD_1, r.SHIP_ADD_2, r.SHIP_ADD_3, r.SHIP_ADD_4,
                r.ZIP9Dash, r.SerialInterleaved, r.ChannelAI, r.STC, r.MID9, r.ZIP9,
                r.TrackingCore, r.MOD10, r.GS1_128_Raw, r.HR_Text
            };
            sb.AppendLine(string.Join(",", cols.Select(v => QuoteCsv(v?.ToString() ?? ""))));
        }
        return sb.ToString();
    }

    private static string QuoteCsv(string v) =>
        "\"" + v.Replace("\"", "\"\"") + "\"";

    // ── Shipping label CSV ────────────────────────────────────

    /// <summary>
    /// Builds a CSV for outbound shipping labels with full barcodes.
    /// One row per box (global serial offset across all entries).
    /// Barcode credentials come from shippingSite; address from each returnSite.
    /// </summary>
    public static string ToShippingCsv(
        List<(Site returnSite, int boxes)> entries,
        ShippingLabelManager.Models.ShippingSite shippingSite,
        int firstSerial)
    {
        var headers = new[]
        {
            "Ord", "LIBCODE", "SiteName",
            "SHIP_ADD_1", "SHIP_ADD_2", "SHIP_ADD_3", "SHIP_ADD_4",
            "ZIP-9", "SerialInterleaved", "ChannelAI", "STC", "MID9", "ZIP9",
            "TrackingCore", "MOD10", "GS1_128_Raw", "HR_Text", "NumRecs"
        };

        int totalBoxes = entries.Sum(e => e.boxes);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(QuoteCsv)));

        int ord          = 0;
        int serialOffset = 0;

        foreach (var (returnSite, boxes) in entries)
        {
            // Temp site: shipper credentials + destination ZIP for barcode generation
            var tempSite = new Site
            {
                STC       = shippingSite.STC,
                MID9      = shippingSite.MID9 ?? "",
                ChannelAI = shippingSite.ChannelAI,
                ZIP9Dash  = returnSite.ZIP9Dash
            };

            for (int b = 0; b < boxes; b++)
            {
                ord++;
                int serial   = firstSerial + serialOffset++;
                var labelRow = BuildLabelRow(tempSite, serial);

                var cols = new object[]
                {
                    ord,
                    returnSite.LIBCODE    ?? "",
                    returnSite.SiteName   ?? "",
                    returnSite.SHIP_ADD_1 ?? "",
                    returnSite.SHIP_ADD_2 ?? "",
                    returnSite.SHIP_ADD_3 ?? "",
                    returnSite.SHIP_ADD_4 ?? "",
                    returnSite.ZIP9Dash   ?? "",
                    labelRow.SerialInterleaved,
                    labelRow.ChannelAI,
                    labelRow.STC,
                    labelRow.MID9,
                    labelRow.ZIP9,
                    labelRow.TrackingCore,
                    labelRow.MOD10,
                    labelRow.GS1_128_Raw,
                    labelRow.HR_Text,
                    totalBoxes
                };
                sb.AppendLine(string.Join(",", cols.Select(v => QuoteCsv(v?.ToString() ?? ""))));
            }
        }
        return sb.ToString();
    }
}
