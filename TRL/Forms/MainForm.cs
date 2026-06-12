using TRL.Models;
using TRL.Services;
using TRL.Data;
using System.Diagnostics;
using System.Text;  // Add this for StringBuilder

namespace TRL.Forms;

public partial class MainForm : Form
{
    // Add this helper method right here, after the class declaration
    private string FormatSerial(int serial)
    {
        string format = AppSettings.SerialDisplayDigits == 6 ? "D6" : "D9";
        return serial.ToString(format);
    }
    
    // ... rest of your existing code
    private TabControl _tabs = null!;
    private TabPage _tabLabels = null!, _tabSites = null!, _tabHistory = null!;



    private bool _shipMergeRunning = false;

    // Sub-tabs inside "Labels"
    private TabControl _tabsLabels   = null!;
    private TabPage    _tabReturn    = null!, _tabShipping = null!;

    // ── Return Labels tab controls ────────────────────────────
    private ComboBox      _cboSite      = null!;
    private NumericUpDown _numQty       = null!;
    private Label         _lblLastSerial = null!, _lblNextSerial = null!, _lblNextRange = null!;
    private DataGridView  _dgvComputed  = null!;
    private Button        _btnExportCSV = null!, _btnOpenWord = null!;
    private Panel         _pnlSiteInfo  = null!;

    // ── Shipping Labels tab controls ──────────────────────────
    private DataGridView _dgvShipping   = null!;
    private Label        _lblShipSummary = null!;
    private Button       _btnShipSelectAll = null!, _btnShipClearAll = null!;
    private Button       _btnShipExportCSV = null!, _btnShipOpenWord = null!;
    private TextBox      _txtShipSearch    = null!;

    // ── Sites tab — two sub-tabs ──────────────────────────────
    private TabControl _tabsSites        = null!;
    private TabPage    _tabReturnSites   = null!, _tabShippingSites = null!;

    // Return Sites sub-tab
    private DataGridView _dgvSites       = null!;
    private Button       _btnAddSite     = null!, _btnEditSite = null!, _btnDeleteSite = null!;
    private TextBox      _txtSiteSearch  = null!;

    // Shipping Sites sub-tab
    private DataGridView _dgvShippingSites          = null!;
    private Button       _btnAddShippingSite        = null!,
                         _btnEditShippingSite       = null!,
                         _btnSetActiveShippingSite  = null!;
    private List<TRL.Models.ShippingSite> _shippingSites = new();

    // ── History tab ───────────────────────────────────────────
    private DataGridView _dgvHistory      = null!;
    private Button       _btnClearHistory = null!;
    private TextBox      _txtHistorySearch = null!;
    private ComboBox     _cboHistoryType   = null!;

    // ── Reports tab ───────────────────────────────────────────
    private TabPage    _tabReports      = null!;
    private TabControl _tabsReports     = null!;
    private TabPage    _tabOverview     = null!, _tabReturnReport = null!, _tabShipReport = null!;

    // Shared filter controls
    private DateTimePicker _dtpFrom        = null!, _dtpTo = null!;
    private ComboBox       _cboPeriod      = null!, _cboReportSite = null!;
    private Button         _btnRefresh     = null!;
    private bool           _suppressPeriodSync;

    // KPI number labels
    private Label _lblKpiReturnLabels = null!, _lblKpiBoxesShipped = null!,
                  _lblKpiReturnSites  = null!, _lblKpiShipSites    = null!,
                  _lblKpiAllReturn    = null!, _lblKpiAllShip      = null!;
    // KPI sub-text labels (for dynamic "of X sites" text)
    private Label _lblKpiReturnSitesSub = null!, _lblKpiShipSitesSub = null!;

    // Report grids
    private DataGridView _dgvOverview       = null!,
                         _dgvReturnReport   = null!,
                         _dgvShippingReport = null!;

    // Summary labels for report sub-tabs
    private Label _lblReturnSummary = null!, _lblShipReportSummary = null!;

    // Report data
    private List<TRL.Models.ReturnLabelReport>   _returnReportData   = new();
    private List<TRL.Models.ShippingLabelReport> _shippingReportData = new();
    private TRL.Models.ReportSummary?            _reportSummary;
    private List<TRL.Models.SiteBreakdown>       _siteBreakdown      = new();
    private List<Site>                                             _reportSites        = new();

    // ── Status strip ──────────────────────────────────────────
    private ToolStripStatusLabel _statSites = null!, _statShipper = null!, _statTotal = null!, _statToday = null!;

    private List<Site> _sites = new();

    // Column indices in the shipping grid (set once in BuildShippingTab)
    private const int ShipColSelected = 0;
    private const int ShipColLibcode  = 1;
    private const int ShipColName     = 2;
    private const int ShipColAdd1     = 3;
    private const int ShipColAdd2     = 4;
    private const int ShipColAdd3     = 5;
    private const int ShipColAdd4     = 6;
    private const int ShipColBoxes    = 7;
    private const int ShipColLastUsed = 8;

    public MainForm()
    {
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text        = "TRL  |  Track Return Labels";
        Size        = new Size(1100, 740);
        MinimumSize = new Size(920, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        // ── Menu ──────────────────────────────────────────────
        var menu = new MenuStrip();

        var fileMenu      = new ToolStripMenuItem("File");
        var miDbSettings  = new ToolStripMenuItem("Database connection...", null, (s, e) => ShowConnectionSettings());
        var miAppSettings = new ToolStripMenuItem("Settings...",            null, (s, e) => ShowAppSettings());
        var miExit        = new ToolStripMenuItem("Exit",                   null, (s, e) => Application.Exit());
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { miDbSettings, miAppSettings, new ToolStripSeparator(), miExit });

        var sitesMenu     = new ToolStripMenuItem("Sites");
        var miImport      = new ToolStripMenuItem("Import sites from CSV...",        null, (s, e) => ImportSitesFromCsv());
        var miTemplate    = new ToolStripMenuItem("Download CSV import template...", null, (s, e) => DownloadImportTemplate());
        var miExportSites = new ToolStripMenuItem("Export all sites to CSV...",      null, (s, e) => ExportSitesToCsv());
        sitesMenu.DropDownItems.AddRange(new ToolStripItem[] { miImport, miTemplate, new ToolStripSeparator(), miExportSites });

        var helpMenu = new ToolStripMenuItem("Help");
        var miHelp   = new ToolStripMenuItem("Help contents...", null, (s, e) => { using var f = new HelpForm();  f.ShowDialog(this); });
        var miAbout  = new ToolStripMenuItem("About...",         null, (s, e) => { using var f = new AboutForm(); f.ShowDialog(this); });
        helpMenu.DropDownItems.AddRange(new ToolStripItem[] { miHelp, new ToolStripSeparator(), miAbout });

        menu.Items.Add(fileMenu);
        menu.Items.Add(sitesMenu);
        menu.Items.Add(helpMenu);
        MainMenuStrip = menu;

        // ── Status strip ──────────────────────────────────────
        var strip = new StatusStrip();
        _statSites   = new ToolStripStatusLabel("Return sites: 0") { BorderSides = ToolStripStatusLabelBorderSides.Right };
        _statShipper = new ToolStripStatusLabel("Shipper: —")      { BorderSides = ToolStripStatusLabelBorderSides.Right };
        _statTotal   = new ToolStripStatusLabel("Total labels: 0") { BorderSides = ToolStripStatusLabelBorderSides.Right };
        _statToday   = new ToolStripStatusLabel("Today: 0");
        strip.Items.AddRange(new ToolStripItem[] { _statSites, _statShipper, _statTotal, _statToday });

        // ── Top-level tabs ────────────────────────────────────
        _tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        _tabLabels  = new TabPage("  Labels  ");
        _tabSites   = new TabPage("  Manage Sites  ");
        _tabHistory = new TabPage("  Print History  ");
        _tabReports = new TabPage("  Reports  ");
        _tabs.TabPages.AddRange(new[] { _tabLabels, _tabSites, _tabHistory, _tabReports });

        // Add in correct dock order: Fill first, then Bottom, then Top
        Controls.Add(_tabs);
        Controls.Add(strip);
        Controls.Add(menu);

        BuildLabelsTab();
        BuildSitesTab();
        BuildHistoryTab();
        BuildReportsTab();

        _tabs.SelectedIndexChanged += (s, e) =>
        {
            if (_tabs.SelectedTab == _tabHistory) LoadHistory();
            if (_tabs.SelectedTab == _tabReports) ApplyReportFilters();
        };
    }

    // ══════════════════════════════════════════════════════════
    //  LABELS TAB — hosts two sub-tabs
    // ══════════════════════════════════════════════════════════
    private void BuildLabelsTab()
    {
        _tabLabels.Padding = new Padding(0);

        _tabsLabels  = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 5) };
        _tabReturn   = new TabPage("  Return Labels  ");
        _tabShipping = new TabPage("  Shipping Labels  ");
        _tabsLabels.TabPages.AddRange(new[] { _tabReturn, _tabShipping });

        // Must add Fill control last
        _tabLabels.Controls.Add(_tabsLabels);

        BuildReturnTab();
        BuildShippingTab();

        _tabsLabels.SelectedIndexChanged += (s, e) =>
        {
            if (_tabsLabels.SelectedTab == _tabShipping)
                RefreshShippingGrid();
        };
    }

    // ══════════════════════════════════════════════════════════
    //  RETURN LABELS SUB-TAB  (existing logic, unchanged)
    // ══════════════════════════════════════════════════════════
   private void BuildReturnTab()
{
    var p = _tabReturn;
    p.Padding = new Padding(12);

    // Config group - increased height to accommodate batch controls
    var grpConfig = new GroupBox { Text = "Label Configuration", Dock = DockStyle.Top, Height = 160, Padding = new Padding(10) };

    var lblSite = new Label { Text = "Site:", AutoSize = true, Location = new Point(10, 28) };
    _cboSite = new ComboBox
    {
        DropDownStyle = ComboBoxStyle.DropDown,
        Location = new Point(50, 25),
        Width = 380,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems
    };
    _cboSite.SelectedIndexChanged += OnSiteChanged;
    var lblSiteHint = new Label
    {
        Text = "Start typing to filter sites",
        AutoSize = true,
        Location = new Point(52, 52),
        ForeColor = Color.FromArgb(160, 160, 160),
        Font = new Font("Segoe UI", 7.5f, FontStyle.Italic)
    };

    var lblQty = new Label { Text = "Quantity:", AutoSize = true, Location = new Point(445, 28) };
    _numQty = new NumericUpDown { Location = new Point(510, 25), Width = 90, Minimum = 1, Maximum = 9999999, Value = 1, ThousandsSeparator = true };
    _numQty.ValueChanged += OnQtyChanged;

    // Batch Size selector
    var lblBatchSize = new Label { Text = "Batch Size:", AutoSize = true, Location = new Point(445, 58) };
    var numBatchSize = new NumericUpDown
    {
        Location = new Point(510, 55),
        Width = 90,
        Minimum = 1000,
        Maximum = 10000,
        Value = 1000,
        Increment = 1000,
        ThousandsSeparator = true,
        Enabled = false
    };

    var chkBatchMode = new CheckBox
    {
        Text = "Batch Print Mode (multiples of 1000)",
        Location = new Point(10, 65),
        AutoSize = true,
        Checked = false
    };
    chkBatchMode.CheckedChanged += (s, e) =>
    {
        numBatchSize.Enabled = chkBatchMode.Checked;
        _numQty.Enabled = !chkBatchMode.Checked;
        if (chkBatchMode.Checked)
        {
            // Round up to nearest 1000
            decimal val = Math.Ceiling(_numQty.Value / 1000) * 1000;
            _numQty.Value = val;
        }
    };

    grpConfig.Controls.AddRange(new Control[] { 
        lblSite, _cboSite, lblSiteHint, 
        lblQty, _numQty, 
        lblBatchSize, numBatchSize, chkBatchMode 
    });
    p.Controls.Add(grpConfig);

    // Sequence info panel
    _pnlSiteInfo = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10, 5, 10, 0), Visible = false };
    var seqTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
    seqTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    seqTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
    seqTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    seqTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

    Func<string, Label> mkHdr = t => new Label { Text = t, AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8f) };
    Func<Color, Label> mkVal = c => new Label { AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = c };

    _lblLastSerial = mkVal(Color.DarkBlue);
    _lblNextSerial = mkVal(Color.DarkBlue);
    _lblNextRange = mkVal(Color.DarkGreen);

    seqTable.Controls.Add(mkHdr("Last printed serial:"), 0, 0);
    seqTable.Controls.Add(_lblLastSerial, 0, 1);
    seqTable.Controls.Add(mkHdr("Next serial (start of batch):"), 2, 0);
    seqTable.Controls.Add(_lblNextSerial, 2, 1);
    seqTable.Controls.Add(_lblNextRange, 3, 1);
    _pnlSiteInfo.Controls.Add(seqTable);
    p.Controls.Add(_pnlSiteInfo);

    // Hint — added before buttons (Bottom dock order: outermost first)
    var pnlHint = new Panel { Dock = DockStyle.Bottom, Height = 22 };
    pnlHint.Controls.Add(new Label
    {
        Text = "Print Labels via Word merges data with your Word template and sends to printer",
        Dock = DockStyle.Fill,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8f),
        TextAlign = ContentAlignment.MiddleLeft
    });
    p.Controls.Add(pnlHint);

    // Buttons panel
    var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 4, 0, 0) };
    _btnExportCSV = MakeButton("Export CSV only", Color.FromArgb(100, 100, 100));
    _btnOpenWord = MakeButton("Print Labels via Word", Color.FromArgb(0, 120, 212));
    var btnBatchPrint = MakeButton("📦 Batch Print", Color.FromArgb(0, 150, 100));

    _btnExportCSV.Click += OnExportCSV;
    _btnOpenWord.Click += OnOpenWord;
    btnBatchPrint.Click += OnBatchPrint;

    LayoutButtons(pnlBtns, new Control[] { _btnExportCSV, _btnOpenWord, btnBatchPrint });
    p.Controls.Add(pnlBtns);

    // Computed fields grid — Fill, added last
    var grpComputed = new GroupBox { Text = "Computed barcode fields (first label in batch)", Dock = DockStyle.Fill, Padding = new Padding(8) };
    _dgvComputed = MakeGrid(false);
    _dgvComputed.Columns.Add("Field", "Field");
    _dgvComputed.Columns.Add("Value", "Value");
    _dgvComputed.Columns["Field"]!.Width = 160;
    _dgvComputed.Columns["Value"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    _dgvComputed.Columns["Field"]!.DefaultCellStyle.ForeColor = Color.Gray;
    _dgvComputed.Columns["Value"]!.DefaultCellStyle.Font = new Font("Courier New", 9f);
    _dgvComputed.Dock = DockStyle.Fill;
    grpComputed.Controls.Add(_dgvComputed);
    p.Controls.Add(grpComputed);
}



private async void OnBatchPrint(object? s, EventArgs e)
{
    if (!ValidateWordSettings()) return;

    var site = GetSelectedSite();
    if (site == null) 
    { 
        MessageBox.Show("Please select a site.", "No site selected", 
            MessageBoxButtons.OK, MessageBoxIcon.Warning); 
        return; 
    }

    // Check if templates are available
    if (AppSettings.WordTemplates.Count == 0)
    {
        MessageBox.Show("No Word templates configured. Please add templates in Settings.", 
            "No Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    // Let user select template for batch printing
    var templateForm = new Form
    {
        Text = "Select Template for Batch Printing",
        Size = new Size(450, 180),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false
    };

    var lblSelect = new Label
    {
        Text = $"Site: {site.SiteName} [{site.LIBCODE}]\nSelect Word template for batch printing:",
        Location = new Point(20, 20),
        Size = new Size(400, 35),
        Font = new Font("Segoe UI", 9f)
    };

    var cboTemplate = new ComboBox
    {
        Location = new Point(20, 65),
        Width = 390,
        Height = 28,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 9.5f)
    };
    foreach (var t in AppSettings.WordTemplates)
        cboTemplate.Items.Add(t.Name);
    cboTemplate.SelectedIndex = 0;

    var btnTemplateOk = new Button
    {
        Text = "OK",
        Location = new Point(260, 105),
        Size = new Size(70, 30),
        DialogResult = DialogResult.OK,
        BackColor = Color.FromArgb(0, 120, 212),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };
    btnTemplateOk.FlatAppearance.BorderSize = 0;

    var btnTemplateCancel = new Button
    {
        Text = "Cancel",
        Location = new Point(340, 105),
        Size = new Size(70, 30),
        DialogResult = DialogResult.Cancel,
        FlatStyle = FlatStyle.Flat
    };

    templateForm.Controls.AddRange(new Control[] { lblSelect, cboTemplate, btnTemplateOk, btnTemplateCancel });

    if (templateForm.ShowDialog() != DialogResult.OK) return;

    WordTemplate selectedTemplate = AppSettings.WordTemplates[cboTemplate.SelectedIndex];
    string templatePath = selectedTemplate.Path;
    string templateName = selectedTemplate.Name;

    int totalQty = (int)_numQty.Value;
    int batchSize = 1000;
    
    // Validate quantity is multiple of batchSize
    if (totalQty % batchSize != 0)
    {
        MessageBox.Show($"Quantity must be a multiple of {batchSize} for batch printing.\n" +
                        $"Current quantity: {totalQty}\n" +
                        $"Suggested: {((totalQty / batchSize) + 1) * batchSize}",
            "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    int batches = totalQty / batchSize;
    
    string startSerialDisplay = FormatSerial(site.CurrentSequence);
    string endSerialDisplay = FormatSerial(site.CurrentSequence + totalQty - 1);
    
    var result = MessageBox.Show(
        $"Ready to print {totalQty:N0} labels in {batches} batches of {batchSize:N0}.\n\n" +
        $"Site: {site.SiteName} [{site.LIBCODE}]\n" +
        $"Template: {templateName}\n" +
        $"Starting serial: {startSerialDisplay}\n" +
        $"Ending serial: {endSerialDisplay}\n\n" +
        $"This will create {batches} Word documents.\n" +
        $"Click OK to begin batch printing.",
        "Batch Print Confirmation",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Information);

    if (result != DialogResult.OK) return;

    // Disable buttons during batch processing
    _btnExportCSV.Enabled = false;
    _btnOpenWord.Enabled = false;
    
    // Use the same Output folder as single prints
    string outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TRL", "Output");
    
    try
    {
        Directory.CreateDirectory(outputFolder);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Failed to create output folder:\n{outputFolder}\n\nError: {ex.Message}", 
            "Folder Creation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        _btnExportCSV.Enabled = true;
        _btnOpenWord.Enabled = true;
        return;
    }
    
    var progressForm = new Form
    {
        Text = $"Batch Printing - {site.SiteName}",
        Size = new Size(550, 400),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false
    };
    
    var progressBar = new ProgressBar
    {
        Dock = DockStyle.Top,
        Height = 30,
        Minimum = 0,
        Maximum = batches,
        Style = ProgressBarStyle.Blocks
    };
    
    var lblProgress = new Label
    {
        Dock = DockStyle.Top,
        Height = 45,
        Text = "Preparing...",
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10f)
    };
    
    var txtLog = new TextBox
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 9f)
    };
    
    var btnCancelPrint = new Button
    {
        Text = "Cancel",
        Dock = DockStyle.Bottom,
        Height = 35,
        Enabled = true
    };
    
    progressForm.Controls.Add(txtLog);
    progressForm.Controls.Add(lblProgress);
    progressForm.Controls.Add(progressBar);
    progressForm.Controls.Add(btnCancelPrint);
    
    bool cancelled = false;
    btnCancelPrint.Click += (s2, e2) => { cancelled = true; btnCancelPrint.Enabled = false; };
    
    progressForm.Shown += async (s2, e2) =>
    {
        try
        {
            int currentSerial = site.CurrentSequence;
            int successCount = 0;
            int calibrationCount = AppSettings.CalibrationCount;
            int timeoutMinutes = AppSettings.WordMergeTimeoutMinutes;
            
            AppendLog(txtLog, $"Template: {templateName}");
            AppendLog(txtLog, $"Output folder: {outputFolder}");
            AppendLog(txtLog, $"Timeout per document: {timeoutMinutes} minutes");
            AppendLog(txtLog, $"Calibration labels per batch: {calibrationCount}");
            AppendLog(txtLog, $"");
            
            for (int batch = 1; batch <= batches; batch++)
            {
                if (cancelled)
                {
                    AppendLog(txtLog, $"❌ CANCELLED at batch {batch}/{batches}");
                    break;
                }
                
                int batchStartSerial = currentSerial;
                int batchQty = Math.Min(batchSize, totalQty - (batch - 1) * batchSize);
                int batchEndSerial = batchStartSerial + batchQty - 1;
                
                AppendLog(txtLog, $"");
                AppendLog(txtLog, $"📄 Batch {batch}/{batches}: Serial {FormatSerial(batchStartSerial)} → {FormatSerial(batchEndSerial)}");
                
                // Update progress UI
                lblProgress.Text = $"Processing batch {batch}/{batches} (Serials: {FormatSerial(batchStartSerial)} → {FormatSerial(batchEndSerial)})";
                progressBar.Value = batch - 1;
                
                // Create batch with calibration rows
                var batchRows = new List<LabelRow>();
                
                // Add calibration rows (using configurable count)
                for (int i = 1; i <= calibrationCount; i++)
                {
                    batchRows.Add(BarcodeService.BuildCalibrationLabelRow(site, batchStartSerial, i));
                }
                
                // Add real labels for this batch
                for (int i = 0; i < batchQty; i++)
                {
                    batchRows.Add(BarcodeService.BuildLabelRow(site, batchStartSerial + i));
                }
                
                // Create CSV file - use ENDING serial for filename
                string csvFileName = $"TRL_batch_{site.LIBCODE}_{FormatSerial(batchEndSerial)}.csv";
                var csvPath = Path.Combine(AppSettings.TempFolder, csvFileName);
                File.WriteAllText(csvPath, BarcodeService.ToCsv(batchRows));
                AppendLog(txtLog, $"   CSV created: {csvFileName}");

                // Verify CSV has data
                var csvLines = File.ReadAllLines(csvPath);
                AppendLog(txtLog, $"   CSV has {csvLines.Length - 1} data rows (should be {calibrationCount + batchQty})");

                if (csvLines.Length <= 1)
                {
                    throw new Exception($"CSV file has no data rows! Path: {csvPath}");
                }

                // Create Word document with batch-specific name - use ENDING serial
                string docFilename = $"{site.LIBCODE}_{FormatSerial(batchEndSerial)}.docx";
                var outputDocxPath = Path.Combine(outputFolder, docFilename);
                
                AppendLog(txtLog, $"   Creating Word document (may take several minutes)...");
                var saveStartTime = DateTime.Now;
                
 
                // Perform the mail merge with the SELECTED template and metadata
                WordMergeService.SaveMergedDocument(
                    csvPath, 
                    templatePath,            
                    outputDocxPath,
                    siteName: site.SiteName,
                    startSerial: batchStartSerial,
                    endSerial: batchEndSerial
                );
                                
                var saveDuration = DateTime.Now - saveStartTime;
                AppendLog(txtLog, $"   ✅ Document saved in {saveDuration.TotalSeconds:F1} seconds: {docFilename}");
                
                // Advance the sequence for real labels only
             // Advance the sequence for real labels only
                var actualFrom = Program.DB.AdvanceSequence(site.Id, batchQty, out var newNext);
                site.CurrentSequence = newNext;
                site.LastPrintedSerial = actualFrom + batchQty - 1;
                site.LastPrintedDate = DateTime.Now;

                // Log history for this batch
                Program.DB.InsertHistory(new PrintHistoryEntry
                {
                    SiteId = site.Id,
                    LIBCODE = site.LIBCODE,
                    SiteName = site.SiteName,
                    SerialFrom = actualFrom,
                    SerialTo = actualFrom + batchQty - 1,
                    LabelCount = batchQty,
                    RunType = "BATCH",
                    LabelSize = "",
                    PrintedBy = Environment.UserName,
                    TemplateName = $"Batch {batch}/{batches} - {templateName}"
                });

                successCount++;
                currentSerial = newNext;

                AppendLog(txtLog, $"   ✅ Batch {batch} complete. Serial now at: {FormatSerial(site.CurrentSequence)}");

                // Wait random time between batches (3-5 minutes)
                if (batch < batches && !cancelled)
                {
                    int minSeconds = 180;  // 3 minutes
                    int maxSeconds = 300;  // 5 minutes
                    int delaySeconds = Random.Shared.Next(minSeconds, maxSeconds + 1);
                    AppendLog(txtLog, $"   ⏱️ Waiting {delaySeconds} seconds (random between 3-5 min) before next batch...");
                    await Task.Delay(delaySeconds * 1000);
                }
            }
            
            progressBar.Value = batches;
            AppendLog(txtLog, $"");
            AppendLog(txtLog, $"🎉 BATCH PRINTING COMPLETE!");
            AppendLog(txtLog, $"   Successful batches: {successCount}/{batches}");
            AppendLog(txtLog, $"   Total labels printed: {successCount * batchSize:N0}");
            AppendLog(txtLog, $"   Final serial: {FormatSerial(site.CurrentSequence)}");
            
            lblProgress.Text = $"Complete! {successCount}/{batches} batches successful";
            
            UpdateStats();
            if (_tabs.SelectedTab == _tabHistory) LoadHistory();
            RefreshGeneratePreview();
            
            btnCancelPrint.Text = "Close";
            MessageBox.Show($"Batch printing complete!\n\n" +
                           $"Template: {templateName}\n" +
                           $"Successful batches: {successCount}/{batches}\n" +
                           $"Total labels: {successCount * batchSize:N0}\n" +
                           $"Documents saved in:\n{outputFolder}",
                "Batch Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendLog(txtLog, $"❌ ERROR: {ex.Message}");
            MessageBox.Show($"Error during batch printing:\n{ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnExportCSV.Enabled = true;
            _btnOpenWord.Enabled = true;
            btnCancelPrint.Enabled = true;
        }
    };
    
    progressForm.ShowDialog(this);
}








private void OnOpenWord(object? s, EventArgs e)
{
    if (!ValidateWordSettings()) return;

    var site = GetSelectedSite();
    if (site == null) { MessageBox.Show("Please select a site.", "No site selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
    var qty = (int)_numQty.Value;
    var from = site.CurrentSequence;

    if (!Directory.Exists(AppSettings.TempFolder))
        Directory.CreateDirectory(AppSettings.TempFolder);

    // Build batch with configurable calibration rows
    var batch = new List<LabelRow>();
    
    int calibrationCount = AppSettings.CalibrationCount;
    
    // Add calibration rows (0 to configured number)
    for (int i = 1; i <= calibrationCount; i++)
    {
        batch.Add(BarcodeService.BuildCalibrationLabelRow(site, from, i));
    }
    
    // Add real labels with actual serial numbers
    for (int i = 0; i < qty; i++)
    {
        batch.Add(BarcodeService.BuildLabelRow(site, from + i));
    }
    
    var csvPath = Path.Combine(AppSettings.TempFolder, "TRL_labels_" + site.LIBCODE + "_current.csv");
    try 
    { 
        File.WriteAllText(csvPath, BarcodeService.ToCsv(batch)); 
    }
    catch (Exception ex) 
    { 
        MessageBox.Show("Could not write temp CSV:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
        return; 
    }

    // Create output path for Word document
    string outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TRL", "Output");
    Directory.CreateDirectory(outputFolder);
    string outputDocxPath = Path.Combine(outputFolder, $"{site.LIBCODE}_{FormatSerial(from)}.docx");

    // Define the callback
    Action<string, string> onPrintAttempt = (templateName, runType) =>
    {
        Program.DB.InsertHistory(new PrintHistoryEntry
        {
            SiteId = site.Id,
            LIBCODE = site.LIBCODE,
            SiteName = site.SiteName,
            SerialFrom = from,
            SerialTo = from + qty - 1,
            LabelCount = qty,
            RunType = runType,
            LabelSize = "",
            PrintedBy = Environment.UserName,
            TemplateName = templateName
        });
        UpdateStats();
        if (_tabs.SelectedTab == _tabHistory) LoadHistory();
    };

    // CORRECT ORDER: site, fromSerial, qty, csvPath, outputDocxPath, templates, callback
    using var session = new PrintSessionForm(site, from, qty, csvPath, outputDocxPath, AppSettings.WordTemplates, onPrintAttempt);

    if (session.ShowDialog(this) == DialogResult.OK)
    {
        // Confirmed — advance sequence by qty (real labels only, NOT including calibration)
        var actualFrom = Program.DB.AdvanceSequence(site.Id, qty, out var newNext);
        site.CurrentSequence = newNext; 
        site.LastPrintedSerial = actualFrom + qty - 1; 
        site.LastPrintedDate = DateTime.Now;
        
        Program.DB.InsertHistory(new PrintHistoryEntry
        {
            SiteId = site.Id,
            LIBCODE = site.LIBCODE,
            SiteName = site.SiteName,
            SerialFrom = actualFrom,
            SerialTo = actualFrom + qty - 1,
            LabelCount = qty,
            RunType = "WORD",
            LabelSize = "",
            PrintedBy = Environment.UserName,
            TemplateName = string.IsNullOrEmpty(session.LastTemplateName) ? null : session.LastTemplateName
        });
        UpdateStats();
        if (_tabs.SelectedTab == _tabHistory) LoadHistory();
        RefreshGeneratePreview();
    }
    else
    {
        // Cancelled — log cancellation (still only real labels count)
        if (session.AttemptedTemplates.Count > 0)
        {
            Program.DB.InsertHistory(new PrintHistoryEntry
            {
                SiteId = site.Id,
                LIBCODE = site.LIBCODE,
                SiteName = site.SiteName,
                SerialFrom = from,
                SerialTo = from + qty - 1,
                LabelCount = qty,
                RunType = "CANCELLED",
                LabelSize = "",
                PrintedBy = Environment.UserName,
                TemplateName = string.Join(", ", session.AttemptedTemplates)
            });
            UpdateStats();
            if (_tabs.SelectedTab == _tabHistory) LoadHistory();
        }
    }
}




private void AppendLog(TextBox txtLog, string message)
{
    if (txtLog.InvokeRequired)
    {
        txtLog.Invoke(new Action(() => AppendLog(txtLog, message)));
        return;
    }
    txtLog.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
    txtLog.SelectionStart = txtLog.TextLength;
    txtLog.ScrollToCaret();
}

    // ══════════════════════════════════════════════════════════
    //  SHIPPING LABELS SUB-TAB
    // ══════════════════════════════════════════════════════════
    private void BuildShippingTab()
{
    var p = _tabShipping;
    p.Padding = new Padding(12);

    // ── Header banner ─────────────────────────────────────
    var pnlHeader = new Panel
    {
        Dock      = DockStyle.Top,
        Height    = 46,
        BackColor = Color.FromArgb(240, 244, 252),
        Padding   = new Padding(10, 8, 10, 6)
    };
    pnlHeader.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(200, 210, 230) });
    pnlHeader.Controls.Add(new Label
    {
        Text      = "Check the sites to include, enter number of boxes per site. Only checked sites with Boxes > 0 will be printed.",
        Dock      = DockStyle.Fill,
        ForeColor = Color.FromArgb(15, 55, 115),
        Font      = new Font("Segoe UI", 9f),
        AutoSize  = false
    });
    
    // ── Summary bar (Bottom — add before Fill) ────────────
    var pnlSummary = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(4, 4, 4, 0) };
    _lblShipSummary = new Label
    {
        Dock      = DockStyle.Fill,
        AutoSize  = false,
        ForeColor = Color.FromArgb(15, 55, 115),
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };
    pnlSummary.Controls.Add(_lblShipSummary);

    // ── Hint (Bottom) ─────────────────────────────────────
    var pnlHint = new Panel { Dock = DockStyle.Bottom, Height = 22 };
    pnlHint.Controls.Add(new Label
    {
        Text      = "Print Shipping Labels merges site box quantities with your shipping template and sends to printer",
        Dock      = DockStyle.Fill,
        ForeColor = Color.Gray,
        Font      = new Font("Segoe UI", 8f),
        TextAlign = ContentAlignment.MiddleLeft
    });

    // ── Buttons (Bottom) ──────────────────────────────────
    var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 4, 0, 0) };
    _btnShipSelectAll = MakeButton("Select all", Color.FromArgb(80, 80, 80));
    _btnShipClearAll  = MakeButton("Clear all", Color.FromArgb(160, 30, 30));
    _btnShipExportCSV = MakeButton("Export CSV only", Color.FromArgb(100, 100, 100));
    _btnShipOpenWord  = MakeButton("Print Shipping Labels", Color.FromArgb(0, 120, 212));
    
    _btnShipSelectAll.Click += OnShipSelectAll;
    _btnShipClearAll.Click  += OnShipClearAll;
    _btnShipExportCSV.Click += OnShipExportCSV;
    _btnShipOpenWord.Click  += OnShipOpenWord;
    
    LayoutButtons(pnlBtns, new Control[] { _btnShipSelectAll, _btnShipClearAll, _btnShipExportCSV, _btnShipOpenWord });

    // ── Search bar (Top — between header and grid) ────────
    var pnlShipSearch = new Panel
    {
        Dock    = DockStyle.Top,
        Height  = 38,
        Padding = new Padding(10, 5, 10, 4)
    };
    var lblShipSearch = new Label
    {
        Text      = "Search:",
        AutoSize  = true,
        Location  = new Point(0, 9),
        ForeColor = Color.FromArgb(60, 60, 60)
    };
    _txtShipSearch = new TextBox
    {
        Location        = new Point(56, 6),
        Width           = 280,
        Font            = new Font("Segoe UI", 9.5f),
        BorderStyle     = BorderStyle.FixedSingle,
        PlaceholderText = "Filter by LIBCODE or site name…"
    };
    _txtShipSearch.TextChanged += OnShipSearchChanged;
    var btnShipClearSearch = new Button
    {
        Text      = "Clear",
        Location  = new Point(344, 5),
        Size      = new Size(58, 24),
        BackColor = Color.FromArgb(225, 225, 225),
        ForeColor = Color.FromArgb(50, 50, 50),
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 8.5f)
    };
    btnShipClearSearch.FlatAppearance.BorderSize = 0;
    btnShipClearSearch.Click += (s2, e2) => _txtShipSearch.Text = "";
    pnlShipSearch.Controls.AddRange(new Control[] { lblShipSearch, _txtShipSearch, btnShipClearSearch });

    // ── Dock order: Fill first, Bottom panels, Top last ───
    _dgvShipping = MakeGrid(false);
    _dgvShipping.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    _dgvShipping.ColumnHeadersVisible = true;
    _dgvShipping.ColumnHeadersHeight = 30;
    _dgvShipping.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
    _dgvShipping.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 232, 236);
    _dgvShipping.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
    _dgvShipping.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
    _dgvShipping.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 232, 236);
    _dgvShipping.EnableHeadersVisualStyles = false;
    _dgvShipping.AllowUserToAddRows = false;
    _dgvShipping.AllowUserToDeleteRows = false;
    _dgvShipping.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

    var colSelected = new DataGridViewCheckBoxColumn
    {
        Name = "Selected",
        HeaderText = "",
        Width = 30,
        ReadOnly = false
    };
    colSelected.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    colSelected.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
    _dgvShipping.Columns.Add(colSelected);

    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE", HeaderText = "LIBCODE", Width = 90, ReadOnly = true });
    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName", HeaderText = "Site name", Width = 200, ReadOnly = true });
    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "SHIP_ADD_1", HeaderText = "Address 1", Width = 160, ReadOnly = true });
    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "SHIP_ADD_2", HeaderText = "Address 2", Width = 140, ReadOnly = true });
    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "SHIP_ADD_3", HeaderText = "Address 3", Width = 140, ReadOnly = true });
    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "SHIP_ADD_4", HeaderText = "Address 4", Width = 120, ReadOnly = true });

    var colBoxes = new DataGridViewTextBoxColumn
    {
        Name = "Boxes",
        HeaderText = "Boxes",
        Width = 70,
        ReadOnly = false
    };
    colBoxes.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 230);
    colBoxes.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
    colBoxes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    colBoxes.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
    _dgvShipping.Columns.Add(colBoxes);

    _dgvShipping.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastUsed", HeaderText = "Last used", Width = 80, ReadOnly = true });
    _dgvShipping.Columns["LastUsed"]!.DefaultCellStyle.ForeColor = Color.Gray;
    _dgvShipping.Columns["LastUsed"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

    _dgvShipping.Dock = DockStyle.Fill;
    _dgvShipping.CellValueChanged += OnShippingCellChanged;
    _dgvShipping.CellValidating += OnShippingCellValidating;
    _dgvShipping.CurrentCellDirtyStateChanged += (s, e) =>
    {
        if (_dgvShipping.IsCurrentCellDirty) _dgvShipping.CommitEdit(DataGridViewDataErrorContexts.Commit);
    };

    p.Controls.Add(_dgvShipping);   // Fill — must be first
    p.Controls.Add(pnlSummary);     // Bottom
    p.Controls.Add(pnlHint);        // Bottom
    p.Controls.Add(pnlBtns);        // Bottom
    p.Controls.Add(pnlShipSearch);  // Top — docks below pnlHeader
    p.Controls.Add(pnlHeader);      // Top — must be last (topmost)
}
    // ══════════════════════════════════════════════════════════
    //  SITES TAB — two sub-tabs
    // ══════════════════════════════════════════════════════════
    private void BuildSitesTab()
    {
        var p = _tabSites;
        p.Padding = new Padding(0);

        _tabsSites       = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 5) };
        _tabReturnSites   = new TabPage("  Return Sites  ");
        _tabShippingSites = new TabPage("  Shipping Sites  ");
        _tabsSites.TabPages.AddRange(new[] { _tabReturnSites, _tabShippingSites });
        p.Controls.Add(_tabsSites);

        BuildReturnSitesSubTab();
        BuildShippingSitesSubTab();
    }

    private void BuildReturnSitesSubTab()
    {
        var p = _tabReturnSites;
        p.Padding = new Padding(12);

        var pnlBtns = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        _btnAddSite    = MakeButton("+ Add site",      Color.FromArgb(0, 120, 212));
        _btnEditSite   = MakeButton("Edit selected",   Color.FromArgb(80, 80, 80));
        _btnDeleteSite = MakeButton("Delete selected", Color.FromArgb(200, 50, 50));
        _btnAddSite.Click    += (s, e) => OpenSiteForm(null);
        _btnEditSite.Click   += (s, e) => EditSelectedSite();
        _btnDeleteSite.Click += (s, e) => DeleteSelectedSite();
        LayoutButtons(pnlBtns, new Control[] { _btnAddSite, _btnEditSite, _btnDeleteSite });

        _txtSiteSearch = new TextBox
        {
            Width           = 220,
            Font            = new Font("Segoe UI", 9.5f),
            BorderStyle     = BorderStyle.FixedSingle,
            PlaceholderText = "Filter by LIBCODE, name or MID9…"
        };
        _txtSiteSearch.TextChanged += OnSiteSearchChanged;
        pnlBtns.Controls.Add(_txtSiteSearch);
        pnlBtns.Layout += (s2, e2) =>
            _txtSiteSearch.Location = new Point(pnlBtns.ClientSize.Width - _txtSiteSearch.Width - 4, 9);

        _dgvSites = MakeGrid(true);
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE",     HeaderText = "LIBCODE",      Width = 100 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",    HeaderText = "Site name",    Width = 220 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "MID9",        HeaderText = "MID9",         Width = 120 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "ZIP9Dash",    HeaderText = "ZIP-9",        Width = 110 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "STC",         HeaderText = "STC",          Width = 60  });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurSeq",      HeaderText = "Next serial",  Width = 120 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastPrinted", HeaderText = "Last printed", Width = 210 });
        _dgvSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalLabels", HeaderText = "Total labels", Width = 100 });
        _dgvSites.Dock = DockStyle.Fill;
        _dgvSites.DoubleClick += (s, e) => EditSelectedSite();

        p.Controls.Add(_dgvSites);
        p.Controls.Add(pnlBtns);
    }

    private void BuildShippingSitesSubTab()
    {
        var p = _tabShippingSites;
        p.Padding = new Padding(12);

        // Info banner
        var pnlInfo = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            BackColor = Color.FromArgb(240, 248, 240),
            Padding   = new Padding(10, 5, 10, 4)
        };
        pnlInfo.Controls.Add(new Label
        {
            Dock      = DockStyle.Fill,
            AutoSize  = false,
            Text      = "The active shipping site provides barcode credentials for outbound shipping labels. Only one site may be active at a time.",
            ForeColor = Color.FromArgb(0, 100, 50),
            Font      = new Font("Segoe UI", 8.5f)
        });
        pnlInfo.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(180, 220, 190) });

        // Buttons
        var pnlBtns = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        _btnAddShippingSite       = MakeButton("+ Add",           Color.FromArgb(0, 120, 212));
        _btnEditShippingSite      = MakeButton("Edit selected",   Color.FromArgb(80, 80, 80));
        _btnSetActiveShippingSite = MakeButton("✓ Set as active", Color.FromArgb(0, 140, 60));
        _btnAddShippingSite.Click       += (s, e) => OpenShippingSiteForm(null);
        _btnEditShippingSite.Click      += (s, e) => EditSelectedShippingSite();
        _btnSetActiveShippingSite.Click += (s, e) => SetActiveShippingSite();
        LayoutButtons(pnlBtns, new Control[] { _btnAddShippingSite, _btnEditShippingSite, _btnSetActiveShippingSite });

        _dgvShippingSites = MakeGrid(true);
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteCode",   HeaderText = "Code",       Width = 120 });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",   HeaderText = "Site name",  Width = 200 });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "MID9",       HeaderText = "MID9",       Width = 120 });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "STC",        HeaderText = "STC",        Width = 60  });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "ZIP9Dash",   HeaderText = "ZIP-9",      Width = 110 });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "SHIP_ADD_1", HeaderText = "Address 1",  Width = 180 });
        _dgvShippingSites.Columns.Add(new DataGridViewTextBoxColumn { Name = "Active",     HeaderText = "Active",     Width = 90  });
        _dgvShippingSites.Dock = DockStyle.Fill;
        _dgvShippingSites.DoubleClick += (s, e) => EditSelectedShippingSite();

        // Dock order: Fill → Top (buttons) → Top (info, topmost)
        p.Controls.Add(_dgvShippingSites);
        p.Controls.Add(pnlBtns);
        p.Controls.Add(pnlInfo);
    }

    // ══════════════════════════════════════════════════════════
    //  HISTORY TAB
    // ══════════════════════════════════════════════════════════
    private void BuildHistoryTab()
    {
        var p = _tabHistory;
        p.Padding = new Padding(12);

        var pnlBtns = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        _btnClearHistory = MakeButton("Clear all history", Color.FromArgb(200, 50, 50));
        _btnClearHistory.Click += (s, e) => ClearHistory();
        LayoutButtons(pnlBtns, new Control[] { _btnClearHistory });

        // ── Filter bar ────────────────────────────────────────
        var pnlFilterBar = new Panel
        {
            Dock    = DockStyle.Top,
            Height  = 40,
            Padding = new Padding(0, 5, 4, 4)
        };
        var lblHistSearch = new Label
        {
            Text      = "Search:",
            AutoSize  = true,
            Location  = new Point(0, 9),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        _txtHistorySearch = new TextBox
        {
            Location        = new Point(56, 6),
            Width           = 220,
            Font            = new Font("Segoe UI", 9.5f),
            BorderStyle     = BorderStyle.FixedSingle,
            PlaceholderText = "Filter by site, LIBCODE or template…"
        };
        _txtHistorySearch.TextChanged += (s2, e2) => ApplyHistoryFilter();

        var lblType = new Label
        {
            Text      = "Type:",
            AutoSize  = true,
            Location  = new Point(286, 9),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        _cboHistoryType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location      = new Point(324, 6),
            Width         = 140,
            Font          = new Font("Segoe UI", 9.5f)
        };
        _cboHistoryType.Items.AddRange(new object[] { "All types", "WORD", "CSV", "ATTEMPT", "CANCELLED", "SHIP" });
        _cboHistoryType.SelectedIndex = 0;
        _cboHistoryType.SelectedIndexChanged += (s2, e2) => ApplyHistoryFilter();

        var btnHistClear = new Button
        {
            Text      = "Clear",
            Location  = new Point(474, 5),
            Size      = new Size(58, 24),
            BackColor = Color.FromArgb(225, 225, 225),
            ForeColor = Color.FromArgb(50, 50, 50),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f)
        };
        btnHistClear.FlatAppearance.BorderSize = 0;
        btnHistClear.Click += (s2, e2) => { _txtHistorySearch.Text = ""; _cboHistoryType.SelectedIndex = 0; };
        pnlFilterBar.Controls.AddRange(new Control[] { lblHistSearch, _txtHistorySearch, lblType, _cboHistoryType, btnHistClear });

        _dgvHistory = MakeGrid(true);
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrintedAt",    HeaderText = "Date & time",   Width = 160 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",     HeaderText = "Site",          Width = 180 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE",      HeaderText = "LIBCODE",       Width = 90  });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunType",      HeaderText = "Type",          Width = 90  });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "LabelSize",    HeaderText = "Size",          Width = 50  });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TemplateName", HeaderText = "Template",      Width = 150 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialRange",  HeaderText = "Serial range",  Width = 220 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "LabelCount",   HeaderText = "Count",         Width = 60  });
        _dgvHistory.Dock = DockStyle.Fill;

        // Fill first, then Top panels (last Top added = topmost)
        p.Controls.Add(_dgvHistory);
        p.Controls.Add(pnlBtns);       // Top — below filter bar
        p.Controls.Add(pnlFilterBar);  // Top — last (topmost)
    }

    // ══════════════════════════════════════════════════════════
    //  DATA LOADING
    // ══════════════════════════════════════════════════════════
    private void LoadData()
    {
        try
        {
            _sites = Program.DB.GetAllSites();
            _cboSite.Items.Clear();
            _sites.ForEach(s => _cboSite.Items.Add(s));
            _cboSite.DisplayMember = "DisplayName";
            _txtSiteSearch.Text = "";
            RefreshSiteGrid();
            _shippingSites = Program.DB.GetAllShippingSites();
            RefreshShippingSiteGrid();
            _reportSites = _sites;
            RefreshReportSiteFilter();
            UpdateStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshSiteGrid()
    {
        _dgvSites.Rows.Clear();
        foreach (var s in _sites)
        {
            _dgvSites.Rows.Add(
                s.LIBCODE ?? "",
                s.SiteName ?? "",
                s.MID9 ?? "",
                s.ZIP9Dash ?? "",
                s.STC ?? "",
                s.CurrentSequence.ToString("D9"),
                s.LastPrintedDisplay ?? "",
                s.TotalLabels.ToString());
        }
    }

    private void RefreshShippingSiteGrid()
    {
        _dgvShippingSites.Rows.Clear();
        foreach (var ss in _shippingSites)
        {
            var idx = _dgvShippingSites.Rows.Add(
                ss.SiteCode,
                ss.SiteName,
                ss.MID9,
                ss.STC ?? "",
                ss.ZIP9Dash,
                ss.SHIP_ADD_1 ?? "",
                ss.IsActive ? "✓  ACTIVE" : "");
            if (ss.IsActive)
            {
                _dgvShippingSites.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(0, 130, 50);
                _dgvShippingSites.Rows[idx].DefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
        }
    }

    private void LoadHistory()
    {
        try
        {
            _dgvHistory.Rows.Clear();
            foreach (var h in Program.DB.GetHistory())
            {
                var row = _dgvHistory.Rows[_dgvHistory.Rows.Add()];
                row.Cells["PrintedAt"].Value    = h.PrintedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row.Cells["SiteName"].Value     = h.SiteName;
                row.Cells["LIBCODE"].Value      = h.LIBCODE;
                row.Cells["RunType"].Value      = h.RunType;
                row.Cells["LabelSize"].Value    = h.LabelSize ?? "";
                row.Cells["TemplateName"].Value = h.TemplateName ?? "";
                row.Cells["SerialRange"].Value  = h.SerialRange;
                row.Cells["LabelCount"].Value   = h.LabelCount;
                row.DefaultCellStyle.ForeColor =
                    h.RunType == "CSV"       ? Color.DarkBlue :
                    h.RunType == "WORD"      ? Color.DarkGreen :
                    h.RunType == "SHIP"      ? Color.FromArgb(0, 110, 80) :
                    h.RunType == "PRINT"     ? Color.FromArgb(150, 0, 150) :
                    h.RunType == "ATTEMPT"   ? Color.FromArgb(180, 130, 0) :
                    h.RunType == "CANCELLED" ? Color.FromArgb(180, 50, 50) :
                    Color.Black;
            }
            ApplyHistoryFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateStats()
    {
        var (sites, total, today) = Program.DB.GetStats();
        _statSites.Text = "Return sites: " + sites;
        var activeSite = _shippingSites.FirstOrDefault(ss => ss.IsActive);
        _statShipper.Text = activeSite != null ? "Shipper: " + activeSite.SiteCode : "Shipper: —";
        _statTotal.Text = "Total labels: " + total.ToString("N0");
        _statToday.Text = "Today: " + today.ToString("N0");
    }

    // ══════════════════════════════════════════════════════════
    //  RETURN LABELS — EVENTS
    // ══════════════════════════════════════════════════════════
    private Site? GetSelectedSite() => _cboSite.SelectedItem as Site;
    private void OnSiteChanged(object? s, EventArgs e) => RefreshGeneratePreview();
    private void OnQtyChanged(object? s, EventArgs e)  => RefreshGeneratePreview();

    private void RefreshGeneratePreview()
    {
        var site = GetSelectedSite();
        if (site == null) { _pnlSiteInfo.Visible = false; _dgvComputed.Rows.Clear(); return; }

        _pnlSiteInfo.Visible = true;
        var qty  = (int)_numQty.Value;
        var from = site.CurrentSequence;
        var to   = from + qty - 1;

        _lblLastSerial.Text = site.LastPrintedSerial.HasValue ? site.LastPrintedSerial.Value.ToString("D9") : "None yet";
        _lblNextSerial.Text = from.ToString("D9");
        _lblNextRange.Text  = qty > 1 ? "  to  " + to.ToString("D9") + "  (" + qty.ToString("N0") + " labels)" : "";

        var row = BarcodeService.BuildLabelRow(site, from);
        _dgvComputed.Rows.Clear();
        foreach (var (label, value) in new (string, string)[]
        {
            ("ZIP9 (dash removed)", row.ZIP9),
            ("SerialInterleaved",   row.SerialInterleaved.ToString()),
            ("TrackingCore",        row.TrackingCore),
            ("MOD10",               row.MOD10.ToString()),
            ("GS1_128_Raw",         row.GS1_128_Raw),
            ("HR_Text",             row.HR_Text),
        })
        _dgvComputed.Rows.Add(label, value);
    }

    // ── Export CSV only ───────────────────────────────────────





private void OnExportCSV(object? s, EventArgs e)
{
    var site = GetSelectedSite();
    if (site == null) { MessageBox.Show("Please select a site.", "No site selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
    var qty = (int)_numQty.Value;
    var from = site.CurrentSequence;
    var to = from + qty - 1;

    using var dlg = new SaveFileDialog
    {
        Title = "Save CSV for mail merge",
        Filter = "CSV files (*.csv)|*.csv",
        FileName = "TRL_labels_" + site.LIBCODE + "_" + from.ToString("D9") + "_qty" + qty + ".csv",
        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    };
    if (dlg.ShowDialog() != DialogResult.OK) return;

    // Build batch with configurable calibration rows
    var batch = new List<LabelRow>();
    
    int calibrationCount = AppSettings.CalibrationCount;
    
    // Add calibration rows (0 to configured number)
    for (int i = 1; i <= calibrationCount; i++)
    {
        batch.Add(BarcodeService.BuildCalibrationLabelRow(site, from, i));
    }
    
    // Add real labels with actual serial numbers
    for (int i = 0; i < qty; i++)
    {
        batch.Add(BarcodeService.BuildLabelRow(site, from + i));
    }
    
    try 
    { 
        File.WriteAllText(dlg.FileName, BarcodeService.ToCsv(batch)); 
    }
    catch (Exception ex) 
    { 
        MessageBox.Show("Could not write CSV:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
        return; 
    }


    var nl = Environment.NewLine;
    var ans = MessageBox.Show(
        "CSV saved successfully." + nl + nl +
        "Serial range:  " + from.ToString("D9") + "  →  " + to.ToString("D9") + nl +
        "File:  " + dlg.FileName + nl + nl +
        "Advance the sequence now?" + nl +
        "Click No if printing failed or was incomplete — you can reprint the same labels.",
        "Confirm labels printed",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);

    if (ans != DialogResult.Yes) return;

    // Confirmed — advance sequence by qty (real labels only, NOT including calibration)
    var actualFrom = Program.DB.AdvanceSequence(site.Id, qty, out var newNext);
    site.CurrentSequence = newNext; 
    site.LastPrintedSerial = actualFrom + qty - 1; 
    site.LastPrintedDate = DateTime.Now;
    
    Program.DB.InsertHistory(new PrintHistoryEntry 
    { 
        SiteId = site.Id, 
        LIBCODE = site.LIBCODE, 
        SiteName = site.SiteName, 
        SerialFrom = actualFrom, 
        SerialTo = actualFrom + qty - 1, 
        LabelCount = qty,  // Only real labels count
        RunType = "CSV", 
        LabelSize = "", 
        PrintedBy = Environment.UserName 
    });
    
    UpdateStats();
    if (_tabs.SelectedTab == _tabHistory) LoadHistory();
    RefreshGeneratePreview();
}












    private bool ValidateWordSettings()
    {
        if (string.IsNullOrEmpty(AppSettings.TempFolder))
        { MessageBox.Show("Please configure a Temp CSV folder in File > Settings.", "Settings required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        if (AppSettings.WordTemplates.Count == 0)
        { MessageBox.Show("Please add at least one Word template in File > Settings.", "Settings required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        return true;
    }

    // ══════════════════════════════════════════════════════════
    //  SHIPPING LABELS — EVENTS
    // ══════════════════════════════════════════════════════════
    private void RefreshShippingGrid()
    {
        _dgvShipping.CellValueChanged -= OnShippingCellChanged;
        _dgvShipping.Rows.Clear();

        foreach (var s in _sites)
        {
            var idx = _dgvShipping.Rows.Add(
                false,
                s.LIBCODE    ?? "",
                s.SiteName   ?? "",
                s.SHIP_ADD_1 ?? "",
                s.SHIP_ADD_2 ?? "",
                s.SHIP_ADD_3 ?? "",
                s.SHIP_ADD_4 ?? "",
                s.LastShippingBoxQty > 0 ? s.LastShippingBoxQty.ToString() : "",
                s.LastShippingBoxQty > 0 ? s.LastShippingBoxQty.ToString() : "");
            _dgvShipping.Rows[idx].Tag = s;
        }

        _dgvShipping.CellValueChanged += OnShippingCellChanged;
        UpdateShippingSummary();
    }

    private void OnShippingCellValidating(object? s, DataGridViewCellValidatingEventArgs e)
    {
        if (e.ColumnIndex != ShipColBoxes) return;

        var raw = e.FormattedValue?.ToString()?.Trim() ?? "";
        if (raw == "" || raw == "0")
        {
            // Allow blank / zero — means skip this site
            return;
        }
        if (!int.TryParse(raw, out int v) || v < 0)
        {
            e.Cancel = true;
            MessageBox.Show("Boxes must be a whole number (0 or blank to skip).", "Invalid value",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnShippingCellChanged(object? s, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == ShipColBoxes)
            UpdateShippingSummary();
    }

    private void UpdateShippingSummary()
    {
        int sites  = 0;
        int labels = 0;
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            if (!row.Visible) continue;
            var isChecked = row.Cells[ShipColSelected].Value as bool? ?? false;
            if (!isChecked) continue;
            var raw = row.Cells[ShipColBoxes].Value?.ToString()?.Trim() ?? "";
            if (int.TryParse(raw, out int boxes) && boxes > 0)
            {
                sites++;
                labels += boxes;
            }
        }
        _lblShipSummary.Text = sites > 0
            ? sites.ToString() + " site" + (sites > 1 ? "s" : "") + " selected  —  " + labels.ToString("N0") + " total label" + (labels > 1 ? "s" : "")
            : "No sites selected (enter a Boxes value greater than 0)";
    }

    private List<(Site site, int boxes)> GetShippingEntries(bool useSelectionFallback = true)
    {
        var entries = new List<(Site, int)>();

        // First pass — collect checked rows with Boxes > 0
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            if (row.Tag is not Site site) continue;
            var isChecked = row.Cells[ShipColSelected].Value as bool? ?? false;
            if (!isChecked) continue;
            var raw = row.Cells[ShipColBoxes].Value?.ToString()?.Trim() ?? "";
            if (int.TryParse(raw, out int boxes) && boxes > 0)
                entries.Add((site, boxes));
        }

        // If nothing checked, fall back to the currently highlighted row
        if (entries.Count == 0 && useSelectionFallback)
        {
            var currentRow = _dgvShipping.CurrentRow;
            if (currentRow != null && currentRow.Tag is Site selectedSite)
            {
                var raw = currentRow.Cells[ShipColBoxes].Value?.ToString()?.Trim() ?? "";
                if (int.TryParse(raw, out int boxes) && boxes > 0)
                {
                    entries.Add((selectedSite, boxes));
                }
                else
                {
                    MessageBox.Show(
                        "Please enter a Boxes value for " + selectedSite.SiteName +
                        " or check the sites you want to include.",
                        "No quantity entered",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        return entries;
    }

    private void OnShipSearchChanged(object? s, EventArgs e)
    {
        var term = _txtShipSearch.Text.Trim().ToLower();
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            try
            {
                if (string.IsNullOrEmpty(term))
                {
                    row.Visible = true;
                }
                else
                {
                    var libcode  = row.Cells[ShipColLibcode].Value?.ToString()?.ToLower() ?? "";
                    var siteName = row.Cells[ShipColName].Value?.ToString()?.ToLower() ?? "";
                    row.Visible  = libcode.Contains(term) || siteName.Contains(term);
                }
            }
            catch { }
        }
        UpdateShippingSummary();
    }

    private void OnShipSelectAll(object? s, EventArgs e)
    {
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            var raw = row.Cells[ShipColBoxes].Value?.ToString()?.Trim() ?? "";
            if (int.TryParse(raw, out int boxes) && boxes > 0)
                row.Cells[ShipColSelected].Value = true;
        }
        UpdateShippingSummary();
    }

    private void OnShipClearAll(object? s, EventArgs e)
    {
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            row.Cells[ShipColSelected].Value = false;
            row.Cells[ShipColBoxes].Value    = "";
        }
        UpdateShippingSummary();
    }

    
    




private void OnShipExportCSV(object? s, EventArgs e)
{
    var entries = GetShippingEntries();
    if (entries.Count == 0)
    {
        MessageBox.Show("No sites selected for printing.", "Nothing to print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var activeSite = Program.DB.GetActiveShippingSite();
    if (activeSite == null)
    {
        MessageBox.Show("No active shipping site configured.", "Missing config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    int totalLabels = entries.Sum(x => x.boxes);
    using var dlg = new SaveFileDialog
    {
        Title = "Save shipping labels CSV",
        Filter = "CSV files (*.csv)|*.csv",
        FileName = "TRL_shipping_" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    };
    if (dlg.ShowDialog() != DialogResult.OK) return;

    int firstSerial = Program.DB.AdvanceShippingSequence(totalLabels, out _);

    // Use the original method WITHOUT calibration rows
    File.WriteAllText(dlg.FileName, BarcodeService.ToShippingCsv(entries, activeSite, firstSerial, calibrationCount: 0));

    SaveShippingQuantities(entries);
    LogShippingHistory(entries, "SHIP", activeSite, firstSerial, totalLabels);
    UpdateStats();

    MessageBox.Show(
        $"CSV exported with {entries.Count} site(s), {totalLabels} total labels\n\nFile: {dlg.FileName}",
        "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
}





private void OnShipOpenWord(object? s, EventArgs e)
{
    if (_shipMergeRunning)
    {
        MessageBox.Show("A shipping label document is already being created. Please wait.", "Already running", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    var entries = GetShippingEntries();
    if (entries.Count == 0)
    {
        MessageBox.Show("No sites selected for printing.", "Nothing to print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    if (string.IsNullOrEmpty(AppSettings.TempFolder) || string.IsNullOrEmpty(AppSettings.ShippingTemplatePath))
    {
        MessageBox.Show("Please configure Temp folder and Shipping template in Settings.", "Settings required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var activeSite = Program.DB.GetActiveShippingSite();
    if (activeSite == null)
    {
        MessageBox.Show("No active shipping site configured.", "Missing config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    _shipMergeRunning = true;
    _btnShipOpenWord.Enabled = false;
    _btnShipExportCSV.Enabled = false;

    using var progress = new Form { Text = "Shipping Label Merge Session", Size = new Size(720, 420), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };

    var txtLog = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9f) };
    var btnClose = new Button { Text = "Close", Dock = DockStyle.Bottom, Height = 36, Enabled = false };

    progress.Controls.Add(txtLog);
    progress.Controls.Add(btnClose);

    void Log(string message)
    {
        txtLog.Invoke(new Action(() =>
        {
            txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }));
    }

    btnClose.Click += (s2, e2) => progress.Close();

    progress.Shown += (s2, e2) =>
    {
        try
        {
            Log("Starting shipping label merge session...");

            int realLabels = entries.Sum(x => x.boxes);
            int firstSerial = Program.DB.AdvanceShippingSequence(realLabels, out _);

            Log($"Real shipping labels: {realLabels}");
            Log($"First real serial: {FormatSerial(firstSerial)}");

            var csvPath = Path.Combine(AppSettings.TempFolder, $"TRL_shipping_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            // Use original method WITHOUT calibration rows
            string csvContent = BarcodeService.ToShippingCsv(entries, activeSite, firstSerial, calibrationCount: 0);
            File.WriteAllText(csvPath, csvContent);
            
            Log("CSV ready for Word merge");

            var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TRL", "Output");
            Directory.CreateDirectory(outputFolder);
            var outputDocxPath = Path.Combine(outputFolder, $"TRL_shipping_{DateTime.Now:yyyyMMdd_HHmmss}_Final.docx");

            Log("Calling Word merge...");
            WordMergeService.SaveMergedDocument(
                csvPath, 
                AppSettings.ShippingTemplatePath, 
                outputDocxPath,
                siteName: "",
                startSerial: 0,
                endSerial: 0
            );

            Log("Word document created successfully.");

            SaveShippingQuantities(entries);
            LogShippingHistory(entries, "SHIP", activeSite, firstSerial, realLabels);
            UpdateStats();

            Process.Start(new ProcessStartInfo { FileName = outputDocxPath, UseShellExecute = true });
            Log("Opened final document.");
            Log("Complete.");

            btnClose.Enabled = true;
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnClose.Enabled = true;
        }
        finally
        {
            _shipMergeRunning = false;
            _btnShipOpenWord.Invoke(new Action(() => _btnShipOpenWord.Enabled = true));
            _btnShipExportCSV.Invoke(new Action(() => _btnShipExportCSV.Enabled = true));
        }
    };

    progress.ShowDialog(this);
}









    private void SaveShippingQuantities(List<(Site site, int boxes)> entries)
    {
        // Save quantities for the sites that are in this run, and zero out
        // sites that previously had a quantity but were not in this run.
        var inRun = new HashSet<int>(entries.Select(x => x.site.Id));

        foreach (var (site, boxes) in entries)
        {
            Program.DB.UpdateShippingBoxQty(site.Id, boxes);
            site.LastShippingBoxQty = boxes;
        }
        // Update the "Last used" column in the grid to reflect saved values
        foreach (DataGridViewRow row in _dgvShipping.Rows)
        {
            if (row.Tag is not Site site) continue;
            if (!inRun.Contains(site.Id)) continue;
            var qty = entries.First(x => x.site.Id == site.Id).boxes;
            row.Cells[ShipColLastUsed].Value = qty.ToString();
        }
    }

    private void LogShippingHistory(List<(Site site, int boxes)> entries, string runType,
        TRL.Models.ShippingSite shippingSite, int firstSerial, int totalBoxes)
    {
        int serialOffset = 0;
        foreach (var (site, boxes) in entries)
        {
            Program.DB.InsertHistory(new PrintHistoryEntry
            {
                SiteId       = site.Id,
                LIBCODE      = site.LIBCODE,
                SiteName     = site.SiteName,
                SerialFrom   = firstSerial + serialOffset,
                SerialTo     = firstSerial + serialOffset + boxes - 1,
                LabelCount   = boxes,
                RunType      = runType,
                LabelSize    = "",
                PrintedBy    = Environment.UserName,
                TemplateName = shippingSite.SiteCode
            });
            serialOffset += boxes;
        }
        if (_tabs.SelectedTab == _tabHistory) LoadHistory();
    }

    // ══════════════════════════════════════════════════════════
    //  SITES TAB ACTIONS
    // ══════════════════════════════════════════════════════════
    private void OnSiteSearchChanged(object? s, EventArgs e)
    {
        var term = _txtSiteSearch.Text.Trim().ToLower();
        for (int i = 0; i < _dgvSites.Rows.Count && i < _sites.Count; i++)
        {
            try
            {
                if (string.IsNullOrEmpty(term))
                {
                    _dgvSites.Rows[i].Visible = true;
                }
                else
                {
                    var site  = _sites[i];
                    var match = (site.LIBCODE  ?? "").ToLower().Contains(term) ||
                                (site.SiteName ?? "").ToLower().Contains(term) ||
                                (site.MID9     ?? "").ToLower().Contains(term);
                    _dgvSites.Rows[i].Visible = match;
                }
            }
            catch { }
        }
    }
    private void OpenSiteForm(Site? site) { using var f = new SiteForm(site); if (f.ShowDialog(this) == DialogResult.OK) LoadData(); }
    private void EditSelectedSite() { var s = GetSelectedSiteFromGrid(); if (s != null) OpenSiteForm(s); }
    private Site? GetSelectedSiteFromGrid() => (_dgvSites.CurrentRow == null || _dgvSites.CurrentRow.Index < 0 || _dgvSites.CurrentRow.Index >= _sites.Count) ? null : _sites[_dgvSites.CurrentRow.Index];

    private void DeleteSelectedSite()
    {
        var s = GetSelectedSiteFromGrid(); if (s == null) return;
        if (MessageBox.Show("Delete site '" + s.SiteName + "'?\nHistory is kept but the sequence will be lost.", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Program.DB.DeleteSite(s.Id); LoadData();
    }

    // ── Shipping Sites actions ────────────────────────────────
    private void OpenShippingSiteForm(TRL.Models.ShippingSite? site)
    {
        using var f = new ShippingSiteForm(site);
        if (f.ShowDialog(this) == DialogResult.OK) LoadData();
    }

    private void EditSelectedShippingSite()
    {
        var ss = GetSelectedShippingSiteFromGrid();
        if (ss != null) OpenShippingSiteForm(ss);
    }

    private TRL.Models.ShippingSite? GetSelectedShippingSiteFromGrid() =>
        (_dgvShippingSites.CurrentRow == null ||
         _dgvShippingSites.CurrentRow.Index < 0 ||
         _dgvShippingSites.CurrentRow.Index >= _shippingSites.Count)
        ? null
        : _shippingSites[_dgvShippingSites.CurrentRow.Index];

    private void SetActiveShippingSite()
    {
        var ss = GetSelectedShippingSiteFromGrid();
        if (ss == null)
        {
            MessageBox.Show("Select a shipping site first.", "No selection",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Program.DB.SetActiveShippingSite(ss.Id);
        LoadData();
    }

    private void ApplyHistoryFilter()
    {
        var term       = _txtHistorySearch?.Text.Trim().ToLower() ?? "";
        var typeFilter = _cboHistoryType?.SelectedItem?.ToString() ?? "All types";

        foreach (DataGridViewRow row in _dgvHistory.Rows)
        {
            try
            {
                bool matchType = typeFilter == "All types" ||
                                 (row.Cells["RunType"].Value?.ToString() ?? "") == typeFilter;
                if (!matchType) { row.Visible = false; continue; }

                if (string.IsNullOrEmpty(term)) { row.Visible = true; continue; }

                var site    = row.Cells["SiteName"].Value?.ToString()?.ToLower() ?? "";
                var libcode = row.Cells["LIBCODE"].Value?.ToString()?.ToLower() ?? "";
                var tpl     = row.Cells["TemplateName"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = site.Contains(term) || libcode.Contains(term) || tpl.Contains(term);
            }
            catch { }
        }
    }

    private void ClearHistory()
    {
        if (MessageBox.Show("Clear all print history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Program.DB.ClearHistory(); LoadHistory(); UpdateStats();
    }

    private void ShowConnectionSettings() { using var d = new ConnectionForm(Program.ConnectionString); d.ShowDialog(this); }
    private void ShowAppSettings() { using var d = new SettingsForm(_sites); d.ShowDialog(this); LoadData(); }

    // ══════════════════════════════════════════════════════════
    //  CSV IMPORT / EXPORT
    // ══════════════════════════════════════════════════════════
    private void ImportSitesFromCsv()
    {
        using var dlg = new OpenFileDialog { Title = "Import / update sites from CSV", Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var existingDict = _sites.ToDictionary(s => s.LIBCODE.ToUpper(), s => s);
        var (toInsert, toUpdate, result) = CsvImportService.Parse(dlg.FileName, existingDict, CsvImportService.ImportMode.Upsert);

        if (toInsert.Count == 0 && toUpdate.Count == 0 && result.ErrorMessages.Count > 0)
        {
            MessageBox.Show("No sites could be imported:" + Environment.NewLine + string.Join(Environment.NewLine, result.ErrorMessages), "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (toInsert.Count == 0 && toUpdate.Count == 0)
        {
            MessageBox.Show("Nothing to import or update. No changes found.", "Nothing to do", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var preview = new ImportPreviewForm(toInsert, toUpdate, result);
        if (preview.ShowDialog(this) != DialogResult.OK) return;

        int inserted = 0, updated = 0;
        var errors = new List<string>();

        foreach (var site in toInsert)
        {
            try { Program.DB.InsertSite(site); inserted++; }
            catch (Exception ex) { errors.Add("INSERT " + site.LIBCODE + ": " + ex.Message); }
        }
        foreach (var (_, updatedSite) in toUpdate)
        {
            try { Program.DB.UpdateSite(updatedSite); updated++; }
            catch (Exception ex) { errors.Add("UPDATE " + updatedSite.LIBCODE + ": " + ex.Message); }
        }

        LoadData();

        var nl = Environment.NewLine;
        var summary = "Import complete." + nl + nl +
                      "  Inserted:  " + inserted + nl +
                      "  Updated:   " + updated + nl +
                      "  Errors:    " + (result.Errors + errors.Count);
        if (errors.Count > 0) summary += nl + nl + "Errors:" + nl + "  " + string.Join(nl + "  ", errors);
        MessageBox.Show(summary, "Import results", MessageBoxButtons.OK, errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void DownloadImportTemplate()
    {
        using var dlg = new SaveFileDialog { Title = "Save import template", Filter = "CSV files (*.csv)|*.csv", FileName = "TRL_import_template.csv", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        File.WriteAllText(dlg.FileName, CsvImportService.GenerateTemplate());
        MessageBox.Show("Template saved to:" + Environment.NewLine + dlg.FileName, "Template saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportSitesToCsv()
    {
        if (_sites.Count == 0) { MessageBox.Show("No sites to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var dlg = new SaveFileDialog { Title = "Export all sites", Filter = "CSV files (*.csv)|*.csv", FileName = "TRL_sites_" + DateTime.Now.ToString("yyyyMMdd") + ".csv", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var header = "\"LIBCODE\",\"SiteName\",\"SHIP_ADD_1\",\"SHIP_ADD_2\",\"SHIP_ADD_3\",\"SHIP_ADD_4\",\"ZIP-9\",\"MID9\",\"STC\",\"ChannelAI\",\"CurrentSequence\",\"LastPrintedSerial\"";
        var lines = new List<string> { header };
        foreach (var s in _sites) lines.Add(Q(s.LIBCODE) + "," + Q(s.SiteName) + "," + Q(s.SHIP_ADD_1) + "," + Q(s.SHIP_ADD_2) + "," + Q(s.SHIP_ADD_3) + "," + Q(s.SHIP_ADD_4) + "," + Q(s.ZIP9Dash) + "," + Q(s.MID9) + "," + Q(s.STC) + "," + Q(s.ChannelAI) + "," + Q(s.CurrentSequence.ToString()) + "," + Q(s.LastPrintedSerial?.ToString() ?? ""));
        File.WriteAllText(dlg.FileName, string.Join("\r\n", lines) + "\r\n");
        MessageBox.Show("Exported " + _sites.Count + " site(s) to:" + Environment.NewLine + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Q(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";

    // ══════════════════════════════════════════════════════════
    //  REPORTS TAB — BUILD
    // ══════════════════════════════════════════════════════════
    private void BuildReportsTab()
    {
        var p = _tabReports;
        p.Padding = new Padding(12);

        // ── Shared filter bar (Top) ───────────────────────────
        var pnlFilter = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 54,
            Padding   = new Padding(0, 8, 0, 4)
        };

        int x = 0;

        var lblFrom = new Label { Text = "From:", AutoSize = true, Location = new Point(x, 13), ForeColor = Color.FromArgb(60, 60, 60) };
        x += 42;
        _dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(x, 9), Width = 110 };
        _dtpFrom.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        x += 118;

        var lblTo = new Label { Text = "To:", AutoSize = true, Location = new Point(x, 13), ForeColor = Color.FromArgb(60, 60, 60) };
        x += 30;
        _dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(x, 9), Width = 110 };
        _dtpTo.Value = DateTime.Today;
        x += 118;

        var lblPeriod = new Label { Text = "Period:", AutoSize = true, Location = new Point(x, 13), ForeColor = Color.FromArgb(60, 60, 60) };
        x += 54;
        _cboPeriod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, 9), Width = 130, Font = new Font("Segoe UI", 9.5f) };
        _cboPeriod.Items.AddRange(new object[] { "This month", "Last month", "Last 3 months", "Last 6 months", "This year", "All time", "Custom" });
        _cboPeriod.SelectedIndex = 0;
        x += 138;

        var lblSite = new Label { Text = "Site:", AutoSize = true, Location = new Point(x, 13), ForeColor = Color.FromArgb(60, 60, 60) };
        x += 40;
        _cboReportSite = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, 9), Width = 220, Font = new Font("Segoe UI", 9.5f) };
        _cboReportSite.Items.Add("All sites");
        _cboReportSite.SelectedIndex = 0;
        x += 228;

        _btnRefresh = new Button
        {
            Text      = "Refresh",
            Location  = new Point(x, 6),
            Size      = new Size(90, 28),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.Click += (s, e) => ApplyReportFilters();

        pnlFilter.Controls.AddRange(new Control[] { lblFrom, _dtpFrom, lblTo, _dtpTo, lblPeriod, _cboPeriod, lblSite, _cboReportSite, _btnRefresh });
        pnlFilter.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(210, 215, 225) });

        // Wire filter events
        _cboPeriod.SelectedIndexChanged += (s, e) =>
        {
            if (_suppressPeriodSync) return;
            _suppressPeriodSync = true;
            UpdateDateRangeFromPeriod();
            _suppressPeriodSync = false;
            ApplyReportFilters();
        };
        _dtpFrom.ValueChanged += (s, e) =>
        {
            if (!_suppressPeriodSync) _cboPeriod.SelectedIndex = 6; // Custom
        };
        _dtpTo.ValueChanged += (s, e) =>
        {
            if (!_suppressPeriodSync) _cboPeriod.SelectedIndex = 6; // Custom
        };
        _cboReportSite.SelectedIndexChanged += (s, e) => ApplyReportFilters();

        // ── Sub-tabs (Fill) ──────────────────────────────────
        _tabsReports     = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 5) };
        _tabOverview     = new TabPage("  Overview  ");
        _tabReturnReport = new TabPage("  Return Labels  ");
        _tabShipReport   = new TabPage("  Shipping Labels  ");
        _tabsReports.TabPages.AddRange(new[] { _tabOverview, _tabReturnReport, _tabShipReport });

        // Dock order: Fill first, Top last
        p.Controls.Add(_tabsReports); // Fill — first
        p.Controls.Add(pnlFilter);    // Top  — last

        BuildOverviewSubTab();
        BuildReturnReportSubTab();
        BuildShippingReportSubTab();
    }

    private void BuildOverviewSubTab()
    {
        var p = _tabOverview;
        p.Padding = new Padding(8);

        // KPI cards row
        var pnlKpi = new Panel { Dock = DockStyle.Top, Height = 110 };
        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0, 6, 0, 0)
        };

        var blue  = Color.FromArgb(0,  90,  160);
        var green = Color.FromArgb(0, 130,   50);
        var grey  = Color.FromArgb(80,  80,   80);

        (_lblKpiReturnLabels, _)                = MakeKpiCard(flow, "Return Labels",    blue,  "labels printed");
        (_lblKpiBoxesShipped, _)                = MakeKpiCard(flow, "Boxes Shipped",    green, "boxes shipped");
        (_lblKpiReturnSites,  _lblKpiReturnSitesSub) = MakeKpiCard(flow, "Sites w/ Returns", blue,  "of — sites");
        (_lblKpiShipSites,    _lblKpiShipSitesSub)   = MakeKpiCard(flow, "Sites Shipped To", green, "of — sites");
        (_lblKpiAllReturn, _)                   = MakeKpiCard(flow, "All-Time Returns", grey,  "total since start");
        (_lblKpiAllShip, _)                     = MakeKpiCard(flow, "All-Time Shipped", grey,  "boxes since start");

        pnlKpi.Controls.Add(flow);

        // Breakdown grid
        _dgvOverview = MakeGrid(true);
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE",      HeaderText = "LIBCODE",       Width = 90  });
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",     HeaderText = "Site name",     Width = 200 });
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReturnLabels", HeaderText = "Return labels", Width = 120 });
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "BoxesShipped", HeaderText = "Boxes shipped", Width = 120 });
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastReturn",   HeaderText = "Last return run", Width = 160 });
        _dgvOverview.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastShip",     HeaderText = "Last shipment", Width = 160 });
        _dgvOverview.Columns["ReturnLabels"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _dgvOverview.Columns["BoxesShipped"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _dgvOverview.Dock = DockStyle.Fill;

        p.Controls.Add(_dgvOverview); // Fill — first
        p.Controls.Add(pnlKpi);       // Top  — last
    }

    private void BuildReturnReportSubTab()
    {
        var p = _tabReturnReport;
        p.Padding = new Padding(8);

        _lblReturnSummary = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(15, 55, 115),
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "No data loaded — click Refresh"
        };

        var pnlExport = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        var btnCsv   = MakeButton("Export CSV",   Color.FromArgb(100, 100, 100));
        var btnXlsx  = MakeButton("Export Excel", Color.FromArgb(0, 130, 50));
        var btnPdf   = MakeButton("Export PDF",   Color.FromArgb(160, 30, 30));
        btnCsv.Click  += OnExportReturnCsv;
        btnXlsx.Click += OnExportReturnExcel;
        btnPdf.Click  += OnExportReturnPdf;
        LayoutButtons(pnlExport, new Control[] { btnCsv, btnXlsx, btnPdf });

        _dgvReturnReport = MakeGrid(true);
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrintedAt",    HeaderText = "Date & time",  Width = 160 });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE",      HeaderText = "LIBCODE",      Width = 90  });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",     HeaderText = "Site",         Width = 180 });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialRange",  HeaderText = "Serial range", Width = 220 });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            Name = "FirstGS1",    
            HeaderText = "First GS1_128", 
            Width = 220 
        });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            Name = "LastGS1",     
            HeaderText = "Last GS1_128",  
            Width = 220 
        });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            Name = "FirstHR",     
            HeaderText = "First HR_Text", 
            Width = 160 
        });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn 
        { 
            Name = "LastHR",      
            HeaderText = "Last HR_Text",  
            Width = 160 
        });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "LabelCount",   HeaderText = "Labels",       Width = 70  });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "RunType",      HeaderText = "Type",         Width = 60  });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "TemplateName", HeaderText = "Template",     Width = 150 });
        _dgvReturnReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "LabelSize",    HeaderText = "Size",         Width = 50  });
        _dgvReturnReport.Columns["SerialRange"]!.DefaultCellStyle.Font      = new Font("Courier New", 9f);
        _dgvReturnReport.Columns["FirstGS1"]!.DefaultCellStyle.Font         = new Font("Courier New", 8.5f);
        _dgvReturnReport.Columns["LastGS1"]!.DefaultCellStyle.Font          = new Font("Courier New", 8.5f);
        _dgvReturnReport.Columns["FirstHR"]!.DefaultCellStyle.Font          = new Font("Courier New", 8.5f);
        _dgvReturnReport.Columns["LastHR"]!.DefaultCellStyle.Font           = new Font("Courier New", 8.5f);
        _dgvReturnReport.Columns["FirstGS1"]!.DefaultCellStyle.ForeColor    = Color.FromArgb(0, 100, 0);
        _dgvReturnReport.Columns["LastGS1"]!.DefaultCellStyle.ForeColor     = Color.FromArgb(0, 100, 0);
        _dgvReturnReport.Columns["FirstHR"]!.DefaultCellStyle.ForeColor     = Color.FromArgb(0, 100, 0);
        _dgvReturnReport.Columns["LastHR"]!.DefaultCellStyle.ForeColor      = Color.FromArgb(0, 100, 0);
        _dgvReturnReport.Columns["LabelCount"]!.DefaultCellStyle.Alignment  = DataGridViewContentAlignment.MiddleRight;
        _dgvReturnReport.Dock = DockStyle.Fill;

        p.Controls.Add(_dgvReturnReport);  // Fill — first
        p.Controls.Add(pnlExport);         // Bottom
        p.Controls.Add(_lblReturnSummary); // Top — last
    }

    private void BuildShippingReportSubTab()
    {
        var p = _tabShipReport;
        p.Padding = new Padding(8);

        _lblShipReportSummary = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(0, 100, 50),
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "No data loaded — click Refresh"
        };

        var pnlExport = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
        var btnCsv  = MakeButton("Export CSV",   Color.FromArgb(100, 100, 100));
        var btnXlsx = MakeButton("Export Excel", Color.FromArgb(0, 130, 50));
        var btnPdf  = MakeButton("Export PDF",   Color.FromArgb(160, 30, 30));
        btnCsv.Click  += OnExportShipCsv;
        btnXlsx.Click += OnExportShipExcel;
        btnPdf.Click  += OnExportShipPdf;
        LayoutButtons(pnlExport, new Control[] { btnCsv, btnXlsx, btnPdf });

        _dgvShippingReport = MakeGrid(true);
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrintedAt",    HeaderText = "Date & time",    Width = 160 });
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "LIBCODE",      HeaderText = "Ship-to site",   Width = 90  });
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiteName",     HeaderText = "Site name",      Width = 180 });
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "BoxCount",     HeaderText = "Boxes",          Width = 70  });
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialRange",  HeaderText = "Tracking range", Width = 220 });
        _dgvShippingReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "TemplateName", HeaderText = "Shipper",        Width = 150 });
        _dgvShippingReport.Columns["SerialRange"]!.DefaultCellStyle.Font     = new Font("Courier New", 9f);
        _dgvShippingReport.Columns["BoxCount"]!.DefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleRight;
        _dgvShippingReport.Dock = DockStyle.Fill;

        p.Controls.Add(_dgvShippingReport);    // Fill — first
        p.Controls.Add(pnlExport);             // Bottom
        p.Controls.Add(_lblShipReportSummary); // Top — last
    }

    private (Label number, Label sub) MakeKpiCard(FlowLayoutPanel flow, string title, Color titleColor, string subtext)
    {
        var card = new Panel
        {
            Width     = 148,
            Height    = 96,
            Margin    = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(245, 247, 252)
        };
        card.Paint += (s, e) => e.Graphics.DrawRectangle(
            new Pen(Color.FromArgb(210, 215, 225)),
            new Rectangle(0, 0, card.Width - 1, card.Height - 1));

        var lblTitle = new Label
        {
            Text      = title,
            Dock      = DockStyle.Top,
            Height    = 22,
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = titleColor,
            TextAlign = ContentAlignment.BottomLeft,
            Padding   = new Padding(6, 0, 0, 0)
        };
        var lblNumber = new Label
        {
            Text      = "—",
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = titleColor,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var lblSub = new Label
        {
            Text      = subtext,
            Dock      = DockStyle.Bottom,
            Height    = 18,
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(120, 120, 120),
            TextAlign = ContentAlignment.TopCenter
        };

        card.Controls.Add(lblNumber); // Fill — first
        card.Controls.Add(lblSub);    // Bottom
        card.Controls.Add(lblTitle);  // Top — last
        flow.Controls.Add(card);

        return (lblNumber, lblSub);
    }

    // ══════════════════════════════════════════════════════════
    //  REPORTS — FILTER & LOAD
    // ══════════════════════════════════════════════════════════
    private void RefreshReportSiteFilter()
    {
        if (_cboReportSite == null) return;
        var prev = _cboReportSite.SelectedIndex;
        _cboReportSite.Items.Clear();
        _cboReportSite.Items.Add("All sites");
        foreach (var s in _reportSites) _cboReportSite.Items.Add(s.DisplayName);
        _cboReportSite.SelectedIndex = (prev >= 0 && prev < _cboReportSite.Items.Count) ? prev : 0;
    }

    private void UpdateDateRangeFromPeriod()
    {
        var today = DateTime.Today;
        switch (_cboPeriod.SelectedItem?.ToString())
        {
            case "This month":
                _dtpFrom.Value = new DateTime(today.Year, today.Month, 1);
                _dtpTo.Value   = today;
                break;
            case "Last month":
                var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                _dtpFrom.Value = firstOfThisMonth.AddMonths(-1);
                _dtpTo.Value   = firstOfThisMonth.AddDays(-1);
                break;
            case "Last 3 months":
                _dtpFrom.Value = today.AddDays(-90);
                _dtpTo.Value   = today;
                break;
            case "Last 6 months":
                _dtpFrom.Value = today.AddDays(-180);
                _dtpTo.Value   = today;
                break;
            case "This year":
                _dtpFrom.Value = new DateTime(today.Year, 1, 1);
                _dtpTo.Value   = today;
                break;
            case "All time":
                _dtpFrom.Value = new DateTime(2000, 1, 1);
                _dtpTo.Value   = today;
                break;
        }
    }

    private void ApplyReportFilters()
    {
        if (_dtpFrom == null) return;
        var from    = _dtpFrom.Value.Date;
        var to      = _dtpTo.Value.Date.AddDays(1);
        int? siteId = (_cboReportSite.SelectedIndex <= 0 || _reportSites.Count == 0)
                      ? (int?)null
                      : _reportSites[_cboReportSite.SelectedIndex - 1].Id;
        try
        {
            _reportSummary    = Program.DB.GetReportSummary(from, to);
            _siteBreakdown    = Program.DB.GetSiteBreakdown(from, to);
            _returnReportData = Program.DB.GetReturnLabelReport(from, to, siteId);
            _shippingReportData = Program.DB.GetShippingLabelReport(from, to, siteId);
            LoadOverview();
            LoadReturnLabelsReport();
            LoadShippingReport();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading report: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadOverview()
    {
        if (_reportSummary == null) return;
        var totalSites = _sites.Count;

        _lblKpiReturnLabels.Text    = _reportSummary.ReturnLabelsInPeriod.ToString("N0");
        _lblKpiBoxesShipped.Text    = _reportSummary.BoxesShippedInPeriod.ToString("N0");
        _lblKpiReturnSites.Text     = _reportSummary.ReturnSitesInPeriod.ToString("N0");
        _lblKpiShipSites.Text       = _reportSummary.ShipSitesInPeriod.ToString("N0");
        _lblKpiAllReturn.Text       = _reportSummary.AllTimeReturnLabels.ToString("N0");
        _lblKpiAllShip.Text         = _reportSummary.AllTimeBoxesShipped.ToString("N0");
        _lblKpiReturnSitesSub.Text  = "of " + totalSites + " sites";
        _lblKpiShipSitesSub.Text    = "of " + totalSites + " sites";

        _dgvOverview.Rows.Clear();
        foreach (var b in _siteBreakdown)
        {
            var idx = _dgvOverview.Rows.Add(
                b.LIBCODE,
                b.SiteName,
                b.ReturnLabels.ToString("N0"),
                b.BoxesShipped.ToString("N0"),
                b.LastReturnRun?.ToString("yyyy-MM-dd HH:mm") ?? "",
                b.LastShipRun?.ToString("yyyy-MM-dd HH:mm") ?? "");

            var row = _dgvOverview.Rows[idx];
            if (b.ReturnLabels == 0 && b.BoxesShipped == 0)
            {
                row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
            }
            else
            {
                if (b.ReturnLabels > 0)
                    row.Cells["ReturnLabels"].Style.ForeColor = Color.FromArgb(0, 90, 160);
                if (b.BoxesShipped > 0)
                    row.Cells["BoxesShipped"].Style.ForeColor = Color.FromArgb(0, 130, 50);
            }
        }
    }

    private void LoadReturnLabelsReport()
    {
        // Compute tracking numbers for each run
        foreach (var entry in _returnReportData)
        {
            // Find the site for this LIBCODE
            var site = _sites.FirstOrDefault(
                s => s.LIBCODE == entry.LIBCODE);
            if (site == null) continue;

            // Compute first label in run
            var firstRow = BarcodeService.BuildLabelRow(
                site, entry.SerialFrom);
            entry.FirstGS1_128_Raw = firstRow.GS1_128_Raw;
            entry.FirstHR_Text     = firstRow.HR_Text;

            // Compute last label in run
            // (if only 1 label, first = last)
            if (entry.SerialTo > entry.SerialFrom)
            {
                var lastRow = BarcodeService.BuildLabelRow(
                    site, entry.SerialTo);
                entry.LastGS1_128_Raw = lastRow.GS1_128_Raw;
                entry.LastHR_Text     = lastRow.HR_Text;
            }
            else
            {
                entry.LastGS1_128_Raw = entry.FirstGS1_128_Raw;
                entry.LastHR_Text     = entry.FirstHR_Text;
            }
        }

        _dgvReturnReport.Rows.Clear();
        foreach (var r in _returnReportData)
        {
            var idx = _dgvReturnReport.Rows.Add(
                r.PrintedAt.ToString("yyyy-MM-dd HH:mm"),
                r.LIBCODE,
                r.SiteName,
                r.SerialRange,
                r.FirstGS1_128_Raw,
                r.LastGS1_128_Raw,
                r.FirstHR_Text,
                r.LastHR_Text,
                r.LabelCount.ToString("N0"),
                r.RunType,
                r.TemplateName ?? "",
                r.LabelSize ?? "");
            var row = _dgvReturnReport.Rows[idx];
            row.DefaultCellStyle.ForeColor =
                r.RunType == "WORD" ? Color.FromArgb(0, 120, 50) : Color.FromArgb(0, 70, 160);
        }

        int totalLabels = _returnReportData.Sum(r => r.LabelCount);
        string range = _returnReportData.Count > 0
            ? _returnReportData.Min(r => r.SerialFrom).ToString("D9") + " → " +
              _returnReportData.Max(r => r.SerialTo).ToString("D9")
            : "—";
        string trackingRange = "";
        if (_returnReportData.Count > 0)
        {
            var firstGS1 = _returnReportData.OrderByDescending(r => r.PrintedAt).First().FirstGS1_128_Raw;
            var lastGS1 = _returnReportData.OrderBy(r => r.PrintedAt).First().LastGS1_128_Raw;
            var truncate = (string gs1) => gs1.Length > 20 ? gs1.Substring(0, 20) + "..." : gs1;
            trackingRange = "  |  Tracking: " + truncate(firstGS1) + " → " + truncate(lastGS1);
        }
        _lblReturnSummary.Text =
            "Showing " + _returnReportData.Count + " run" + (_returnReportData.Count != 1 ? "s" : "") +
            "  |  " + totalLabels.ToString("N0") + " total labels" +
            "  |  Serial range: " + range + trackingRange;
    }

    private void LoadShippingReport()
    {
        _dgvShippingReport.Rows.Clear();
        foreach (var r in _shippingReportData)
        {
            _dgvShippingReport.Rows.Add(
                r.PrintedAt.ToString("yyyy-MM-dd HH:mm"),
                r.LIBCODE,
                r.SiteName,
                r.BoxCount.ToString("N0"),
                r.SerialRange,
                r.TemplateName ?? "");
        }

        int totalBoxes = _shippingReportData.Sum(r => r.BoxCount);
        string range = _shippingReportData.Count > 0
            ? _shippingReportData.Min(r => r.SerialFrom).ToString("D9") + " → " +
              _shippingReportData.Max(r => r.SerialTo).ToString("D9")
            : "—";
        _lblShipReportSummary.Text =
            "Showing " + _shippingReportData.Count + " run" + (_shippingReportData.Count != 1 ? "s" : "") +
            "  |  " + totalBoxes.ToString("N0") + " total boxes" +
            "  |  Tracking range: " + range;
    }

    // ══════════════════════════════════════════════════════════
    //  REPORTS — EXPORT HANDLERS
    // ══════════════════════════════════════════════════════════
    private static readonly string[] ReturnCsvHeaders =
        { "Date", "LIBCODE", "Site", "Serial From", "Serial To", "Count", "Type", "Template", "Size" };

    private static string[] ReturnRowMapper(TRL.Models.ReturnLabelReport r) =>
        new[] { r.PrintedAt.ToString("yyyy-MM-dd HH:mm"), r.LIBCODE, r.SiteName,
                r.SerialFrom.ToString("D9"), r.SerialTo.ToString("D9"),
                r.LabelCount.ToString(), r.RunType, r.TemplateName ?? "", r.LabelSize ?? "" };

    private static readonly string[] ShipCsvHeaders =
        { "Date", "LIBCODE", "Site", "Boxes", "Tracking From", "Tracking To", "Shipper" };

    private static string[] ShipRowMapper(TRL.Models.ShippingLabelReport r) =>
        new[] { r.PrintedAt.ToString("yyyy-MM-dd HH:mm"), r.LIBCODE, r.SiteName,
                r.BoxCount.ToString(), r.SerialFrom.ToString("D9"), r.SerialTo.ToString("D9"),
                r.TemplateName ?? "" };

    private void OnExportReturnCsv(object? s, EventArgs e)
    {
        if (_returnReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "TRL_return_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".csv", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        string siteFilter = _cboReportSite.SelectedIndex > 0 ? _reportSites[_cboReportSite.SelectedIndex - 1].SiteName : null;
        ReportExportService.ExportReturnLabelsDetail(
            _returnReportData,
            _sites,
            dlg.FileName,
            ReportExportService.ExportFormat.Csv,
            "TRL – Return Label Report",
            "Period: " + _dtpFrom.Value.ToString("yyyy-MM-dd") + 
            " to " + _dtpTo.Value.ToString("yyyy-MM-dd") +
            (siteFilter != null 
                ? "  |  Site: " + siteFilter 
                : "  |  All sites"));
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExportReturnExcel(object? s, EventArgs e)
    {
        if (_returnReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = "TRL_return_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        string siteFilter = _cboReportSite.SelectedIndex > 0 ? _reportSites[_cboReportSite.SelectedIndex - 1].SiteName : null;
        ReportExportService.ExportReturnLabelsDetail(
            _returnReportData,
            _sites,
            dlg.FileName,
            ReportExportService.ExportFormat.Excel,
            "TRL – Return Label Report",
            "Period: " + _dtpFrom.Value.ToString("yyyy-MM-dd") + 
            " to " + _dtpTo.Value.ToString("yyyy-MM-dd") +
            (siteFilter != null 
                ? "  |  Site: " + siteFilter 
                : "  |  All sites"));
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExportReturnPdf(object? s, EventArgs e)
    {
        if (_returnReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = "TRL_return_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".pdf", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        string siteFilter = _cboReportSite.SelectedIndex > 0 ? _reportSites[_cboReportSite.SelectedIndex - 1].SiteName : null;
        ReportExportService.ExportReturnLabelsDetail(
            _returnReportData,
            _sites,
            dlg.FileName,
            ReportExportService.ExportFormat.Pdf,
            "TRL – Return Label Report",
            "Period: " + _dtpFrom.Value.ToString("yyyy-MM-dd") + 
            " to " + _dtpTo.Value.ToString("yyyy-MM-dd") +
            (siteFilter != null 
                ? "  |  Site: " + siteFilter 
                : "  |  All sites"));
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExportShipCsv(object? s, EventArgs e)
    {
        if (_shippingReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "TRL_shipping_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".csv", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        TRL.Services.ReportExportService.ExportToCsv(_shippingReportData, ShipCsvHeaders, ShipRowMapper, dlg.FileName);
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExportShipExcel(object? s, EventArgs e)
    {
        if (_shippingReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = "TRL_shipping_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        TRL.Services.ReportExportService.ExportToExcel(_shippingReportData, ShipCsvHeaders, ShipRowMapper, dlg.FileName, "Shipping Labels");
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnExportShipPdf(object? s, EventArgs e)
    {
        if (_shippingReportData.Count == 0) { MessageBox.Show("No data to export.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var dlg = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = "TRL_shipping_labels_" + DateTime.Now.ToString("yyyyMMdd") + ".pdf", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var subtitle = _dtpFrom.Value.ToString("yyyy-MM-dd") + " to " + _dtpTo.Value.ToString("yyyy-MM-dd") +
                       (_cboReportSite.SelectedIndex > 0 ? "  |  " + _cboReportSite.SelectedItem : "  |  All sites");
        TRL.Services.ReportExportService.ExportToPdf(_shippingReportData, ShipCsvHeaders, ShipRowMapper, dlg.FileName, "TRL – Shipping Label Report", subtitle);
        MessageBox.Show("Exported: " + dlg.FileName, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
    private static DataGridView MakeGrid(bool readOnly)
    {
        var g = new DataGridView
        {
            ReadOnly = readOnly, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = SystemColors.Window,
            GridColor = Color.FromArgb(220, 220, 220),
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            RowTemplate = { Height = 26 }, Font = new Font("Segoe UI", 9f),
            ScrollBars = ScrollBars.Both, EnableHeadersVisualStyles = false
        };
        g.ColumnHeadersDefaultCellStyle.BackColor          = Color.FromArgb(230, 232, 236);
        g.ColumnHeadersDefaultCellStyle.ForeColor          = Color.FromArgb(40, 40, 40);
        g.ColumnHeadersDefaultCellStyle.Font               = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.Padding            = new Padding(4, 4, 4, 4);
        g.ColumnHeadersDefaultCellStyle.Alignment          = DataGridViewContentAlignment.MiddleLeft;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 232, 236);
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);
        g.DefaultCellStyle.Padding            = new Padding(4, 2, 4, 2);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 210, 240);
        g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 20, 20);
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        return g;
    }

    private static Button MakeButton(string text, Color backColor) => new Button
    {
        Text = text, BackColor = backColor, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Height = 32, AutoSize = true,
        Padding = new Padding(10, 0, 10, 0), Font = new Font("Segoe UI", 9.5f)
    };

    private static void LayoutButtons(Panel p, Control[] buttons)
    {
        int x = 0;
        foreach (var b in buttons) { b.Location = new Point(x, p.Padding.Top); b.Height = 32; p.Controls.Add(b); x += b.Width + 8; }
    }
}