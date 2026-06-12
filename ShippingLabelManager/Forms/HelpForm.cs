namespace ShippingLabelManager.Forms;

public class HelpForm : Form
{
    public HelpForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Help — TRL";
        Size = new Size(780, 700);
        MinimumSize = new Size(700, 580);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(250, 251, 253);

        // ── Left nav ──────────────────────────────────────────
        var navPanel = new Panel
        {
            Dock       = DockStyle.Left,
            Width      = 210,
            BackColor  = Color.FromArgb(237, 239, 244),
            Padding    = new Padding(0, 8, 0, 8),
            AutoScroll = true
        };

        var topics = new[]
        {
            "Getting started",
            "Installation & first run",
            "Initial setup (do this first)",
            "Managing sites",
            "Importing & updating sites",
            "Return Labels — how it works",
            "Shipping Labels — how it works",
            "Shipping Sites",
            "Templates & Word merge",
            "Print sessions & sequence control",
            "Settings reference",
            "Reports & analytics",
            "Print History",
            "Resetting sequences",
            "Troubleshooting",
            "Contact & support"
        };

        var contentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 20, 24, 20) };
        var rtf = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.FromArgb(250, 251, 253),
            Font        = new Font("Segoe UI", 9.5f),
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };
        contentPanel.Controls.Add(rtf);

        // Build nav buttons
        int btnY = 8;
        Button? firstBtn = null;
        foreach (var topic in topics)
        {
            var btn = new Button
            {
                Text      = "  " + topic,
                Location  = new Point(0, btnY),
                Size      = new Size(210, 34),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent,
                Tag       = topic
            };
            btn.FlatAppearance.BorderSize           = 0;
            btn.FlatAppearance.MouseOverBackColor   = Color.FromArgb(220, 224, 234);
            var t = topic;
            btn.Click += (s, e) =>
            {
                foreach (Control c in navPanel.Controls)
                    if (c is Button b) { b.BackColor = Color.Transparent; b.Font = new Font("Segoe UI", 9f); }
                btn.BackColor = Color.FromArgb(205, 212, 228);
                btn.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
                ShowTopic(t, rtf);
            };
            navPanel.Controls.Add(btn);
            if (firstBtn == null) firstBtn = btn;
            btnY += 34;
        }

        // Divider between nav and content
        var divider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Color.FromArgb(210, 213, 220) };

        Controls.Add(contentPanel);
        Controls.Add(divider);
        Controls.Add(navPanel);

        // Close button
        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(237, 239, 244) };
        var sep       = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 213, 220) };
        var btnClose  = new Button
        {
            Text         = "Close",
            Size         = new Size(90, 32),
            Location     = new Point(20, 10),
            FlatStyle    = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        pnlBottom.Controls.AddRange(new Control[] { sep, btnClose });
        Controls.Add(pnlBottom);
        CancelButton = btnClose;

        // Show first topic
        firstBtn?.PerformClick();
    }

    private static void ShowTopic(string topic, RichTextBox rtf)
    {
        rtf.Clear();
        switch (topic)
        {
            case "Getting started":                   ShowGettingStarted(rtf);   break;
            case "Installation & first run":          ShowInstallation(rtf);     break;
            case "Initial setup (do this first)":     ShowDatabase(rtf);         break;
            case "Managing sites":                    ShowManagingSites(rtf);    break;
            case "Importing & updating sites":        ShowImporting(rtf);        break;
            case "Return Labels — how it works":      ShowGenerating(rtf);       break;
            case "Shipping Labels — how it works":    ShowShippingLabels(rtf);   break;
            case "Shipping Sites":                    ShowShippingSites(rtf);    break;
            case "Templates & Word merge":            ShowMailMerge(rtf);        break;
            case "Print sessions & sequence control": ShowPrintSessions(rtf);    break;
            case "Settings reference":                ShowSettings(rtf);         break;
            case "Reports & analytics":               ShowReportsAnalytics(rtf); break;
            case "Print History":                     ShowPrintHistory(rtf);     break;
            case "Resetting sequences":               ShowReset(rtf);            break;
            case "Troubleshooting":                   ShowTroubleshooting(rtf);  break;
            case "Contact & support":                 ShowContactSupport(rtf);   break;
        }
        rtf.SelectionStart = 0;
        rtf.ScrollToCaret();
    }

    // ── Topic renderers ───────────────────────────────────────

    private static void ShowGettingStarted(RichTextBox r)
    {
        H1(r, "Getting started");
        Body(r, "TRL (Track Return Labels) automates the generation and tracking of return labels across 55+ sites. Each site has its own independent serial sequence — no two labels ever share the same number.");
        Spacer(r);
        H2(r, "What the app does");
        Bullet(r, "Manages up to 55 sites, each with unique Mailer ID (MID9) and address fields");
        Bullet(r, "Generates all 15 barcode fields automatically (TrackingCore, MOD10, GS1_128_Raw, HR_Text etc.)");
        Bullet(r, "Exports a mail-merge-ready CSV for your return-label Word template");
        Bullet(r, "Opens Word with your formatted template pre-linked to the data");
        Bullet(r, "Tracks every print run and serial range in SQL Express");
        Spacer(r);
        H2(r, "Typical workflow");
        Step(r, "1", "Select a site from the dropdown on Generate Labels tab");
        Step(r, "2", "Set the quantity of labels needed");
        Step(r, "3", "Click Print Labels via Word");
        Step(r, "4", "Review the formatted labels in Word — print when ready");
        Step(r, "5", "The sequence automatically advances and is saved to the database");
    }

    private static void ShowInstallation(RichTextBox r)
    {
        H1(r, "Installation");
        H2(r, "System requirements");
        Bullet(r, "Windows 10, Windows 11, or Windows Server 2016–2022");
        Bullet(r, ".NET 9 or .NET 10 runtime (or SDK for development)");
        Bullet(r, "SQL Server Express (free) — any recent version");
        Bullet(r, "Microsoft Word (for mail merge functionality)");
        Spacer(r);
        H2(r, "Step 1 — Install .NET runtime");
        Body(r, "Download .NET 9 or 10 from: https://dotnet.microsoft.com/download");
        Body(r, "Choose the Windows x64 Runtime (not SDK unless developing). Run the installer.");
        Spacer(r);
        H2(r, "Step 2 — Install SQL Server Express");
        Body(r, "Download SQL Server Express (free) from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads");
        Body(r, "Choose Basic installation. The default instance name will be .\\SQLEXPRESS.");
        Spacer(r);
        H2(r, "Step 3 — Run the application");
        Body(r, "Extract the application folder to your preferred location (e.g. C:\\ShippingLabelManager).");
        Body(r, "Double-click ShippingLabelManager.exe to launch. On first run, the database and all tables are created automatically — no manual SQL scripts required.");
        Spacer(r);
        H2(r, "Step 4 — Configure settings");
        Body(r, "Go to File > Settings and configure:");
        Bullet(r, "Temp CSV folder — where exported CSVs are saved");
        Bullet(r, "Word template path — your formatted .docx mail merge template");
        Bullet(r, "Default printer — for direct printing");
        Bullet(r, "Admin PIN — to protect sequence reset actions");
    }

    private static void ShowDatabase(RichTextBox r)
    {
        H1(r, "Database setup");
        H2(r, "Automatic setup on first run");
        Body(r, "The application automatically creates the ShippingLabels database on first launch. No manual SQL scripts or SSMS interaction is required. The following are created automatically:");
        Spacer(r);
        Bullet(r, "Sites table — stores all site records with address fields, MID9, STC, ChannelAI and current sequence number");
        Bullet(r, "PrintHistory table — logs every CSV export and Word merge run with serial range and timestamp");
        Bullet(r, "ShippingSites table — stores distribution center records used for outbound shipping label barcodes");
        Bullet(r, "ShippingSequence table — global counter for shipping label tracking numbers");
        Bullet(r, "vw_SiteSummary view — aggregates site data with total label counts");
        Spacer(r);
        H2(r, "Connection string");
        Body(r, "The default connection string targets a local SQL Express instance:");
        Code(r, @"Server=.\SQLEXPRESS;Database=ShippingLabels;Integrated Security=true;TrustServerCertificate=true;");
        Body(r, "To change this, go to File > Database connection. The connection string is saved in appsettings.json alongside the executable.");
        Spacer(r);
        H2(r, "Manual SSMS access (optional)");
        Body(r, "You can connect to the database using SQL Server Management Studio (SSMS) for reporting or backup. The database name is ShippingLabels on your local SQLEXPRESS instance.");
    }

    private static void ShowManagingSites(RichTextBox r)
    {
        H1(r, "Managing sites");
        Body(r, "The Manage Sites tab is split into two sub-tabs: Return Sites and Shipping Sites. Return Sites are the 56 library destinations. Shipping Sites are your distribution centers.");
        Spacer(r);
        H2(r, "Return Sites sub-tab");
        Body(r, "Shows all configured return sites. Each site stores:");
        Bullet(r, "LIBCODE — unique site identifier (e.g. AR1A)");
        Bullet(r, "Site name and full address (SHIP_ADD_1-4), ZIP-9");
        Bullet(r, "MID9 (Mailer ID), STC, ChannelAI — used in barcode computation");
        Bullet(r, "Current sequence — the next serial number to be used");
        Bullet(r, "Last printed serial and date");
        Spacer(r);
        H2(r, "Adding a return site");
        Step(r, "1", "Click + Add site");
        Step(r, "2", "Fill in required fields: LIBCODE, Site name, MID9, ZIP-9 (highlighted in yellow)");
        Step(r, "3", "Fill in address lines and optional fields (STC, ChannelAI)");
        Step(r, "4", "Set Start at sequence (default 1 for new sites)");
        Step(r, "5", "Click Save site");
        Spacer(r);
        H2(r, "Editing a return site");
        Step(r, "1", "Select the site in the grid (single-click) or double-click it");
        Step(r, "2", "Click Edit selected");
        Step(r, "3", "Modify fields as needed and click Save site");
        Body(r, "Editing does not affect the sequence or print history — only address and barcode fields are updated.");
        Spacer(r);
        H2(r, "Deleting a return site");
        Body(r, "Select a site and click Delete selected. Print history is retained, but the site record and sequence are permanently removed. This cannot be undone.");
        Spacer(r);
        H2(r, "Search and filter");
        Body(r, "Use the search box in the top-right of the Return Sites tab to filter by LIBCODE, site name, or MID9. The grid updates in real time as you type.");
    }

    private static void ShowImporting(RichTextBox r)
    {
        H1(r, "Importing sites");
        H2(r, "Step 1 — Download the CSV template");
        Body(r, "Go to Sites > Download CSV import template. Save the file to your Desktop. Open it in Excel — it contains the correct column headers with a sample row.");
        Spacer(r);
        H2(r, "CSV column format");
        Code(r, "LIBCODE, SiteName, SHIP_ADD_1, SHIP_ADD_2, SHIP_ADD_3, SHIP_ADD_4, ZIP-9, MID9, STC, ChannelAI");
        Spacer(r);
        Bullet(r, "LIBCODE — your internal site code (must be unique, e.g. AR1A)");
        Bullet(r, "SiteName — display name for the site");
        Bullet(r, "SHIP_ADD_1 to SHIP_ADD_4 — address lines");
        Bullet(r, "ZIP-9 — postal code with or without dash (e.g. 12345-6789)");
        Bullet(r, "MID9 — 9-digit Mailer ID assigned by Canada Post / USPS");
        Bullet(r, "STC — 2-digit Service Type Code");
        Bullet(r, "ChannelAI — Application Identifier (typically 420)");
        Spacer(r);
        H2(r, "Step 2 — Fill in your sites");
        Body(r, "Add one row per site. Column order does not matter — the importer reads by header name. Column headers are case-insensitive.");
        Spacer(r);
        H2(r, "Step 3 — Import the file");
        Body(r, "Go to Sites > Import sites from CSV. Browse to your file. A preview grid shows all rows about to be imported — review them before confirming. Sites with a LIBCODE that already exists are automatically skipped (not overwritten).");
        Spacer(r);
        H2(r, "Adding sites individually");
        Body(r, "Go to Manage Sites tab and click + Add site to enter a site manually using the site form.");
    }

    private static void ShowGenerating(RichTextBox r)
    {
        H1(r, "Return Labels — how it works");
        H2(r, "Labels tab > Return Labels sub-tab");
        Step(r, "1", "Select a site from the Site dropdown. The dropdown shows all configured sites — start typing to filter.");
        Step(r, "2", "Set the Quantity — how many labels you need for this batch.");
        Step(r, "3", "Review the sequence info: Last printed serial and the Next serial range for this batch.");
        Step(r, "4", "Review the Computed barcode fields panel — shows TrackingCore, MOD10, GS1_128_Raw and HR_Text for the first label.");
        Spacer(r);
        H2(r, "Computed fields");
        Bullet(r, "ZIP9 — ZIP-9 with dash removed");
        Bullet(r, "SerialInterleaved — the sequential label number for this site");
        Bullet(r, "TrackingCore — STC(2) + ZIP9(3) + Serial(9) + MID9(7) zero-padded");
        Bullet(r, "MOD10 — GS1 check digit calculated from TrackingCore");
        Bullet(r, "GS1_128_Raw — 420 + ZIP9 + \\F + TrackingCore + MOD10");
        Bullet(r, "HR_Text — last 22 chars of GS1_128_Raw split into groups of 4");
        Spacer(r);
        H2(r, "Export buttons");
        Bullet(r, "Export CSV only — saves the CSV to a location you choose. Confirms before advancing the sequence.");
        Bullet(r, "Print Labels via Word — exports CSV to temp folder, opens a Print Session dialog, merges with your Word template. Sequence advances only when you confirm labels printed successfully.");
    }

    private static void ShowShippingLabels(RichTextBox r)
    {
        H1(r, "Shipping Labels — how it works");
        Body(r, "Shipping labels are printed for outbound boxes sent from your distribution center to each of the 56 library sites. Each box gets a unique tracked barcode using a global serial sequence.");
        Spacer(r);
        H2(r, "Workflow");
        Step(r, "1", "Go to Labels tab > Shipping Labels sub-tab");
        Step(r, "2", "Ensure an active Shipping Site is configured — check the status bar shows Shipper: [code]. If not set, go to Manage Sites > Shipping Sites");
        Step(r, "3", "The grid shows all 56 return sites");
        Step(r, "4", "Enter number of boxes in the Boxes column for each site you are shipping to");
        Step(r, "5", "Select sites for this print run:");
        Body(r, "     Option A — Single site: click to highlight the row (no checkbox needed — the highlighted row is used automatically if no checkboxes are checked)");
        Body(r, "     Option B — Multiple sites: check the checkbox on the left of each site to include");
        Step(r, "6", "The summary bar shows selected sites and total label count");
        Step(r, "7", "Click Print Shipping Labels or Export CSV only");
        Spacer(r);
        H2(r, "Barcode computation for shipping labels");
        Body(r, "Shipping label barcodes use a hybrid of data from two sources:");
        Bullet(r, "Delivery address (SHIP_ADD_1-4, ZIP-9) — comes from the Return Site (destination)");
        Bullet(r, "MID9, STC, ChannelAI — comes from the active Shipping Site (sender)");
        Bullet(r, "Serial number — from the global shipping sequence (unique across all runs and all sites)");
        Body(r, "This matches postal authority requirements where the tracking account belongs to the sender. Each box gets a globally unique tracking barcode.");
        Spacer(r);
        H2(r, "Box X of Y");
        Body(r, "The CSV includes Ord (box number) and NumRecs (total boxes in the run). Your Word template uses these fields to show \"Box 1 of 5\" etc. on each label automatically.");
        Spacer(r);
        H2(r, "Sequence");
        Body(r, "The shipping sequence advances immediately when you export or print — there is no confirm/cancel step. The sequence is global and shared across all shipping sites and all runs. Every box ever shipped gets a unique number.");
    }

    private static void ShowShippingSites(RichTextBox r)
    {
        H1(r, "Shipping Sites");
        Body(r, "Shipping Sites are the distribution centers that send boxes of return labels to the 56 library sites. They are separate from Return Sites and serve a different purpose in the barcode computation.");
        Spacer(r);
        H2(r, "Why Shipping Sites exist");
        Body(r, "When printing shipping labels, the barcode fields (MID9, STC, ChannelAI) come from the SHIPPER (your distribution center), not from the destination library site. The destination site only provides the delivery address (SHIP_ADD_1-4, ZIP-9). This matches postal authority requirements where the tracking barcode is tied to the sender's mailer account.");
        Spacer(r);
        H2(r, "Shipping vs Return sites — key difference");
        Bullet(r, "Return Sites — provide the ship-to address and have their own serial sequences for return labels");
        Bullet(r, "Shipping Sites — provide MID9, STC, ChannelAI for barcode computation and their own address for the shipper block on the label template");
        Bullet(r, "One Shipping Site is marked ACTIVE at a time — this is the site used for all shipping label runs");
        Spacer(r);
        H2(r, "Managing Shipping Sites");
        Step(r, "1", "Go to Manage Sites tab");
        Step(r, "2", "Click the Shipping Sites sub-tab");
        Step(r, "3", "Click + Add shipping site");
        Step(r, "4", "Fill in required fields:");
        Body(r, "     Site code — unique identifier (e.g. HQ or DC1)");
        Body(r, "     Site name — e.g. Main Distribution Center");
        Body(r, "     MID9 — 9-digit Mailer ID used in barcode computation");
        Body(r, "     STC — Service Type Code");
        Body(r, "     ChannelAI — typically 420");
        Body(r, "     Full address — shown in the shipper block on your label template");
        Step(r, "5", "Click Save site");
        Spacer(r);
        H2(r, "Setting the active shipping site");
        Body(r, "Only one shipping site is used for barcode computation at a time. To set a site as active:");
        Step(r, "1", "Go to Manage Sites > Shipping Sites");
        Step(r, "2", "Select the site in the grid");
        Step(r, "3", "Click Set as active");
        Body(r, "The active site is shown in the status bar at the bottom of the app as \"Shipper: [SiteCode]\". If no shipping site is active, printing shipping labels will show a warning.");
        Spacer(r);
        H2(r, "When to add a new shipping site");
        Bullet(r, "Distribution center moves to new address");
        Bullet(r, "New regional distribution center opened");
        Bullet(r, "MID9 or postal account changes");
        Body(r, "Old shipping sites are kept for history integrity — they are never deleted, only deactivated.");
        Spacer(r);
        H2(r, "Shipping sequence");
        Body(r, "Shipping labels use a single global sequence counter shared across all sites and all print runs. This guarantees every box ever shipped gets a globally unique tracking number — no collisions across different sites, dates, or print sessions. The sequence never resets unless manually adjusted in File > Settings > Sequence Reset.");
    }

    private static void ShowMailMerge(RichTextBox r)
    {
        H1(r, "Mail merge & Word");
        H2(r, "How it works");
        Body(r, "The app exports a CSV with all 15 barcode fields, then opens your Word mail merge template and links the CSV as the data source. Word merges the data into your formatted template — preserving logos, barcodes, and layout.");
        Spacer(r);
        H2(r, "CSV columns exported");
        Code(r, "LIBCODE, SHIP_ADD_1, SHIP_ADD_2, SHIP_ADD_3, SHIP_ADD_4, ZIP-9,\nSerialInterleaved, ChannelAI, STC, MID9, ZIP9,\nTrackingCore, MOD10, GS1_128_Raw, HR_Text");
        Spacer(r);
        H2(r, "Setting up your Word template");
        Body(r, "Your Word template must have mail merge fields already configured. The field names must match the CSV column headers exactly (case-sensitive). Configure this once — the app reuses the same template every time.");
        Spacer(r);
        H2(r, "File locking");
        Body(r, "The app kills any running Word processes before opening a new instance, and opens the template as read-only to prevent file locking. The template is always released cleanly after the merge completes.");
        Spacer(r);
        H2(r, "Printing from Word");
        Body(r, "After the merge, Word shows the formatted labels. Review them in the print preview, then print using Word's standard print dialog (Ctrl+P) to send to your label printer.");
    }

    private static void ShowPrintSessions(RichTextBox r)
    {
        H1(r, "Print sessions & sequence control");
        Body(r, "Print sessions separate the act of merging and printing from the act of advancing the sequence. This prevents the sequence from advancing if printing fails or is incomplete.");
        Spacer(r);
        H2(r, "What is a print session?");
        Body(r, "When you click Print Labels via Word on the Return Labels tab, a persistent Print Session dialog opens. The serial sequence stays unchanged until you explicitly confirm that labels printed successfully.");
        Spacer(r);
        H2(r, "Print session workflow");
        Step(r, "1", "Click Print Labels via Word on the Return Labels tab");
        Step(r, "2", "The Print Session dialog opens — shows site name, serial range, and quantity for this run");
        Step(r, "3", "Select a template from the dropdown");
        Step(r, "4", "Click Merge & Print — Word opens in the background; the dialog logs the attempt and shows status");
        Step(r, "5", "Review labels in Word — print when satisfied, then return to the Print Session dialog");
        Step(r, "6a", "Click Confirm — labels printed: sequence advances, run logged as WORD in history");
        Step(r, "6b", "Click Cancel — reprint needed: sequence is NOT advanced; the same labels can be reprinted");
        Spacer(r);
        H2(r, "Multiple attempts in one session");
        Body(r, "You can click Merge & Print multiple times in a single session — for example, if the first merge had a formatting issue or the printer jammed. Each click is logged as ATTEMPT in print history. Only Confirm advances the sequence.");
        Spacer(r);
        H2(r, "Sequence control");
        Body(r, "The sequence advances atomically in the database. If two users run the app simultaneously, each gets a unique non-overlapping serial range. No labels ever share a tracking number.");
        Spacer(r);
        H2(r, "History entries created by a session");
        Bullet(r, "ATTEMPT — logged immediately each time Merge & Print is clicked within the session");
        Bullet(r, "WORD — logged when Confirm is clicked (confirmed successful print)");
        Bullet(r, "CANCELLED — logged when Cancel is clicked (if at least one attempt was made)");
    }

    private static void ShowSettings(RichTextBox r)
    {
        H1(r, "Settings reference");
        Body(r, "Access settings via File > Settings. Settings are saved in appsettings.json alongside the executable and persist between sessions.");
        Spacer(r);
        H2(r, "General tab");
        Bullet(r, "Temp CSV folder — folder where CSV files are saved before Word opens them (e.g. C:\\Temp\\Labels)");
        Bullet(r, "Word mail merge template — full path to your .docx template file");
        Bullet(r, "Shipping label template — full path to your shipping label .docx template");
        Bullet(r, "Default printer — printer used for direct printing. Select (Use Word default printer) to let Word decide.");
        Bullet(r, "Admin PIN — PIN required to access sequence reset functions. Set this before going to production.");
        Spacer(r);
        H2(r, "Sequence Reset tab");
        Body(r, "Hidden inside Settings to prevent accidental use. See the Resetting sequences section for full details.");
        Spacer(r);
        H2(r, "Database connection");
        Body(r, "Access via File > Database connection. Change the SQL Express instance name or use SQL authentication instead of Windows authentication.");
        Spacer(r);
        H2(r, "Manage Sites > Shipping Sites tab");
        Bullet(r, "Add shipping site — opens form to enter site code, name, MID9, STC, ChannelAI, and full address");
        Bullet(r, "Edit selected — modify an existing shipping site record");
        Bullet(r, "Set as active — marks the selected site as the one used for barcode computation on all shipping label runs. Only one site can be active at a time.");
        Body(r, "The active shipping site is shown in the status bar at the bottom of the app as \"Shipper: [SiteCode]\". If no shipping site is active, printing shipping labels will be blocked with a warning message.");
    }

    private static void ShowReportsAnalytics(RichTextBox r)
    {
        H1(r, "Reports & analytics");
        Body(r, "The Reports tab provides tracking number visibility, usage statistics and export capabilities for accounting and operations. Access it via the Reports tab in the main navigation.");
        Spacer(r);
        H2(r, "Date range and filters");
        Body(r, "All reports share a common filter bar at the top:");
        Bullet(r, "From / To date pickers — select any date range");
        Bullet(r, "Period presets — This month, Last month, Last 3 months, Last 6 months, This year, All time (selecting a preset auto-fills the date pickers)");
        Bullet(r, "Site filter — All sites, or a specific return site");
        Bullet(r, "Refresh button — applies the current filters to all three sub-tabs");
        Spacer(r);
        H2(r, "Overview tab");
        Body(r, "Shows key performance indicators for the selected period:");
        Bullet(r, "Return labels printed in period");
        Bullet(r, "Boxes shipped in period");
        Bullet(r, "Sites with return label activity");
        Bullet(r, "Sites shipped to");
        Bullet(r, "All-time totals (return labels + boxes shipped)");
        Body(r, "Below the KPI cards, a per-site breakdown grid shows return labels and boxes shipped for every site in the selected period, sorted by volume. Sites with no activity appear in grey.");
        Spacer(r);
        H2(r, "Return Labels report");
        Body(r, "Shows every return label print run in the selected period and site filter. Each row shows:");
        Bullet(r, "Date and time of the run");
        Bullet(r, "LIBCODE and site name");
        Bullet(r, "Serial range (e.g. 000000001 → 000001000)");
        Bullet(r, "Label count");
        Bullet(r, "Run type (WORD or CSV)");
        Bullet(r, "Template used");
        Bullet(r, "Label size");
        Body(r, "The summary bar above the grid shows: total runs, total labels, and the overall serial range covered by the filtered results.");
        Spacer(r);
        H2(r, "Shipping Labels report");
        Body(r, "Shows every shipping label print run in the selected period and site filter. Each row shows:");
        Bullet(r, "Date and time of the run");
        Bullet(r, "Ship-to site (LIBCODE and name)");
        Bullet(r, "Number of boxes shipped");
        Bullet(r, "Tracking number range");
        Bullet(r, "Shipper (active shipping site code used for that run)");
        Spacer(r);
        H2(r, "Exporting reports");
        Body(r, "Each report tab has three export buttons:");
        Step(r, "1", "Apply your date range and site filters");
        Step(r, "2", "Review the data on screen");
        Step(r, "3", "Click your preferred export format:");
        Body(r, "     Export CSV — comma-separated, opens in any spreadsheet application");
        Body(r, "     Export Excel — formatted .xlsx with headers, coloured header row, alternating rows — no Excel install required");
        Body(r, "     Export PDF — formatted PDF with report title, date range, page numbers and Tadala Technologies footer");
        Step(r, "4", "Choose save location in the file dialog");
        Step(r, "5", "File is saved and a confirmation message appears");
        Spacer(r);
        H2(r, "Using reports for accounting");
        Bullet(r, "Use Return Labels report filtered by site and date to verify labels delivered match invoices");
        Bullet(r, "Use Shipping Labels report to reconcile boxes shipped with carrier manifests");
        Bullet(r, "Use Overview per-site breakdown to identify high-volume sites for resource planning");
        Bullet(r, "Export to Excel for further analysis or to share with management");
        Bullet(r, "Export to PDF for formal audit documentation");
    }

    private static void ShowPrintHistory(RichTextBox r)
    {
        H1(r, "Print History");
        Body(r, "Every label generation and shipping run is logged to the PrintHistory table. The Print History tab shows these records with full details for auditing and troubleshooting.");
        Spacer(r);
        H2(r, "Run types logged");
        Bullet(r, "WORD — return labels sent to Word for printing (sequence advanced)");
        Bullet(r, "CSV — return labels exported as CSV only (sequence advanced)");
        Bullet(r, "ATTEMPT — a Merge & Print attempt within a print session (sequence not yet advanced)");
        Bullet(r, "CANCELLED — a print session that was cancelled without confirming (sequence not advanced)");
        Bullet(r, "SHIP — a shipping label print run (shipping sequence advanced)");
        Spacer(r);
        H2(r, "Viewing history");
        Body(r, "Go to the Print History tab. The grid shows the 500 most recent entries, ordered by date descending. Each row shows date/time, site, LIBCODE, run type, template, serial range, and label count.");
        Spacer(r);
        H2(r, "Filtering history");
        Bullet(r, "Search box — filter by site name, LIBCODE, or template name (updates in real time)");
        Bullet(r, "Type dropdown — show only WORD, CSV, ATTEMPT, CANCELLED, or SHIP runs");
        Bullet(r, "Clear button — resets both filters and shows all records");
        Spacer(r);
        H2(r, "Clearing history");
        Body(r, "Click Clear all history to delete all records from the PrintHistory table. This cannot be undone. Clearing history does not affect sequences — sites continue from their current serial number regardless.");
        Notice(r, "For historical reporting, export to CSV or PDF first via the Reports tab before clearing.");
    }

    private static void ShowReset(RichTextBox r)
    {
        H1(r, "Resetting sequences");
        H2(r, "When to reset");
        Bullet(r, "Moving from demo/testing to production — reset all sites to serial 1");
        Bullet(r, "Correcting a specific site that printed wrong labels — reset to the correct number");
        Bullet(r, "Resuming from a previous system — reset to the last number used in that system");
        Spacer(r);
        H2(r, "How to reset");
        Step(r, "1", "Go to File > Settings");
        Step(r, "2", "Click the Sequence Reset tab");
        Step(r, "3", "To reset one site: select it from the dropdown and click Reset selected site");
        Step(r, "4", "To reset all sites: click Reset ALL sites");
        Step(r, "5", "In the confirmation dialog: set the starting serial number (default 1), choose whether to clear print history, type RESET, and enter your Admin PIN");
        Spacer(r);
        H2(r, "Security");
        Body(r, "Both reset actions require the Admin PIN configured in File > Settings > General tab. If no PIN is set, the reset is blocked. This prevents accidental resets in production.");
        Spacer(r);
        H2(r, "What gets reset");
        Bullet(r, "CurrentSequence — the next serial number for the site(s)");
        Bullet(r, "LastPrintedSerial and LastPrintedDate — cleared to null");
        Bullet(r, "PrintHistory records — optionally cleared (checkbox in confirmation dialog)");
    }

    private static void ShowTroubleshooting(RichTextBox r)
    {
        H1(r, "Troubleshooting");

        H2(r, "Cannot connect to database");
        Body(r, "Ensure SQL Server Express is running. Open Windows Services and check that SQL Server (SQLEXPRESS) is Started. Verify the instance name in File > Database connection.");
        Spacer(r);

        H2(r, "Word does not open after export");
        Body(r, "Ensure Microsoft Word is installed. Check that the Word template path in File > Settings is correct and the file exists. If Word is already open from a previous run, close it and try again.");
        Spacer(r);

        H2(r, "File In Use prompt from Word");
        Body(r, "The app automatically closes any running Word instances before opening a new one. If the prompt still appears, manually close Word, then retry.");
        Spacer(r);

        H2(r, "CSV import — sites not importing");
        Body(r, "Ensure your CSV has the correct headers. Column names are case-insensitive but must match: LIBCODE, MID9, and ZIP-9 are required. Sites with a LIBCODE that already exists are skipped, not overwritten.");
        Spacer(r);

        H2(r, "Sequence advanced but labels not printed");
        Body(r, "The sequence advances when you click Export. If the Word merge failed after export, you can correct the sequence using File > Settings > Sequence Reset > Reset selected site — set it to the correct starting number.");
        Spacer(r);

        H2(r, "Application will not start");
        Body(r, "Ensure the correct .NET runtime is installed (version 9 or 10). Run dotnet --version in a terminal to verify. Download from https://dotnet.microsoft.com/download if missing.");
        Spacer(r);

        H2(r, "Shipping labels — \"No active shipping site\"");
        Body(r, "Go to Manage Sites > Shipping Sites tab. If the grid is empty, add a shipping site using + Add shipping site. If sites exist but none is active, select one and click Set as active. The status bar should then show \"Shipper: [code]\".");
        Spacer(r);

        H2(r, "Shipping labels — barcode fields missing in Word");
        Body(r, "The shipping label template requires the same merge fields as return labels (TrackingCore, MOD10, GS1_128_Raw, HR_Text etc.) plus Ord and NumRecs for box numbering. Ensure your template has all these merge fields configured. If Word shows \"Invalid Merge Field\", the template field name does not match the CSV column name exactly — field names are case-sensitive.");
        Spacer(r);

        H2(r, "Reports tab — no data showing");
        Body(r, "Check the date range filter. The default is This Month — if no labels were printed this month, change the period to Last 3 months or All time. Also check the Site filter is set to All sites unless you specifically want one site.");
        Spacer(r);

        H2(r, "Excel export fails");
        Body(r, "Ensure the DocumentFormat.OpenXml package is installed (included in the application build). Also ensure the target file is not already open in Excel — close it and retry the export.");
        Spacer(r);

        H2(r, "PDF export fails");
        Body(r, "Ensure the QuestPDF package is installed (included in the application build). The app is configured for Community licence use. If you see a licence error, verify that QuestPDF.Settings.License = LicenseType.Community is set in the application startup.");
        Spacer(r);

        // Contact section
        r.SelectionFont  = new Font("Segoe UI", 1f);
        r.SelectionColor = Color.FromArgb(200, 210, 230);
        r.AppendText("------------------------------------------------" + Environment.NewLine);
        Spacer(r);
        H2(r, "Still need help?");
        Body(r, "See the Contact & support section, or contact the developer directly — details are in that section of this Help.");
    }

    private static void ShowContactSupport(RichTextBox r)
    {
        H1(r, "Contact & support");
        Body(r, "If you are unable to resolve an issue using the Troubleshooting section, contact the developer directly.");
        Spacer(r);
        H2(r, "Before contacting support");
        Bullet(r, "Check the Troubleshooting section for your specific error");
        Bullet(r, "Note your Windows version and .NET version (run: dotnet --version)");
        Bullet(r, "Note the app version — visible in Help > About");
        Bullet(r, "Take a screenshot of any error message shown");
        Spacer(r);
        r.SelectionFont  = new Font("Segoe UI", 10f, FontStyle.Bold);
        r.SelectionColor = Color.FromArgb(0, 90, 160);
        r.AppendText("  Tadala Technologies" + Environment.NewLine);
        r.SelectionFont  = new Font("Segoe UI", 9.5f);
        r.SelectionColor = Color.FromArgb(40, 40, 40);
        r.AppendText("  Email:  ");
        r.SelectionFont  = new Font("Segoe UI", 9.5f, FontStyle.Underline);
        r.SelectionColor = Color.FromArgb(0, 100, 200);
        r.AppendText("muyepaa@gmail.com" + Environment.NewLine);
        r.SelectionFont  = new Font("Segoe UI", 9f);
        r.SelectionColor = Color.FromArgb(110, 110, 110);
        r.AppendText("  When contacting support, please include:" + Environment.NewLine);
        Bullet(r, "A description of the issue and steps to reproduce it");
        Bullet(r, "The error message if any (screenshot is helpful)");
        Bullet(r, "Your Windows version and .NET version (run: dotnet --version)");
        Bullet(r, "The app version — visible in Help > About");
    }

    // ── RTF helpers ───────────────────────────────────────────
    private static void H1(RichTextBox r, string text)
    {
        r.SelectionFont  = new Font("Segoe UI", 16f, FontStyle.Bold);
        r.SelectionColor = Color.FromArgb(20, 60, 120);
        r.AppendText(text + "\n");
        r.SelectionFont  = new Font("Segoe UI", 1f);
        r.SelectionColor = Color.FromArgb(0, 100, 180);
        r.AppendText("─────────────────────────────────────────\n");
        Spacer(r);
    }

    private static void H2(RichTextBox r, string text)
    {
        r.SelectionFont  = new Font("Segoe UI", 10f, FontStyle.Bold);
        r.SelectionColor = Color.FromArgb(0, 90, 160);
        r.AppendText(text + "\n");
    }

    private static void Body(RichTextBox r, string text)
    {
        r.SelectionFont  = new Font("Segoe UI", 9.5f);
        r.SelectionColor = Color.FromArgb(40, 40, 40);
        r.AppendText(text + "\n");
    }

    private static void Bullet(RichTextBox r, string text)
    {
        r.SelectionFont  = new Font("Segoe UI", 9.5f);
        r.SelectionColor = Color.FromArgb(40, 40, 40);
        r.AppendText("  •  " + text + "\n");
    }

    private static void Step(RichTextBox r, string num, string text)
    {
        r.SelectionFont  = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        r.SelectionColor = Color.FromArgb(0, 90, 160);
        r.AppendText("  " + num + ".  ");
        r.SelectionFont  = new Font("Segoe UI", 9.5f);
        r.SelectionColor = Color.FromArgb(40, 40, 40);
        r.AppendText(text + "\n");
    }

    private static void Code(RichTextBox r, string text)
    {
        r.SelectionFont       = new Font("Courier New", 9f);
        r.SelectionColor      = Color.FromArgb(20, 100, 20);
        r.SelectionBackColor  = Color.FromArgb(240, 248, 240);
        r.AppendText("  " + text + "\n");
        r.SelectionBackColor  = Color.FromArgb(250, 251, 253);
    }

    private static void Notice(RichTextBox r, string text)
    {
        r.SelectionFont      = new Font("Segoe UI", 9f, FontStyle.Italic);
        r.SelectionColor     = Color.FromArgb(120, 80, 0);
        r.SelectionBackColor = Color.FromArgb(255, 248, 225);
        r.AppendText("  ⚠  " + text + "\n");
        r.SelectionBackColor = Color.FromArgb(250, 251, 253);
    }

    private static void Spacer(RichTextBox r)
    {
        r.SelectionFont = new Font("Segoe UI", 5f);
        r.AppendText("\n");
    }
}
