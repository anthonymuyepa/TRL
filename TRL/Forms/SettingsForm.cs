using TRL.Services;
using TRL.Models;

namespace TRL.Forms;

public class SettingsForm : Form
{
    private TextBox  _txtTempFolder = null!;
    private TextBox  _txtAdminPin   = null!;
    private ComboBox _cboResetSite  = null!;
    private Button   _btnResetSite  = null!, _btnResetAll = null!;
    private List<Site> _sites       = new();

    // Return-label template list controls
    private ListBox _lstTemplates    = null!;
    private Label   _lblTemplatePath = null!;
    private Button  _btnTplEdit      = null!, _btnTplRemove = null!;
    private List<WordTemplate> _templates = new();

    // Shipping label template
    private TextBox _txtShippingTemplate = null!;

    // New: Calibration and Batch settings
    private NumericUpDown _numCalibrationCount = null!;
    private CheckBox _chkEnableBatchPrinting = null!;
    private NumericUpDown _numBatchSize = null!;

 
    private ComboBox _cmbSerialDigits = null!;

    public SettingsForm(List<Site> sites)
    {
        _sites = sites;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text            = "Settings";
        Size            = new Size(740, 700);  // Increased height for new controls
        MinimumSize     = new Size(700, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 9.5f);
        BackColor       = Color.FromArgb(245, 246, 248);

        // ── Tab control ───────────────────────────────────────
        var tabs = new TabControl
        {
            Dock    = DockStyle.Fill,
            Padding = new Point(16, 8),
            Font    = new Font("Segoe UI", 9.5f)
        };

        var tabGeneral = new TabPage("  General  ");
        var tabReset   = new TabPage("  Sequence Reset  ");
        tabs.TabPages.AddRange(new[] { tabGeneral, tabReset });

        BuildGeneralTab(tabGeneral);
        BuildResetTab(tabReset);

        Controls.Add(tabs);

        // ── Bottom button strip ───────────────────────────────
        var pnlBottom = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 64,
            BackColor = Color.FromArgb(237, 238, 242)
        };
        pnlBottom.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 212, 218) });

        var btnSave = new Button
        {
            Text      = "Save settings",
            Size      = new Size(130, 34),
            Location  = new Point(20, 16),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;

        var btnClose = new Button
        {
            Text         = "Close",
            Size         = new Size(90, 34),
            Location     = new Point(158, 16),
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 9.5f),
            DialogResult = DialogResult.Cancel
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);

        pnlBottom.Controls.AddRange(new Control[] { btnSave, btnClose });
        Controls.Add(pnlBottom);

        AcceptButton = btnSave;
        CancelButton = btnClose;
    }

    // ══════════════════════════════════════════════════════════
    //  GENERAL TAB
    // ══════════════════════════════════════════════════════════
    private void BuildGeneralTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(250, 251, 253);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 12, 8, 12) };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            Width         = 660,
            Padding       = new Padding(0)
        };

        // ── Temp folder ───────────────────────────────────────
        flow.Controls.Add(MakeSectionLabel("Mail merge configuration"));
        flow.Controls.Add(MakeFieldLabel2("Temp CSV folder"));
        flow.Controls.Add(MakeBrowseRow(out _txtTempFolder, @"e.g. C:\Temp\Labels", (s, e) => BrowseFolder(_txtTempFolder)));
        flow.Controls.Add(MakeHint2("CSV files are saved here before Word opens them"));
        flow.Controls.Add(MakeSpacer(10));

        // ── Template list ─────────────────────────────────────
        flow.Controls.Add(MakeFieldLabel2("Word mail merge templates (.docx)"));

        var pnlTemplates = new Panel { Width = 660, Height = 150, Margin = new Padding(0, 2, 0, 0) };

        _lstTemplates = new ListBox
        {
            Location      = new Point(0, 0),
            Size          = new Size(536, 126),
            Font          = new Font("Segoe UI", 9.5f),
            IntegralHeight = false,
            BorderStyle   = BorderStyle.FixedSingle
        };
        _lstTemplates.SelectedIndexChanged += OnTemplateSelectionChanged;
        _lstTemplates.DoubleClick          += (s, e) => OnTemplateEdit(s, e);

        var btnTplAdd = MakeSmallButton("Add",    new Point(542, 0));
        _btnTplEdit   = MakeSmallButton("Edit",   new Point(542, 34));
        _btnTplRemove = MakeSmallButton("Remove", new Point(542, 68));

        btnTplAdd.Click    += OnTemplateAdd;
        _btnTplEdit.Click  += OnTemplateEdit;
        _btnTplRemove.Click += OnTemplateRemove;

        _lblTemplatePath = new Label
        {
            Location  = new Point(0, 130),
            Size      = new Size(660, 18),
            Font      = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(100, 100, 100),
            AutoSize  = false
        };

        pnlTemplates.Controls.AddRange(new Control[] { _lstTemplates, btnTplAdd, _btnTplEdit, _btnTplRemove, _lblTemplatePath });
        flow.Controls.Add(pnlTemplates);
        flow.Controls.Add(MakeHint2("Each template has a display name and a .docx file path — used when exporting return labels"));
        flow.Controls.Add(MakeSpacer(10));

        // ── Shipping label template ───────────────────────────
        flow.Controls.Add(MakeFieldLabel2("Shipping label template (.docx)"));
        flow.Controls.Add(MakeBrowseRow(out _txtShippingTemplate, @"e.g. C:\Templates\shipping_label.docx", (s, e) => BrowseFile(_txtShippingTemplate)));
        flow.Controls.Add(MakeHint2("Used by the Shipping Labels tab when printing outbound box labels"));
        flow.Controls.Add(MakeDivider2());

        // ── NEW: Calibration Labels Section ───────────────────
        flow.Controls.Add(MakeSectionLabel("Calibration Labels"));
        flow.Controls.Add(MakeFieldLabel2("Number of calibration/test labels"));
        
        var pnlCalib = new Panel { Width = 660, Height = 40, Margin = new Padding(0, 2, 0, 0) };
        _numCalibrationCount = new NumericUpDown
        {
            Location = new Point(0, 0),
            Width = 80,
            Minimum = 0,
            Maximum = 100,
            Value = 20,
            ThousandsSeparator = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        var lblCalibNote = new Label
        {
            Text = "Test labels printed before real labels (0 = disabled)",
            Location = new Point(90, 4),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Segoe UI", 8.5f)
        };
        pnlCalib.Controls.AddRange(new Control[] { _numCalibrationCount, lblCalibNote });
        flow.Controls.Add(pnlCalib);
        flow.Controls.Add(MakeHint2("These labels will show 'CALIBRATION LABEL / DO NOT MAIL / TEST PRINT ONLY'"));
        flow.Controls.Add(MakeDivider2());

        // ── NEW: Batch Printing Section ───────────────────────
        flow.Controls.Add(MakeSectionLabel("Batch Printing"));
        
        var pnlBatch = new Panel { Width = 660, Height = 40, Margin = new Padding(0, 2, 0, 0) };
        _chkEnableBatchPrinting = new CheckBox
        {
            Text = "Enable batch printing mode",
            Location = new Point(0, 5),
            AutoSize = true,
            Checked = false,
            Font = new Font("Segoe UI", 9f)
        };
        pnlBatch.Controls.Add(_chkEnableBatchPrinting);
        flow.Controls.Add(pnlBatch);
        
        var pnlBatchSize = new Panel { Width = 660, Height = 35, Margin = new Padding(0, 0, 0, 0) };
        var lblBatchSize = new Label
        {
            Text = "Labels per batch:",
            Location = new Point(20, 5),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(50, 50, 50)
        };
        _numBatchSize = new NumericUpDown
        {
            Location = new Point(160, 2),
            Width = 100,
            Minimum = 500,
            Maximum = 5000,
            Value = 1000,
            Increment = 500,
            ThousandsSeparator = true,
            Enabled = false,
            Font = new Font("Segoe UI", 9.5f)
        };
        var lblBatchNote = new Label
        {
            Text = "(500-5000 labels) - Splits large print jobs into multiple Word documents",
            Location = new Point(270, 7),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Segoe UI", 8.5f)
        };
        pnlBatchSize.Controls.AddRange(new Control[] { lblBatchSize, _numBatchSize, lblBatchNote });
        flow.Controls.Add(pnlBatchSize);
        
        _chkEnableBatchPrinting.CheckedChanged += (s, e) =>
        {
            _numBatchSize.Enabled = _chkEnableBatchPrinting.Checked;
        };
        
        flow.Controls.Add(MakeHint2("When enabled, quantities will be rounded up to multiples of the batch size"));
        flow.Controls.Add(MakeDivider2());





        // Add after the Batch Printing section, before Security section
        flow.Controls.Add(MakeDivider2());
        flow.Controls.Add(MakeSectionLabel("Display Settings"));

        var pnlSerialDigits = new Panel { Width = 660, Height = 40, Margin = new Padding(0, 2, 0, 0) };
        var lblSerialDigits = new Label
        {
            Text = "Serial number display:",
            Location = new Point(0, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(50, 50, 50)
        };
      
      
        _cmbSerialDigits = new ComboBox
        {
            Location = new Point(180, 5),
            Width = 80,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        _cmbSerialDigits.Items.AddRange(new object[] { "6 digits", "9 digits" });
        _cmbSerialDigits.SelectedIndex = AppSettings.SerialDisplayDigits == 6 ? 0 : 1;

        var lblSerialHint = new Label
        {
            Text = "How serial numbers appear in filenames and logs (database always uses 9 digits)",
            Location = new Point(270, 8),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Segoe UI", 8f)
        };
        pnlSerialDigits.Controls.AddRange(new Control[] { lblSerialDigits, _cmbSerialDigits, lblSerialHint });
        flow.Controls.Add(pnlSerialDigits);






        // ── Security ──────────────────────────────────────────
        flow.Controls.Add(MakeSectionLabel("Security"));
        flow.Controls.Add(MakeFieldLabel2("Admin PIN"));
        _txtAdminPin = new TextBox
        {
            Width           = 280,
            Height          = 28,
            PasswordChar    = '*',
            PlaceholderText = "Set a PIN to secure resets",
            Font            = new Font("Segoe UI", 9.5f),
            Margin          = new Padding(0, 2, 0, 0)
        };
        flow.Controls.Add(_txtAdminPin);
        flow.Controls.Add(MakeHint2("Required to use the Sequence Reset tab"));

        scroll.Controls.Add(flow);
        tab.Controls.Add(scroll);
    }

    // ── Template list actions ─────────────────────────────────
    private void OnTemplateSelectionChanged(object? s, EventArgs e)
    {
        var idx = _lstTemplates.SelectedIndex;
        var hasItem = idx >= 0 && idx < _templates.Count;
        _btnTplEdit.Enabled   = hasItem;
        _btnTplRemove.Enabled = hasItem;
        _lblTemplatePath.Text = hasItem ? "Path: " + _templates[idx].Path : "";
    }

    private void OnTemplateAdd(object? s, EventArgs e)
    {
        using var dlg = new WordTemplateForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _templates.Add(dlg.Result);
        RefreshTemplateList(dlg.Result.Name);
    }

    private void OnTemplateEdit(object? s, EventArgs e)
    {
        var idx = _lstTemplates.SelectedIndex;
        if (idx < 0 || idx >= _templates.Count) return;
        using var dlg = new WordTemplateForm(_templates[idx]);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _templates[idx] = dlg.Result;
        RefreshTemplateList(dlg.Result.Name);
    }

    private void OnTemplateRemove(object? s, EventArgs e)
    {
        var idx = _lstTemplates.SelectedIndex;
        if (idx < 0 || idx >= _templates.Count) return;
        var name = _templates[idx].Name;
        if (MessageBox.Show("Remove template \"" + name + "\"?", "Confirm remove",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _templates.RemoveAt(idx);
        RefreshTemplateList(null);
    }

    private void RefreshTemplateList(string? selectName)
    {
        _lstTemplates.BeginUpdate();
        _lstTemplates.Items.Clear();
        foreach (var t in _templates)
            _lstTemplates.Items.Add(t.Name);
        _lstTemplates.EndUpdate();

        if (selectName != null)
        {
            var idx = _templates.FindIndex(t => t.Name == selectName);
            if (idx >= 0) _lstTemplates.SelectedIndex = idx;
        }
        OnTemplateSelectionChanged(null, EventArgs.Empty);
    }

    // ══════════════════════════════════════════════════════════
    //  RESET TAB
    // ══════════════════════════════════════════════════════════
    private void BuildResetTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(250, 251, 253);
        tab.Padding   = new Padding(20, 16, 20, 16);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        // Warning banner
        var banner = new Panel { Location = new Point(0, 0), Size = new Size(580, 52), BackColor = Color.FromArgb(255, 243, 243) };
        var bannerBorder = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Color.DarkRed };
        var bannerText   = new Label
        {
            Text      = "Resetting sequences is permanent and cannot be undone. Both actions require\nyour Admin PIN (set in the General tab) and the confirmation phrase RESET.",
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(140, 0, 0),
            Font      = new Font("Segoe UI", 8.5f),
            Padding   = new Padding(10, 6, 8, 6)
        };
        banner.Controls.Add(bannerText);
        banner.Controls.Add(bannerBorder);
        panel.Controls.Add(banner);

        // Section: Reset single site
        panel.Controls.Add(MakeSectionHeader("Reset a single site", 72));
        panel.Controls.Add(MakeFieldLabel("Select site", 110));
        _cboResetSite = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location      = new Point(0, 134),
            Width         = 440,
            Font          = new Font("Segoe UI", 9.5f)
        };
        _cboResetSite.Items.Add("— choose a site —");
        _sites.ForEach(s => _cboResetSite.Items.Add(s.SiteName + "  [" + s.LIBCODE + "]"));
        _cboResetSite.SelectedIndex = 0;
        panel.Controls.Add(_cboResetSite);

        _btnResetSite = new Button
        {
            Text      = "Reset selected site sequence...",
            Location  = new Point(0, 174),
            Size      = new Size(240, 34),
            BackColor = Color.FromArgb(160, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnResetSite.FlatAppearance.BorderSize = 0;
        _btnResetSite.Click += OnResetSite;
        panel.Controls.Add(_btnResetSite);
        panel.Controls.Add(MakeHint("Resets sequence to a number you specify. Optionally clears print history for this site.", 214));

        // Divider
        panel.Controls.Add(MakeDivider(244));

        // Section: Reset all
        panel.Controls.Add(MakeSectionHeader("Reset all sites", 256));

        _btnResetAll = new Button
        {
            Text      = "Reset ALL site sequences...",
            Location  = new Point(0, 294),
            Size      = new Size(220, 34),
            BackColor = Color.FromArgb(100, 0, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnResetAll.FlatAppearance.BorderSize = 0;
        _btnResetAll.Click += OnResetAll;
        panel.Controls.Add(_btnResetAll);
        panel.Controls.Add(MakeHint("Resets every site to the same starting number. Use when going from demo to production.", 334));

        tab.Controls.Add(panel);
    }

    // ══════════════════════════════════════════════════════════
    //  ACTIONS
    // ══════════════════════════════════════════════════════════
    private void LoadSettings()
    {
        _txtTempFolder.Text       = AppSettings.TempFolder;
        _txtShippingTemplate.Text = AppSettings.ShippingTemplatePath;
        _txtAdminPin.Text         = AppSettings.AdminPin;
        _numCalibrationCount.Value = AppSettings.CalibrationCount;
        _numBatchSize.Value = AppSettings.BatchSize;
        
        // Add this line
        if (_cmbSerialDigits != null)
            _cmbSerialDigits.SelectedIndex = AppSettings.SerialDisplayDigits == 6 ? 0 : 1;
        
        // Populate template list
        _templates = AppSettings.WordTemplates.Select(t => new WordTemplate { Name = t.Name, Path = t.Path }).ToList();
        RefreshTemplateList(null);
    }


    private void OnSave(object? s, EventArgs e)
    {
        AppSettings.TempFolder            = _txtTempFolder.Text.Trim();
        AppSettings.ShippingTemplatePath  = _txtShippingTemplate.Text.Trim();
        AppSettings.AdminPin              = _txtAdminPin.Text.Trim();
        AppSettings.CalibrationCount      = (int)_numCalibrationCount.Value;
        AppSettings.BatchSize             = (int)_numBatchSize.Value;
        AppSettings.WordTemplates         = _templates.Select(t => new WordTemplate { Name = t.Name, Path = t.Path }).ToList();
        
        // Save serial display digits setting
        if (_cmbSerialDigits != null)
        {
            AppSettings.SerialDisplayDigits = _cmbSerialDigits.SelectedIndex == 0 ? 6 : 9;
        }
        
        AppSettings.Save();
        MessageBox.Show("Settings saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }






    private void OnResetSite(object? s, EventArgs e)
    {
        if (_cboResetSite.SelectedIndex == 0)
        {
            MessageBox.Show("Please select a site.", "No site selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var site = _sites[_cboResetSite.SelectedIndex - 1];
        using var confirm = new ResetConfirmForm(site.SiteName + "  [" + site.LIBCODE + "]");
        if (confirm.ShowDialog(this) != DialogResult.OK) return;
        TRL.Program.DB.ResetSiteSequence(site.Id, confirm.StartAt, confirm.ClearHistory);
        var nl = Environment.NewLine;
        MessageBox.Show(
            site.SiteName + " reset to serial " + confirm.StartAt.ToString("D9") + "." + nl +
            (confirm.ClearHistory ? "Print history for this site cleared." : "Print history kept."),
            "Reset complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnResetAll(object? s, EventArgs e)
    {
        using var confirm = new ResetConfirmForm("ALL sites");
        if (confirm.ShowDialog(this) != DialogResult.OK) return;
        TRL.Program.DB.ResetAllSequences(confirm.StartAt, confirm.ClearHistory);
        var nl = Environment.NewLine;
        MessageBox.Show(
            "All sites reset to serial " + confirm.StartAt.ToString("D9") + "." + nl +
            (confirm.ClearHistory ? "Print history cleared." : "Print history kept."),
            "Reset complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ══════════════════════════════════════════════════════════
    //  UI HELPERS — General tab (flow layout)
    // ══════════════════════════════════════════════════════════
    private static Label MakeSectionLabel(string text) => new Label
    {
        Text      = text.ToUpper(),
        Width     = 660,
        Height    = 26,
        Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 90, 160),
        Margin    = new Padding(0, 8, 0, 2)
    };

    private static Label MakeFieldLabel2(string text) => new Label
    {
        Text      = text,
        Width     = 660,
        Height    = 20,
        Font      = new Font("Segoe UI", 9f),
        ForeColor = Color.FromArgb(50, 50, 50),
        Margin    = new Padding(0, 4, 0, 2)
    };

    private static Label MakeHint2(string text) => new Label
    {
        Text      = text,
        Width     = 660,
        Height    = 18,
        Font      = new Font("Segoe UI", 8f),
        ForeColor = Color.FromArgb(130, 130, 130),
        Margin    = new Padding(0, 2, 0, 4)
    };

    private static Panel MakeDivider2() => new Panel
    {
        Width     = 660,
        Height    = 1,
        BackColor = Color.FromArgb(218, 220, 226),
        Margin    = new Padding(0, 10, 0, 10)
    };

    private static Panel MakeSpacer(int h) => new Panel { Width = 660, Height = h };

    private Panel MakeBrowseRow(out TextBox txt, string placeholder, EventHandler browseClick)
    {
        var row = new Panel { Width = 660, Height = 28, Margin = new Padding(0, 2, 0, 0) };
        var t   = new TextBox { Location = new Point(0, 0), Width = 556, Height = 28, PlaceholderText = placeholder, Font = new Font("Segoe UI", 9.5f) };
        var btn = new Button  { Text = "Browse...", Location = new Point(564, 0), Width = 90, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f), BackColor = Color.FromArgb(245, 246, 248) };
        btn.Click += browseClick;
        row.Controls.AddRange(new Control[] { t, btn });
        txt = t;
        return row;
    }

    private static Button MakeSmallButton(string text, Point location) => new Button
    {
        Text      = text,
        Location  = location,
        Size      = new Size(110, 28),
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9f),
        BackColor = Color.FromArgb(237, 238, 242),
        Enabled   = text != "Edit" && text != "Remove"   // disabled until selection
    };

    // ── Reset tab helpers ─────────────────────────────────────
    private static Label MakeSectionHeader(string text, int top) => new Label
    {
        Text      = text.ToUpper(),
        Location  = new Point(0, top),
        Size      = new Size(580, 22),
        Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 90, 160),
        AutoSize  = false
    };

    private static Label MakeFieldLabel(string text, int top) => new Label
    {
        Text      = text,
        Location  = new Point(0, top),
        Size      = new Size(580, 20),
        Font      = new Font("Segoe UI", 9f),
        ForeColor = Color.FromArgb(50, 50, 50),
        AutoSize  = false
    };

    private static Label MakeHint(string text, int top) => new Label
    {
        Text      = text,
        Location  = new Point(0, top),
        Size      = new Size(580, 18),
        Font      = new Font("Segoe UI", 8f),
        ForeColor = Color.FromArgb(130, 130, 130),
        AutoSize  = false
    };

    private static Panel MakeDivider(int top) => new Panel
    {
        Location  = new Point(0, top),
        Size      = new Size(580, 1),
        BackColor = Color.FromArgb(218, 220, 226)
    };

    private static void BrowseFolder(TextBox txt)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = txt.Text };
        if (dlg.ShowDialog() == DialogResult.OK) txt.Text = dlg.SelectedPath;
    }

    private static void BrowseFile(TextBox txt)
    {
        using var dlg = new OpenFileDialog { Filter = "Word documents (*.docx;*.doc)|*.docx;*.doc", FileName = txt.Text };
        if (dlg.ShowDialog() == DialogResult.OK) txt.Text = dlg.FileName;
    }
}