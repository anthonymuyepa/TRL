using Dapper;
using Microsoft.Data.SqlClient;
using ShippingLabelManager.Models;

namespace ShippingLabelManager.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqlConnection Connect() => new SqlConnection(_connectionString);

    public bool TestConnection(out string error)
    {
        try
        {
            using var conn = Connect();
            conn.Open();
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Called on first run — creates the database, tables, and view if they don't exist.
    /// </summary>
    public void InitializeDatabase()
    {
        // Connect to master first to create the DB if it doesn't exist
        var masterCs = System.Text.RegularExpressions.Regex.Replace(
            _connectionString,
            @"(Database|Initial Catalog)\s*=\s*ShippingLabels",
            "$1=master",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        using (var master = new SqlConnection(masterCs))
        {
            master.Open();
            master.Execute(@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ShippingLabels')
                    CREATE DATABASE ShippingLabels;");
        }

        // Now connect to ShippingLabels and create tables + view
        using var conn = Connect();
        conn.Open();

        conn.Execute(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sites')
            CREATE TABLE Sites (
                Id                INT IDENTITY(1,1) PRIMARY KEY,
                LIBCODE           NVARCHAR(50)  NOT NULL UNIQUE,
                SiteName          NVARCHAR(150) NOT NULL,
                SHIP_ADD_1        NVARCHAR(150) NULL,
                SHIP_ADD_2        NVARCHAR(150) NULL,
                SHIP_ADD_3        NVARCHAR(150) NULL,
                SHIP_ADD_4        NVARCHAR(150) NULL,
                ZIP9Dash          NVARCHAR(20)  NOT NULL,
                MID9              NVARCHAR(20)  NOT NULL,
                STC               NVARCHAR(5)   NULL,
                ChannelAI         NVARCHAR(20)  NULL,
                CurrentSequence   INT           NOT NULL DEFAULT 1,
                LastPrintedSerial INT           NULL,
                LastPrintedDate   DATETIME      NULL,
                CreatedAt         DATETIME      NOT NULL DEFAULT GETDATE(),
                UpdatedAt         DATETIME      NOT NULL DEFAULT GETDATE()
            );");

        conn.Execute(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrintHistory')
            CREATE TABLE PrintHistory (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                SiteId      INT           NOT NULL REFERENCES Sites(Id),
                LIBCODE     NVARCHAR(50)  NOT NULL,
                SiteName    NVARCHAR(150) NOT NULL,
                SerialFrom  INT           NOT NULL,
                SerialTo    INT           NOT NULL,
                LabelCount  INT           NOT NULL,
                RunType     NVARCHAR(10)  NOT NULL,
                LabelSize   NVARCHAR(10)  NULL,
                PrintedAt   DATETIME      NOT NULL DEFAULT GETDATE(),
                PrintedBy   NVARCHAR(100) NULL
            );");

        // Add TemplateName column to PrintHistory if it doesn't exist (migration for existing DBs)
        conn.Execute(@"
            IF NOT EXISTS (
                SELECT * FROM sys.columns
                WHERE object_id = OBJECT_ID('PrintHistory') AND name = 'TemplateName')
            ALTER TABLE PrintHistory ADD TemplateName NVARCHAR(150) NULL;");

        // Add LastShippingBoxQty column to Sites if it doesn't exist
        conn.Execute(@"
            IF NOT EXISTS (
                SELECT * FROM sys.columns
                WHERE object_id = OBJECT_ID('Sites') AND name = 'LastShippingBoxQty')
            ALTER TABLE Sites ADD LastShippingBoxQty INT NOT NULL DEFAULT 0;");

        conn.Execute(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShippingSites')
            CREATE TABLE ShippingSites (
                Id         INT IDENTITY(1,1) PRIMARY KEY,
                SiteCode   NVARCHAR(50)  NOT NULL UNIQUE,
                SiteName   NVARCHAR(150) NOT NULL,
                SHIP_ADD_1 NVARCHAR(150) NULL,
                SHIP_ADD_2 NVARCHAR(150) NULL,
                SHIP_ADD_3 NVARCHAR(150) NULL,
                SHIP_ADD_4 NVARCHAR(150) NULL,
                ZIP9Dash   NVARCHAR(20)  NOT NULL DEFAULT '',
                MID9       NVARCHAR(20)  NOT NULL,
                STC        NVARCHAR(5)   NULL,
                ChannelAI  NVARCHAR(20)  NULL,
                IsActive   BIT           NOT NULL DEFAULT 0,
                CreatedAt  DATETIME      NOT NULL DEFAULT GETDATE(),
                UpdatedAt  DATETIME      NOT NULL DEFAULT GETDATE()
            );");

        conn.Execute(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShippingSequence')
            CREATE TABLE ShippingSequence (
                Id              INT NOT NULL PRIMARY KEY DEFAULT 1,
                CurrentSequence INT NOT NULL DEFAULT 1,
                LastUsedAt      DATETIME NULL
            );
            IF NOT EXISTS (SELECT * FROM ShippingSequence)
                INSERT INTO ShippingSequence (Id, CurrentSequence) VALUES (1, 1);");

        // Always recreate the view so it stays current
        conn.Execute("IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SiteSummary') DROP VIEW vw_SiteSummary;");
        conn.Execute(@"
            CREATE VIEW vw_SiteSummary AS
            SELECT
                s.Id, s.LIBCODE, s.SiteName,
                s.SHIP_ADD_1, s.SHIP_ADD_2, s.SHIP_ADD_3, s.SHIP_ADD_4,
                s.ZIP9Dash, s.MID9, s.STC, s.ChannelAI,
                s.CurrentSequence, s.LastPrintedSerial, s.LastPrintedDate,
                s.LastShippingBoxQty,
                ISNULL((SELECT SUM(LabelCount) FROM PrintHistory ph WHERE ph.SiteId = s.Id), 0) AS TotalLabels
            FROM Sites s;");
    }

    // ── Sites ────────────────────────────────────────────────

    public List<Site> GetAllSites()
    {
        using var conn = Connect();
        return conn.Query<Site>("SELECT * FROM vw_SiteSummary ORDER BY SiteName").AsList();
    }

    public Site? GetSite(int id)
    {
        using var conn = Connect();
        return conn.QueryFirstOrDefault<Site>("SELECT * FROM vw_SiteSummary WHERE Id = @id", new { id });
    }

    public int InsertSite(Site s)
    {
        using var conn = Connect();
        var sql = @"
            INSERT INTO Sites (LIBCODE,SiteName,SHIP_ADD_1,SHIP_ADD_2,SHIP_ADD_3,SHIP_ADD_4,
                               ZIP9Dash,MID9,STC,ChannelAI,CurrentSequence)
            VALUES (@LIBCODE,@SiteName,@SHIP_ADD_1,@SHIP_ADD_2,@SHIP_ADD_3,@SHIP_ADD_4,
                    @ZIP9Dash,@MID9,@STC,@ChannelAI,@CurrentSequence);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return conn.ExecuteScalar<int>(sql, s);
    }

    public void UpdateSite(Site s)
    {
        using var conn = Connect();
        var sql = @"
            UPDATE Sites SET
                LIBCODE=@LIBCODE, SiteName=@SiteName,
                SHIP_ADD_1=@SHIP_ADD_1, SHIP_ADD_2=@SHIP_ADD_2,
                SHIP_ADD_3=@SHIP_ADD_3, SHIP_ADD_4=@SHIP_ADD_4,
                ZIP9Dash=@ZIP9Dash, MID9=@MID9, STC=@STC,
                ChannelAI=@ChannelAI, CurrentSequence=@CurrentSequence,
                UpdatedAt=GETDATE()
            WHERE Id=@Id";
        conn.Execute(sql, s);
    }

    public void DeleteSite(int id)
    {
        using var conn = Connect();
        conn.Execute("DELETE FROM Sites WHERE Id=@id", new { id });
    }

    public bool LIBCODEExists(string libcode, int excludeId = 0)
    {
        using var conn = Connect();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Sites WHERE LIBCODE=@libcode AND Id<>@excludeId",
            new { libcode, excludeId }) > 0;
    }

    public void UpdateShippingBoxQty(int siteId, int qty)
    {
        using var conn = Connect();
        conn.Execute(
            "UPDATE Sites SET LastShippingBoxQty=@qty, UpdatedAt=GETDATE() WHERE Id=@siteId",
            new { qty, siteId });
    }

    // ── Shipping Sites ───────────────────────────────────────

    public List<ShippingSite> GetAllShippingSites()
    {
        using var conn = Connect();
        return conn.Query<ShippingSite>("SELECT * FROM ShippingSites ORDER BY SiteName").AsList();
    }

    public ShippingSite? GetActiveShippingSite()
    {
        using var conn = Connect();
        return conn.QueryFirstOrDefault<ShippingSite>("SELECT TOP 1 * FROM ShippingSites WHERE IsActive=1");
    }

    public int InsertShippingSite(ShippingSite s)
    {
        using var conn = Connect();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO ShippingSites (SiteCode,SiteName,SHIP_ADD_1,SHIP_ADD_2,SHIP_ADD_3,SHIP_ADD_4,ZIP9Dash,MID9,STC,ChannelAI,IsActive)
            VALUES (@SiteCode,@SiteName,@SHIP_ADD_1,@SHIP_ADD_2,@SHIP_ADD_3,@SHIP_ADD_4,@ZIP9Dash,@MID9,@STC,@ChannelAI,@IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", s);
    }

    public void UpdateShippingSite(ShippingSite s)
    {
        using var conn = Connect();
        conn.Execute(@"
            UPDATE ShippingSites SET
                SiteCode=@SiteCode, SiteName=@SiteName,
                SHIP_ADD_1=@SHIP_ADD_1, SHIP_ADD_2=@SHIP_ADD_2,
                SHIP_ADD_3=@SHIP_ADD_3, SHIP_ADD_4=@SHIP_ADD_4,
                ZIP9Dash=@ZIP9Dash, MID9=@MID9, STC=@STC,
                ChannelAI=@ChannelAI, UpdatedAt=GETDATE()
            WHERE Id=@Id", s);
    }

    public void SetActiveShippingSite(int id)
    {
        using var conn = Connect();
        conn.Open();
        using var tx = conn.BeginTransaction();
        conn.Execute("UPDATE ShippingSites SET IsActive=0, UpdatedAt=GETDATE()", null, tx);
        conn.Execute("UPDATE ShippingSites SET IsActive=1, UpdatedAt=GETDATE() WHERE Id=@id", new { id }, tx);
        tx.Commit();
    }

    public bool ShippingSiteCodeExists(string siteCode, int excludeId = 0)
    {
        using var conn = Connect();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM ShippingSites WHERE SiteCode=@siteCode AND Id<>@excludeId",
            new { siteCode, excludeId }) > 0;
    }

    public int AdvanceShippingSequence(int count, out int newNext)
    {
        using var conn = Connect();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var current = conn.ExecuteScalar<int>(
                "SELECT CurrentSequence FROM ShippingSequence WHERE Id=1", null, tx);
            var next = current + count;
            conn.Execute(
                "UPDATE ShippingSequence SET CurrentSequence=@next, LastUsedAt=GETDATE() WHERE Id=1",
                new { next }, tx);
            tx.Commit();
            newNext = next;
            return current;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public int GetCurrentShippingSequence()
    {
        using var conn = Connect();
        return conn.ExecuteScalar<int>("SELECT CurrentSequence FROM ShippingSequence WHERE Id=1");
    }

    // ── Sequence advancement (atomic) ────────────────────────

    public int AdvanceSequence(int siteId, int count, out int newNext)
    {
        using var conn = Connect();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var current = conn.ExecuteScalar<int>(
                "SELECT CurrentSequence FROM Sites WHERE Id=@siteId",
                new { siteId }, tx);

            var newSeq = current + count;
            conn.Execute(@"
                UPDATE Sites SET
                    CurrentSequence=@newSeq,
                    LastPrintedSerial=@last,
                    LastPrintedDate=GETDATE(),
                    UpdatedAt=GETDATE()
                WHERE Id=@siteId",
                new { newSeq, last = newSeq - 1, siteId }, tx);

            tx.Commit();
            newNext = newSeq;
            return current;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ── History ──────────────────────────────────────────────

    public void InsertHistory(PrintHistoryEntry h)
    {
        using var conn = Connect();
        conn.Execute(@"
            INSERT INTO PrintHistory (SiteId,LIBCODE,SiteName,SerialFrom,SerialTo,LabelCount,RunType,LabelSize,PrintedBy,TemplateName)
            VALUES (@SiteId,@LIBCODE,@SiteName,@SerialFrom,@SerialTo,@LabelCount,@RunType,@LabelSize,@PrintedBy,@TemplateName)", h);
    }

    public List<PrintHistoryEntry> GetHistory(int top = 500)
    {
        using var conn = Connect();
        return conn.Query<PrintHistoryEntry>($@"
            SELECT TOP {top} * FROM PrintHistory
            ORDER BY PrintedAt DESC").AsList();
    }

    public void ClearHistory()
    {
        using var conn = Connect();
        conn.Execute("DELETE FROM PrintHistory");
    }

    // ── Sequence reset ──────────────────────────────────────────

    public void ResetAllSequences(int startAt = 1, bool clearHistory = true)
    {
        using var conn = Connect();
        conn.Execute(
            "UPDATE Sites SET CurrentSequence=@startAt, LastPrintedSerial=NULL, LastPrintedDate=NULL, UpdatedAt=GETDATE()",
            new { startAt });
        if (clearHistory)
            conn.Execute("DELETE FROM PrintHistory");
    }

    public void ResetSiteSequence(int siteId, int startAt = 1, bool clearHistory = true)
    {
        using var conn = Connect();
        conn.Execute(
            "UPDATE Sites SET CurrentSequence=@startAt, LastPrintedSerial=NULL, LastPrintedDate=NULL, UpdatedAt=GETDATE() WHERE Id=@siteId",
            new { startAt, siteId });
        if (clearHistory)
            conn.Execute("DELETE FROM PrintHistory WHERE SiteId=@siteId", new { siteId });
    }

    // ── Reports ──────────────────────────────────────────────

    public List<ReturnLabelReport> GetReturnLabelReport(DateTime from, DateTime to, int? siteId = null)
    {
        using var conn = Connect();
        return conn.Query<ReturnLabelReport>(@"
            SELECT ph.PrintedAt, ph.LIBCODE, ph.SiteName,
                   ph.SerialFrom, ph.SerialTo, ph.LabelCount,
                   ph.RunType, ph.TemplateName, ph.PrintedBy, ph.LabelSize
            FROM PrintHistory ph
            WHERE ph.RunType IN ('WORD','CSV')
              AND ph.PrintedAt >= @from
              AND ph.PrintedAt <  @to
              AND (@siteId IS NULL OR ph.SiteId = @siteId)
            ORDER BY ph.PrintedAt DESC",
            new { from, to, siteId }).AsList();
    }

    public List<ShippingLabelReport> GetShippingLabelReport(DateTime from, DateTime to, int? siteId = null)
    {
        using var conn = Connect();
        return conn.Query<ShippingLabelReport>(@"
            SELECT ph.PrintedAt, ph.LIBCODE, ph.SiteName,
                   ph.LabelCount AS BoxCount,
                   ph.SerialFrom, ph.SerialTo,
                   ph.TemplateName, ph.PrintedBy
            FROM PrintHistory ph
            WHERE ph.RunType = 'SHIP'
              AND ph.PrintedAt >= @from
              AND ph.PrintedAt <  @to
              AND (@siteId IS NULL OR ph.SiteId = @siteId)
            ORDER BY ph.PrintedAt DESC",
            new { from, to, siteId }).AsList();
    }

    public ReportSummary GetReportSummary(DateTime from, DateTime to)
    {
        using var conn = Connect();
        return new ReportSummary
        {
            ReturnLabelsInPeriod = conn.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory WHERE RunType IN ('WORD','CSV') AND PrintedAt>=@from AND PrintedAt<@to",
                new { from, to }),
            BoxesShippedInPeriod = conn.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory WHERE RunType='SHIP' AND PrintedAt>=@from AND PrintedAt<@to",
                new { from, to }),
            ReturnSitesInPeriod = conn.ExecuteScalar<int>(
                "SELECT COUNT(DISTINCT SiteId) FROM PrintHistory WHERE RunType IN ('WORD','CSV') AND PrintedAt>=@from AND PrintedAt<@to",
                new { from, to }),
            ShipSitesInPeriod = conn.ExecuteScalar<int>(
                "SELECT COUNT(DISTINCT SiteId) FROM PrintHistory WHERE RunType='SHIP' AND PrintedAt>=@from AND PrintedAt<@to",
                new { from, to }),
            AllTimeReturnLabels = conn.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory WHERE RunType IN ('WORD','CSV')"),
            AllTimeBoxesShipped = conn.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory WHERE RunType='SHIP'")
        };
    }

    public List<SiteBreakdown> GetSiteBreakdown(DateTime from, DateTime to)
    {
        using var conn = Connect();
        return conn.Query<SiteBreakdown>(@"
            SELECT ph.SiteId, ph.LIBCODE, ph.SiteName,
                   SUM(CASE WHEN RunType IN ('WORD','CSV') THEN LabelCount ELSE 0 END) AS ReturnLabels,
                   SUM(CASE WHEN RunType = 'SHIP' THEN LabelCount ELSE 0 END)          AS BoxesShipped,
                   MAX(CASE WHEN RunType IN ('WORD','CSV') THEN PrintedAt ELSE NULL END) AS LastReturnRun,
                   MAX(CASE WHEN RunType = 'SHIP' THEN PrintedAt ELSE NULL END)         AS LastShipRun
            FROM PrintHistory ph
            WHERE ph.PrintedAt >= @from AND ph.PrintedAt < @to
            GROUP BY ph.SiteId, ph.LIBCODE, ph.SiteName
            ORDER BY ReturnLabels DESC",
            new { from, to }).AsList();
    }

    // ── Stats ────────────────────────────────────────────────

    public (int totalSites, int totalLabels, int todayLabels) GetStats()
    {
        using var conn = Connect();
        var totalSites  = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Sites");
        var totalLabels = conn.ExecuteScalar<int>("SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory");
        var todayLabels = conn.ExecuteScalar<int>(
            "SELECT ISNULL(SUM(LabelCount),0) FROM PrintHistory WHERE CAST(PrintedAt AS DATE)=CAST(GETDATE() AS DATE)");
        return (totalSites, totalLabels, todayLabels);
    }
}
