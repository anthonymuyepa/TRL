namespace TRL.Models;

public enum LabelSize { Size2x4, Size4x6 }

public class WordTemplate
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public class Site
{
    public int Id { get; set; }
    public string LIBCODE { get; set; } = "";
    public string SiteName { get; set; } = "";
    public string? SHIP_ADD_1 { get; set; }
    public string? SHIP_ADD_2 { get; set; }
    public string? SHIP_ADD_3 { get; set; }
    public string? SHIP_ADD_4 { get; set; }
    public string ZIP9Dash { get; set; } = "";        // e.g. 12345-6789
    public string MID9 { get; set; } = "";
    public string? STC { get; set; }
    public string? ChannelAI { get; set; }
    public int CurrentSequence { get; set; } = 1;
    public int? LastPrintedSerial { get; set; }
    public DateTime? LastPrintedDate { get; set; }
    public int TotalLabels { get; set; }
    public int LastShippingBoxQty { get; set; }

    // Derived
    public string ZIP9 => ZIP9Dash.Replace("-", "");
    public string DisplayName => $"{SiteName}  [{LIBCODE}]";
    public string LastPrintedDisplay => LastPrintedSerial.HasValue
        ? $"{LastPrintedSerial:D9}  ({LastPrintedDate:yyyy-MM-dd})"
        : "None yet";
}

public class PrintHistoryEntry
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string LIBCODE { get; set; } = "";
    public string SiteName { get; set; } = "";
    public int SerialFrom { get; set; }
    public int SerialTo { get; set; }
    public int LabelCount { get; set; }
    public string RunType { get; set; } = "";
    public string? LabelSize { get; set; }
    public DateTime PrintedAt { get; set; }
    public string? PrintedBy { get; set; }
    public string? TemplateName { get; set; }

    public string SerialRange => LabelCount > 1
        ? $"{SerialFrom:D9} → {SerialTo:D9}"
        : $"{SerialFrom:D9}";
}

public class ShippingSite
{
    public int Id { get; set; }
    public string SiteCode { get; set; } = "";
    public string SiteName { get; set; } = "";
    public string? SHIP_ADD_1 { get; set; }
    public string? SHIP_ADD_2 { get; set; }
    public string? SHIP_ADD_3 { get; set; }
    public string? SHIP_ADD_4 { get; set; }
    public string ZIP9Dash { get; set; } = "";
    public string MID9 { get; set; } = "";
    public string? STC { get; set; }
    public string? ChannelAI { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DisplayName => $"{SiteName}  [{SiteCode}]";
}

public class ReturnLabelReport
{
    public DateTime PrintedAt    { get; set; }
    public string   LIBCODE      { get; set; } = "";
    public string   SiteName     { get; set; } = "";
    public int      SerialFrom   { get; set; }
    public int      SerialTo     { get; set; }
    public int      LabelCount   { get; set; }
    public string   RunType      { get; set; } = "";
    public string?  TemplateName { get; set; }
    public string?  PrintedBy    { get; set; }
    public string?  LabelSize    { get; set; }

    public string FirstGS1_128_Raw { get; set; } = "";
    public string LastGS1_128_Raw  { get; set; } = "";
    public string FirstHR_Text     { get; set; } = "";
    public string LastHR_Text      { get; set; } = "";

    public string SerialRange =>
        SerialFrom.ToString("D9") + " → " + SerialTo.ToString("D9");
}

public class ShippingLabelReport
{
    public DateTime PrintedAt    { get; set; }
    public string   LIBCODE      { get; set; } = "";
    public string   SiteName     { get; set; } = "";
    public int      BoxCount     { get; set; }
    public int      SerialFrom   { get; set; }
    public int      SerialTo     { get; set; }
    public string?  TemplateName { get; set; }
    public string?  PrintedBy    { get; set; }

    public string SerialRange =>
        SerialFrom.ToString("D9") + " → " + SerialTo.ToString("D9");
}

public class ReportSummary
{
    public int ReturnLabelsInPeriod { get; set; }
    public int BoxesShippedInPeriod { get; set; }
    public int ReturnSitesInPeriod  { get; set; }
    public int ShipSitesInPeriod    { get; set; }
    public int AllTimeReturnLabels  { get; set; }
    public int AllTimeBoxesShipped  { get; set; }
}

public class SiteBreakdown
{
    public int       SiteId       { get; set; }
    public string    LIBCODE      { get; set; } = "";
    public string    SiteName     { get; set; } = "";
    public int       ReturnLabels { get; set; }
    public int       BoxesShipped { get; set; }
    public DateTime? LastReturnRun { get; set; }
    public DateTime? LastShipRun  { get; set; }
}

public class LabelRow
{
    public string LIBCODE { get; set; } = "";
    public string SHIP_ADD_1 { get; set; } = "";
    public string SHIP_ADD_2 { get; set; } = "";
    public string SHIP_ADD_3 { get; set; } = "";
    public string SHIP_ADD_4 { get; set; } = "";
    public string ZIP9Dash { get; set; } = "";
    public int SerialInterleaved { get; set; }
    public string ChannelAI { get; set; } = "";
    public string STC { get; set; } = "";
    public string MID9 { get; set; } = "";
    public string ZIP9 { get; set; } = "";
    public string TrackingCore { get; set; } = "";
    public int MOD10 { get; set; }
    public string GS1_128_Raw { get; set; } = "";
    public string HR_Text { get; set; } = "";
}
