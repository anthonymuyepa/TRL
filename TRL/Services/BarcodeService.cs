using TRL.Models;
using System.Text;

namespace TRL.Services;

public static class BarcodeService
{
    public static string StripDash(string zip9Dash) =>
        (zip9Dash ?? "").Replace("-", "");

    public static LabelRow BuildCalibrationLabelRow(Site site, int serial, int calibrationNumber)
    {
        var row = BuildLabelRow(site, serial);
        row.LIBCODE = "CAL";
        row.SHIP_ADD_1 = "CALIBRATION LABEL";
        row.SHIP_ADD_2 = "DO NOT MAIL";
        row.SHIP_ADD_3 = "TEST PRINT ONLY";
        row.SHIP_ADD_4 = $"CALIBRATION #{calibrationNumber:D2}";
        return row;
    }

    public static string BuildTrackingCore(Site site, int serial)
    {
        var channelAI = (site.ChannelAI ?? "92").PadLeft(2, '0');
        var stc = (site.STC ?? "").PadLeft(3, '0');
        if (stc.Length > 3) stc = stc[^3..];
        var mid = (site.MID9 ?? "").PadLeft(9, '0');
        if (mid.Length > 9) mid = mid[^9..];
        var ser = serial.ToString().PadLeft(7, '0');
        if (ser.Length > 7) ser = ser[^7..];
        return channelAI + stc + mid + ser;
    }

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

    public static string BuildGS1Raw(string zip9, string trackingCore, int mod10) =>
        "420" + zip9 + @"\F" + trackingCore + mod10;

    public static string BuildHRText(string gs1Raw)
    {
        var marker = @"\F";
        var idx = gs1Raw.IndexOf(marker, StringComparison.Ordinal);
        var tracking = idx >= 0 ? gs1Raw[(idx + marker.Length)..] : gs1Raw;
        if (tracking.Length > 22) tracking = tracking[^22..];
        tracking = tracking.PadLeft(22, '0');
        return string.Join(" ", new[]
        {
            tracking.Substring(0, 4),
            tracking.Substring(4, 4),
            tracking.Substring(8, 4),
            tracking.Substring(12, 4),
            tracking.Substring(16, 4),
            tracking.Substring(20, 2)
        });
    }

    public static LabelRow BuildLabelRow(Site site, int serial)
    {
        var zip9 = StripDash(site.ZIP9Dash);
        var trackingCore = BuildTrackingCore(site, serial);
        var mod10 = Mod10CheckDigit(trackingCore);
        var gs1Raw = BuildGS1Raw(zip9, trackingCore, mod10);
        var hrText = BuildHRText(gs1Raw);

        return new LabelRow
        {
            LIBCODE = site.LIBCODE ?? "",
            SHIP_ADD_1 = site.SHIP_ADD_1 ?? "",
            SHIP_ADD_2 = site.SHIP_ADD_2 ?? "",
            SHIP_ADD_3 = site.SHIP_ADD_3 ?? "",
            SHIP_ADD_4 = site.SHIP_ADD_4 ?? "",
            ZIP9Dash = site.ZIP9Dash ?? "",
            SerialInterleaved = serial,
            ChannelAI = site.ChannelAI ?? "",
            STC = site.STC ?? "",
            MID9 = site.MID9 ?? "",
            ZIP9 = zip9,
            TrackingCore = trackingCore,
            MOD10 = mod10,
            GS1_128_Raw = gs1Raw,
            HR_Text = hrText
        };
    }

    public static string ToShippingCsv(
        List<(Site site, int boxes)> entries, 
        ShippingSite activeSite, 
        int firstSerial, 
        int calibrationCount = 20)
    {
        var sb = new StringBuilder();
        
        // Write header
        sb.AppendLine("\"LIBCODE\",\"SiteName\",\"SHIP_ADD_1\",\"SHIP_ADD_2\",\"SHIP_ADD_3\",\"SHIP_ADD_4\",\"TrackingNumber\",\"MID9\",\"STC\",\"ZIP9Dash\",\"GS1_128_Raw\",\"HR_Text\"");
        
        // Write calibration rows first
        for (int i = 1; i <= calibrationCount; i++)
        {
            // Generate realistic calibration barcode
            string zip9 = StripDash(activeSite.ZIP9Dash ?? "123456789");
            string trackingCore = BuildTrackingCoreForCalibration(i);
            int mod10 = Mod10CheckDigit(trackingCore);
            string calGS1 = BuildGS1Raw(zip9, trackingCore, mod10);
            string calHR = BuildHRText(calGS1);
            
            sb.AppendLine($"\"CAL\",\"CALIBRATION LABEL\",\"CALIBRATION LABEL\",\"DO NOT MAIL\",\"TEST PRINT ONLY\",\"\",\"CAL{i:D6}\",\"{EscapeCsv(activeSite.MID9)}\",\"{EscapeCsv(activeSite.STC)}\",\"{EscapeCsv(activeSite.ZIP9Dash)}\",\"{calGS1}\",\"{calHR}\"");
        }
        
        // Write real shipping labels
        int currentSerial = firstSerial;
        foreach (var (site, boxes) in entries)
        {
            for (int i = 0; i < boxes; i++)
            {
                // Build the label row with proper barcodes
                var labelRow = BuildLabelRow(site, currentSerial);
                
                sb.AppendLine($"\"{EscapeCsv(site.LIBCODE)}\",\"{EscapeCsv(site.SiteName)}\",\"{EscapeCsv(site.SHIP_ADD_1)}\",\"{EscapeCsv(site.SHIP_ADD_2)}\",\"{EscapeCsv(site.SHIP_ADD_3)}\",\"{EscapeCsv(site.SHIP_ADD_4)}\",\"{activeSite.MID9}{currentSerial:D9}\",\"{EscapeCsv(activeSite.MID9)}\",\"{EscapeCsv(activeSite.STC)}\",\"{EscapeCsv(activeSite.ZIP9Dash)}\",\"{labelRow.GS1_128_Raw}\",\"{labelRow.HR_Text}\"");
                currentSerial++;
            }
        }
        
        return sb.ToString();
    }

    private static string BuildTrackingCoreForCalibration(int calNumber)
    {
        // Build a valid tracking core for calibration labels
        string channelAI = "92";
        string stc = "001";
        string mid = "CAL000001";
        string ser = calNumber.ToString().PadLeft(7, '0');
        return channelAI + stc + mid + ser;
    }

    // Return Labels support
    public static List<LabelRow> BuildBatch(Site site, int fromSerial, int count, int calibrationCount = 0)
    {
        var rows = new List<LabelRow>(count + calibrationCount);
        int serial = fromSerial;
        for (int i = 1; i <= calibrationCount; i++) { rows.Add(BuildCalibrationLabelRow(site, serial, i)); serial++; }
        for (int i = 0; i < count; i++) { rows.Add(BuildLabelRow(site, serial)); serial++; }
        return rows;
    }

    public static string ToCsv(List<LabelRow> rows)
    {
        var headers = new[] { "LIBCODE","SHIP_ADD_1","SHIP_ADD_2","SHIP_ADD_3","SHIP_ADD_4",
            "ZIP-9","SerialInterleaved","ChannelAI","STC","MID9","ZIP9",
            "TrackingCore","MOD10","GS1_128_Raw","HR_Text" };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(QuoteCsv)));

        foreach (var r in rows)
        {
            var cols = new object[] { r.LIBCODE, r.SHIP_ADD_1, r.SHIP_ADD_2, r.SHIP_ADD_3, r.SHIP_ADD_4,
                r.ZIP9Dash, r.SerialInterleaved, r.ChannelAI, r.STC, r.MID9, r.ZIP9,
                r.TrackingCore, r.MOD10, r.GS1_128_Raw, r.HR_Text };
            sb.AppendLine(string.Join(",", cols.Select(v => QuoteCsv(v?.ToString() ?? ""))));
        }
        return sb.ToString();
    }

    private static string EscapeCsv(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    private static string QuoteCsv(string v) =>
        "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";


}