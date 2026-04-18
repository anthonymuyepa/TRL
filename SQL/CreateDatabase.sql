-- ============================================================
-- ShippingLabels Database Setup
-- Run this in SQL Server Management Studio (SSMS)
-- against your SQL Express instance
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ShippingLabels')
BEGIN
    CREATE DATABASE ShippingLabels;
END
GO

USE ShippingLabels;
GO

-- ── Sites table ─────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sites')
BEGIN
    CREATE TABLE Sites (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        LIBCODE         NVARCHAR(50)  NOT NULL UNIQUE,
        SiteName        NVARCHAR(150) NOT NULL,
        SHIP_ADD_1      NVARCHAR(150) NULL,
        SHIP_ADD_2      NVARCHAR(150) NULL,
        SHIP_ADD_3      NVARCHAR(150) NULL,
        SHIP_ADD_4      NVARCHAR(150) NULL,
        ZIP9Dash        NVARCHAR(20)  NOT NULL,   -- stored as ZIP-9 with dash e.g. 12345-6789
        MID9            NVARCHAR(20)  NOT NULL,
        STC             NVARCHAR(5)   NULL,
        ChannelAI       NVARCHAR(20)  NULL,
        CurrentSequence INT           NOT NULL DEFAULT 1,
        LastPrintedSerial INT         NULL,
        LastPrintedDate DATETIME      NULL,
        CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE(),
        UpdatedAt       DATETIME      NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ── Print history table ──────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrintHistory')
BEGIN
    CREATE TABLE PrintHistory (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        SiteId      INT          NOT NULL FOREIGN KEY REFERENCES Sites(Id),
        LIBCODE     NVARCHAR(50) NOT NULL,
        SiteName    NVARCHAR(150) NOT NULL,
        SerialFrom  INT          NOT NULL,
        SerialTo    INT          NOT NULL,
        LabelCount  INT          NOT NULL,
        RunType     NVARCHAR(10) NOT NULL,  -- 'CSV' or 'PRINT'
        LabelSize   NVARCHAR(10) NULL,      -- '2x4' or '4x6'
        PrintedAt   DATETIME     NOT NULL DEFAULT GETDATE(),
        PrintedBy   NVARCHAR(100) NULL
    );
END
GO

-- ── Useful views ─────────────────────────────────────────────
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SiteSummary')
    DROP VIEW vw_SiteSummary;
GO

CREATE VIEW vw_SiteSummary AS
SELECT
    s.Id,
    s.LIBCODE,
    s.SiteName,
    s.MID9,
    s.ZIP9Dash,
    s.STC,
    s.ChannelAI,
    s.CurrentSequence,
    s.LastPrintedSerial,
    s.LastPrintedDate,
    ISNULL((SELECT SUM(LabelCount) FROM PrintHistory ph WHERE ph.SiteId = s.Id), 0) AS TotalLabels
FROM Sites s;
GO

PRINT 'ShippingLabels database created successfully.';
