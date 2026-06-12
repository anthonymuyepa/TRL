namespace TRL.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "About — TRL — Track Return Labels";
        Size = new Size(560, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;

        // ── Header band ───────────────────────────────────────
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130,
            BackColor = Color.FromArgb(15, 55, 115),
            Padding = new Padding(28, 0, 28, 0)
        };

        var headerFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 14, 0, 10),
            BackColor = Color.Transparent
        };

        headerFlow.Controls.Add(new Label
        {
            Text = "TRL — Track Return Labels",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        });
        headerFlow.Controls.Add(new Label
        {
            Text = "Track Return Labels with precision",
            ForeColor = Color.FromArgb(180, 200, 230),
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        });
        headerFlow.Controls.Add(new Label
        {
            Text = "Version 1.0.0  —  April 2026",
            ForeColor = Color.FromArgb(140, 170, 210),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        });
        headerFlow.Controls.Add(new Label
        {
            Text = "Tadala Technologies  |  muyepaa@gmail.com",
            ForeColor = Color.FromArgb(160, 185, 220),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Margin = new Padding(0)
        });

        header.Controls.Add(headerFlow);
        Controls.Add(header);

        // ── Content ───────────────────────────────────────────
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(28, 20, 28, 20), BackColor = Color.White };
        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 480,
            Dock = DockStyle.Top
        };

        // Application
        flow.Controls.Add(SectionHeader("Application"));
        flow.Controls.Add(InfoRow("Product", "TRL — Track Return Labels"));
        flow.Controls.Add(InfoRow("Version", "1.0.0"));
        flow.Controls.Add(InfoRow("Release date", "April 2026"));
        flow.Controls.Add(InfoRow("Platform", "Windows 10 / 11 / Server 2016–2022"));
        flow.Controls.Add(InfoRow("Runtime", ".NET 9 / .NET 10 — Windows Forms"));
        flow.Controls.Add(Divider());

        // Architecture
        flow.Controls.Add(SectionHeader("Architecture"));
        flow.Controls.Add(InfoRow("Pattern", "Layered — Forms / Services / Data"));
        flow.Controls.Add(InfoRow("Database", "Microsoft SQL Server Express"));
        flow.Controls.Add(InfoRow("ORM", "Dapper 2.x (lightweight micro-ORM)"));
        flow.Controls.Add(InfoRow("SQL driver", "Microsoft.Data.SqlClient 5.x"));
        flow.Controls.Add(InfoRow("Automation", "VBScript via Windows Script Host (WSH)"));
        flow.Controls.Add(InfoRow("Mail merge", "Microsoft Word COM automation"));
        flow.Controls.Add(InfoRow("Barcode", "GS1-128 / USPS Intelligent Mail Barcode"));
        flow.Controls.Add(Divider());

        // Features
        flow.Controls.Add(SectionHeader("Key features"));
        flow.Controls.Add(FeatureRow("Per-site independent serial sequences"));
        flow.Controls.Add(FeatureRow("GS1 barcode field auto-computation (TrackingCore, MOD10, GS1_128_Raw, HR_Text)"));
        flow.Controls.Add(FeatureRow("CSV import for bulk site onboarding (55+ sites)"));
        flow.Controls.Add(FeatureRow("Word mail merge automation with formatted template"));
        flow.Controls.Add(FeatureRow("Full print history and audit trail in SQL Express"));
        flow.Controls.Add(FeatureRow("PIN-protected sequence reset for demo-to-production transitions"));
        flow.Controls.Add(FeatureRow("Persistent settings via JSON configuration"));
        flow.Controls.Add(Divider());

        // Legal
        flow.Controls.Add(SectionHeader("Legal"));
        var lblLegal = new Label
        {
            Text = "Copyright © 2026 Tadala Technologies. All rights reserved.\n\n" +
                   "This software is proprietary and confidential. Unauthorized copying, distribution, or modification is strictly prohibited. " +
                   "Microsoft Word, SQL Server, and Windows are trademarks of Microsoft Corporation.",
            Width = 480,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(110, 110, 110),
            Margin = new Padding(0, 4, 0, 8)
        };
        flow.Controls.Add(lblLegal);

        scroll.Controls.Add(flow);
        Controls.Add(scroll);

        // ── Footer ────────────────────────────────────────────
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(237, 239, 244)
        };
        var footerDiv = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 213, 220) };
        var footerFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 0, 20, 0),
            WrapContents = false
        };
        var lblFooter = new Label
        {
            Text = "TRL v1.0.0  •  .NET 10 Windows Forms  •  SQL Server Express  •  Tadala Technologies",
            ForeColor = Color.FromArgb(130, 130, 130),
            Font = new Font("Segoe UI", 8f),
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, 18, 0, 0)
        };
        var btnClose = new Button
        {
            Text = "Close",
            Size = new Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 14, 0, 0)
        };
        // Put close button right-aligned
        var footerLeft = new Panel { Dock = DockStyle.Fill, Height = 60 };
        footerLeft.Controls.Add(lblFooter);
        lblFooter.Location = new Point(20, 20);
        var footerRight = new Panel { Dock = DockStyle.Right, Width = 110, Height = 60 };
        footerRight.Controls.Add(btnClose);
        btnClose.Location = new Point(10, 14);
        footer.Controls.Add(footerLeft);
        footer.Controls.Add(footerRight);
        footer.Controls.Add(footerDiv);
        Controls.Add(footer);
        CancelButton = btnClose;
    }

    // ── Helpers ───────────────────────────────────────────────
    private static Label SectionHeader(string text) => new Label
    {
        Text = text.ToUpper(),
        Width = 480,
        Height = 26,
        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 90, 160),
        Margin = new Padding(0, 8, 0, 4)
    };

    private static Panel InfoRow(string label, string value)
    {
        var row = new Panel { Width = 480, Height = 24, Margin = new Padding(0, 1, 0, 1) };
        row.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, 4),
            Width = 130,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 100, 100)
        });
        row.Controls.Add(new Label
        {
            Text = value,
            Location = new Point(135, 4),
            Width = 340,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(30, 30, 30)
        });
        return row;
    }

    private static Panel FeatureRow(string text)
    {
        var row = new Panel { Width = 480, Height = 22, Margin = new Padding(0, 1, 0, 1) };
        row.Controls.Add(new Label
        {
            Text = "✓",
            Location = new Point(0, 3),
            Width = 20,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 140, 60)
        });
        row.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(22, 3),
            Width = 458,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(40, 40, 40)
        });
        return row;
    }

    private static Panel Divider() => new Panel
    {
        Width = 480,
        Height = 1,
        BackColor = Color.FromArgb(225, 227, 232),
        Margin = new Padding(0, 10, 0, 10)
    };
}
