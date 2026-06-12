using System.Text.Json;
using ShippingLabelManager.Models;

namespace ShippingLabelManager.Services;

public static class AppSettings
{
    private static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static string TempFolder            { get; set; } = Path.GetTempPath();
    public static List<WordTemplate> WordTemplates { get; set; } = new();
    public static string ShippingTemplatePath  { get; set; } = "";
    public static string ConnectionString      { get; set; } = @"Server=.\SQLEXPRESS;Database=ShippingLabels;Integrated Security=true;TrustServerCertificate=true;";
    public static string AdminPin              { get; set; } = "";

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ConnectionString",    out var cs))  ConnectionString      = cs.GetString() ?? ConnectionString;
            if (root.TryGetProperty("TempFolder",           out var tf))  TempFolder            = tf.GetString() ?? TempFolder;
            if (root.TryGetProperty("ShippingTemplatePath", out var st))  ShippingTemplatePath  = st.GetString() ?? ShippingTemplatePath;
            if (root.TryGetProperty("AdminPin",             out var ap))  AdminPin              = ap.GetString() ?? AdminPin;

            // Load template list
            if (root.TryGetProperty("WordTemplates", out var wt) && wt.ValueKind == JsonValueKind.Array)
            {
                WordTemplates = new List<WordTemplate>();
                foreach (var elem in wt.EnumerateArray())
                {
                    var name = elem.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                    var path = elem.TryGetProperty("Path", out var p) ? p.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(path))
                        WordTemplates.Add(new WordTemplate { Name = name, Path = path });
                }
            }
            // Backward compatibility: migrate old single WordTemplatePath into the list
            else if (root.TryGetProperty("WordTemplatePath", out var oldWt))
            {
                var oldPath = oldWt.GetString() ?? "";
                if (!string.IsNullOrEmpty(oldPath))
                    WordTemplates.Add(new WordTemplate { Name = "Default template", Path = oldPath });
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            var obj = new
            {
                ConnectionString,
                TempFolder,
                WordTemplates = WordTemplates.Select(t => new { t.Name, t.Path }).ToList(),
                ShippingTemplatePath,
                AdminPin
            };
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
