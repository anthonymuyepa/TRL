using ShippingLabelManager.Models;

namespace ShippingLabelManager.Services;

public class CsvImportService
{
    public static readonly string[] TemplateHeaders =
    {
        "LIBCODE", "SiteName", "SHIP_ADD_1", "SHIP_ADD_2", "SHIP_ADD_3", "SHIP_ADD_4",
        "ZIP-9", "MID9", "STC", "ChannelAI"
    };

    public enum ImportMode { InsertOnly, Upsert }

    public class ImportResult
    {
        public int Imported { get; set; }
        public int Updated  { get; set; }
        public int Skipped  { get; set; }
        public int Errors   { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
        public List<string> SkippedCodes  { get; set; } = new();
    }

    /// <summary>
    /// Parses CSV and returns:
    ///   toInsert — new sites not in the database
    ///   toUpdate — existing sites with changed fields (original + updated copy)
    /// Mode.InsertOnly skips existing LIBCODEs (original behaviour)
    /// Mode.Upsert returns them in toUpdate for preview/confirmation
    /// </summary>
    public static (List<Site> toInsert, List<(Site existing, Site updated)> toUpdate, ImportResult result) Parse(
        string filePath, Dictionary<string, Site> existingSites, ImportMode mode = ImportMode.Upsert)
    {
        var result   = new ImportResult();
        var toInsert = new List<Site>();
        var toUpdate = new List<(Site, Site)>();
        var seenCodes = new HashSet<string>(); // prevent duplicates within the same file

        var lines = File.ReadAllLines(filePath);
        if (lines.Length < 2)
        {
            result.ErrorMessages.Add("File is empty or has no data rows.");
            return (toInsert, toUpdate, result);
        }

        var headers = SplitCsvRow(lines[0]).Select(h => h.Trim().Trim('"')).ToList();

        int Col(string name) => headers.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));

        int cLib  = Col("LIBCODE");
        int cName = Col("SiteName");
        int cA1   = Col("SHIP_ADD_1");
        int cA2   = Col("SHIP_ADD_2");
        int cA3   = Col("SHIP_ADD_3");
        int cA4   = Col("SHIP_ADD_4");
        int cZip  = Col("ZIP-9");
        int cMid  = Col("MID9");
        int cStc  = Col("STC");
        int cChan = Col("ChannelAI");

        if (cLib < 0 || cMid < 0 || cZip < 0)
        {
            result.ErrorMessages.Add("CSV is missing required columns: LIBCODE, MID9, ZIP-9.");
            return (toInsert, toUpdate, result);
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var cols = SplitCsvRow(line).Select(c => c.Trim().Trim('"')).ToList();
                string Get(int idx) => idx >= 0 && idx < cols.Count ? cols[idx] : "";

                var libcode = Get(cLib);
                if (string.IsNullOrEmpty(libcode))
                {
                    result.Errors++;
                    result.ErrorMessages.Add("Row " + (i + 1) + ": LIBCODE is empty — skipped.");
                    continue;
                }

                var mid9 = Get(cMid);
                var zip9 = Get(cZip);

                if (string.IsNullOrEmpty(mid9) || string.IsNullOrEmpty(zip9))
                {
                    result.Errors++;
                    result.ErrorMessages.Add("Row " + (i + 1) + " (" + libcode + "): MID9 or ZIP-9 is empty — skipped.");
                    continue;
                }

                if (seenCodes.Contains(libcode.ToUpper()))
                {
                    result.Errors++;
                    result.ErrorMessages.Add("Row " + (i + 1) + " (" + libcode + "): duplicate LIBCODE in file — skipped.");
                    continue;
                }
                seenCodes.Add(libcode.ToUpper());

                var newSite = new Site
                {
                    LIBCODE    = libcode,
                    SiteName   = cName >= 0 && !string.IsNullOrEmpty(Get(cName)) ? Get(cName) : libcode,
                    SHIP_ADD_1 = Get(cA1),
                    SHIP_ADD_2 = Get(cA2),
                    SHIP_ADD_3 = Get(cA3),
                    SHIP_ADD_4 = Get(cA4),
                    ZIP9Dash   = zip9,
                    MID9       = mid9,
                    STC        = Get(cStc),
                    ChannelAI  = Get(cChan),
                    CurrentSequence = 1
                };

                if (existingSites.TryGetValue(libcode.ToUpper(), out var existing))
                {
                    if (mode == ImportMode.InsertOnly)
                    {
                        result.Skipped++;
                        result.SkippedCodes.Add(libcode);
                    }
                    else
                    {
                        // Upsert — carry over sequence so we don't reset it
                        newSite.Id              = existing.Id;
                        newSite.CurrentSequence = existing.CurrentSequence;
                        newSite.LastPrintedSerial = existing.LastPrintedSerial;
                        newSite.LastPrintedDate   = existing.LastPrintedDate;
                        toUpdate.Add((existing, newSite));
                    }
                }
                else
                {
                    toInsert.Add(newSite);
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.ErrorMessages.Add("Row " + (i + 1) + ": " + ex.Message);
            }
        }

        return (toInsert, toUpdate, result);
    }

    public static string GenerateTemplate()
    {
        var header = string.Join(",", TemplateHeaders.Select(h => "\"" + h + "\""));
        var sample = "\"LIB001\",\"Calgary North Branch\",\"Recipient Name\",\"123 Main St\",\"Calgary AB\",\"\",\"12345-6789\",\"123456789\",\"70\",\"420\"";
        return header + "\r\n" + sample + "\r\n";
    }

    private static List<string> SplitCsvRow(string row)
    {
        var result  = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }
}
