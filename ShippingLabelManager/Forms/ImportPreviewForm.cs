using ShippingLabelManager.Models;
using ShippingLabelManager.Services;

namespace ShippingLabelManager.Forms;

public class ImportPreviewForm : Form
{
    private readonly List<Site> _toInsert;
    private readonly List<(Site existing, Site updated)> _toUpdate;
    private readonly CsvImportService.ImportResult _result;

    private TabControl _tabs = null!;
    private DataGridView _dgvInsert = null!, _dgvUpdate = null!;
    private Label _lblSummary = null!;
    private Button _btnProceed = null!, _btnCancel = null!;

    public ImportPreviewForm(
        List<Site> toInsert,
        List<(Site existing, Site updated)> toUpdate,
        CsvImportService.ImportResult result)
    {
        _toInsert = toInsert;
        _toUpdate = toUpdate;
        _result   = result;
        InitializeComponent();
        PopulateGrids();
        UpdateSummary();
    }

    private void InitializeComponent()
    {
        Text = "Import preview";
        Size = new Size(1020, 640);
        MinimumSize = new Size(860, 500);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);

        // ── Summary bar ───────────────────────────────────────
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(240, 244, 252), Padding = new Padding(16, 10, 16, 8) };
        var topDiv = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(210, 218, 235) };
        _lblSummary = new Label { Dock = DockStyle.Fill, AutoSize = false, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(20, 60, 120) };
        pnlTop.Controls.Add(_lblSummary);
        pnlTop.Controls.Add(topDiv);
        Controls.Add(pnlTop);

        // ── Buttons (added before Fill so dock order is correct) ──
        var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.FromArgb(245, 246, 248) };
        var btnDiv = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(218, 220, 226) };
        _btnProceed = new Button
        {
            Text = BuildProceedLabel(),
            Size = new Size(220, 34),
            Location = new Point(16, 10),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            DialogResult = DialogResult.OK,
            Enabled = (_toInsert.Count + _toUpdate.Count) > 0
        };
        _btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(90, 34),
            Location = new Point(244, 10),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat
        };
        pnlBtns.Controls.AddRange(new Control[] { btnDiv, _btnProceed, _btnCancel });
        Controls.Add(pnlBtns);
        AcceptButton = _btnProceed;
        CancelButton = _btnCancel;

        // ── Errors panel (added before Fill) ─────────────────
        if (_result.Errors > 0 || _result.Skipped > 0)
        {
            var pnlErr = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.FromArgb(255, 248, 235), Padding = new Padding(12, 8, 12, 4) };
            var errDiv = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 200, 140) };
            var lblErr = new Label
            {
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(130, 80, 0),
                Text = BuildErrorText()
            };
            pnlErr.Controls.Add(lblErr);
            pnlErr.Controls.Add(errDiv);
            Controls.Add(pnlErr);
        }

        // ── Tabs (Fill — must be added LAST so it takes remaining space) ──
        _tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        var tabInsert = new TabPage("  New sites (" + _toInsert.Count + ")  ");
        var tabUpdate = new TabPage("  Updates (" + _toUpdate.Count + ")  ");
        _tabs.TabPages.AddRange(new[] { tabInsert, tabUpdate });

        _dgvInsert = MakeGrid();
        AddColumn(_dgvInsert, "LIBCODE",    "LIBCODE",    90);
        AddColumn(_dgvInsert, "SiteName",   "Site name",  180);
        AddColumn(_dgvInsert, "SHIP_ADD_1", "SHIP_ADD_1", 150);
        AddColumn(_dgvInsert, "SHIP_ADD_2", "SHIP_ADD_2", 140);
        AddColumn(_dgvInsert, "ZIP9",       "ZIP-9",      95);
        AddColumn(_dgvInsert, "MID9",       "MID9",       110);
        AddColumn(_dgvInsert, "STC",        "STC",        50);
        AddColumn(_dgvInsert, "ChannelAI",  "ChannelAI",  80);
        _dgvInsert.Dock = DockStyle.Fill;
        tabInsert.Controls.Add(_dgvInsert);

        _dgvUpdate = MakeGrid();
        AddColumn(_dgvUpdate, "LIBCODE",  "LIBCODE",       90);
        AddColumn(_dgvUpdate, "Field",    "Field",         120);
        AddColumn(_dgvUpdate, "OldValue", "Current value", 200);
        AddColumn(_dgvUpdate, "NewValue", "New value",     200);
        AddColumn(_dgvUpdate, "Changed",  "Changed",       70);
        _dgvUpdate.Dock = DockStyle.Fill;
        tabUpdate.Controls.Add(_dgvUpdate);

        Controls.Add(_tabs);

        // Auto-select the tab that has data
        _tabs.SelectedTab = _toUpdate.Count > 0 ? tabUpdate : tabInsert;
    }

    private void PopulateGrids()
    {
        // Insert grid
        _dgvInsert.SuspendLayout();
        foreach (var s in _toInsert)
            _dgvInsert.Rows.Add(s.LIBCODE, s.SiteName ?? "", s.SHIP_ADD_1 ?? "",
                s.SHIP_ADD_2 ?? "", s.ZIP9Dash ?? "", s.MID9 ?? "", s.STC ?? "", s.ChannelAI ?? "");
        _dgvInsert.ResumeLayout();

        // Update grid — diff each field
        _dgvUpdate.SuspendLayout();
        var fields = new (string label, Func<Site, string> get)[]
        {
            ("SiteName",   s => s.SiteName   ?? ""),
            ("SHIP_ADD_1", s => s.SHIP_ADD_1  ?? ""),
            ("SHIP_ADD_2", s => s.SHIP_ADD_2  ?? ""),
            ("SHIP_ADD_3", s => s.SHIP_ADD_3  ?? ""),
            ("SHIP_ADD_4", s => s.SHIP_ADD_4  ?? ""),
            ("ZIP-9",      s => s.ZIP9Dash    ?? ""),
            ("MID9",       s => s.MID9        ?? ""),
            ("STC",        s => s.STC         ?? ""),
            ("ChannelAI",  s => s.ChannelAI   ?? ""),
        };

        foreach (var (existing, updated) in _toUpdate)
        {
            foreach (var (label, get) in fields)
            {
                var oldVal = get(existing);
                var newVal = get(updated);
                bool changed = !string.Equals(oldVal, newVal, StringComparison.Ordinal);
                var rowIdx = _dgvUpdate.Rows.Add(existing.LIBCODE, label, oldVal, newVal, changed ? "YES" : "");
                if (changed)
                {
                    _dgvUpdate.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 235);
                    _dgvUpdate.Rows[rowIdx].Cells["NewValue"].Style.ForeColor = Color.DarkGreen;
                    _dgvUpdate.Rows[rowIdx].Cells["NewValue"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    _dgvUpdate.Rows[rowIdx].Cells["Changed"].Style.ForeColor = Color.DarkOrange;
                }
            }
            // Separator row
            var sepIdx = _dgvUpdate.Rows.Add("", "", "", "", "");
            _dgvUpdate.Rows[sepIdx].DefaultCellStyle.BackColor = Color.FromArgb(230, 234, 242);
            _dgvUpdate.Rows[sepIdx].Height = 4;
        }
        _dgvUpdate.ResumeLayout();
    }

    private void UpdateSummary()
    {
        var parts = new List<string>();
        if (_toInsert.Count > 0) parts.Add(_toInsert.Count + " new site" + (_toInsert.Count > 1 ? "s" : "") + " to insert");
        if (_toUpdate.Count > 0) parts.Add(_toUpdate.Count + " existing site" + (_toUpdate.Count > 1 ? "s" : "") + " to update");
        if (_result.Skipped > 0) parts.Add(_result.Skipped + " skipped");
        if (_result.Errors  > 0) parts.Add(_result.Errors  + " row errors");
        _lblSummary.Text = parts.Count > 0
            ? string.Join("     |     ", parts) + "\r\nReview the tabs below then click Proceed to apply."
            : "Nothing to import.";
    }

    private string BuildProceedLabel()
    {
        var parts = new List<string>();
        if (_toInsert.Count > 0) parts.Add("Insert " + _toInsert.Count);
        if (_toUpdate.Count > 0) parts.Add("Update " + _toUpdate.Count);
        return parts.Count > 0 ? "Proceed (" + string.Join(" + ", parts) + ")" : "Nothing to do";
    }

    private string BuildErrorText()
    {
        var parts = new List<string>();
        if (_result.Skipped > 0) parts.Add(_result.Skipped + " row(s) skipped");
        if (_result.Errors  > 0) parts.Add(_result.Errors  + " row(s) had errors: " + string.Join("; ", _result.ErrorMessages.Take(3)));
        return string.Join("   |   ", parts);
    }

    private static DataGridView MakeGrid()
    {
        var g = new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = SystemColors.Window,
            GridColor = Color.FromArgb(220, 220, 220),
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            RowTemplate = { Height = 24 }, Font = new Font("Segoe UI", 9f),
            EnableHeadersVisualStyles = false, ScrollBars = ScrollBars.Both
        };
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 234, 242);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 234, 242);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 210, 240);
        g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 20, 20);
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
        return g;
    }

    private static void AddColumn(DataGridView g, string name, string header, int width)
    {
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width });
    }
}
