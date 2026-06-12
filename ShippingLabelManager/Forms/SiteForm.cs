using ShippingLabelManager.Models;
using ShippingLabelManager.Services;

namespace ShippingLabelManager.Forms;

public class SiteForm : Form
{
    private readonly Site? _existing;
    private TextBox _txtLIBCODE = null!, _txtName = null!;
    private TextBox _txtAdd1 = null!, _txtAdd2 = null!, _txtAdd3 = null!, _txtAdd4 = null!;
    private TextBox _txtZIP9 = null!, _txtZIP9Preview = null!;
    private TextBox _txtMID9 = null!, _txtSTC = null!, _txtChannelAI = null!;
    private NumericUpDown _numSequence = null!;
    private Label _lblLastPrinted = null!;
    private Button _btnSave = null!, _btnCancel = null!;

    public SiteForm(Site? existing)
    {
        _existing = existing;
        InitializeComponent();
        if (existing != null) PopulateFields();
    }

    private void InitializeComponent()
    {
        Text = _existing == null ? "Add New Site" : $"Edit Site — {_existing.SiteName}";
        Size = new Size(620, 580);
        MinimumSize = new Size(580, 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 20,
            ColumnCount = 4,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        int row = 0;

        // ── Section: Identity ─────────────────────────────────
        AddSectionHeader(layout, "Site Identity", row++);
        _txtLIBCODE = AddRow(layout, "LIBCODE *", "", row++, 0, required: true);
        _txtName    = AddRow(layout, "Site name *", "", row++, 0, colspan: 3, required: true);

        // ── Section: Address ──────────────────────────────────
        AddSectionHeader(layout, "Address Fields", row++);
        _txtAdd1 = AddRow(layout, "SHIP_ADD_1", "Recipient / Org name", row++, 0, colspan: 3);
        _txtAdd2 = AddRow(layout, "SHIP_ADD_2", "Street address", row++, 0, colspan: 3);
        _txtAdd3 = AddRow(layout, "SHIP_ADD_3", "City, State", row++, 0, colspan: 3);
        _txtAdd4 = AddRow(layout, "SHIP_ADD_4", "Additional line", row++, 0, colspan: 3);

        // ZIP-9
        AddLabel(layout, "ZIP-9 *", row, 0);
        _txtZIP9 = new TextBox { PlaceholderText = "12345-6789", Dock = DockStyle.Fill };
        _txtZIP9.TextChanged += (s, e) => _txtZIP9Preview.Text = BarcodeService.StripDash(_txtZIP9.Text);
        layout.Controls.Add(_txtZIP9, 1, row);
        AddLabel(layout, "ZIP9 (auto)", row, 2);
        _txtZIP9Preview = new TextBox { ReadOnly = true, BackColor = SystemColors.Control, Dock = DockStyle.Fill, Font = new Font("Courier New", 9f) };
        layout.Controls.Add(_txtZIP9Preview, 3, row);
        row++;

        // ── Section: Barcode ──────────────────────────────────
        AddSectionHeader(layout, "Barcode Fields (static per site)", row++);
        _txtMID9      = AddRow(layout, "MID9 *", "9-digit Mailer ID", row, 0, required: true);
        _txtSTC       = AddRow(layout, "STC", "2-digit code", row, 2);
        row++;
        _txtChannelAI = AddRow(layout, "ChannelAI", "e.g. 420", row++, 0);

        // ── Section: Sequence ─────────────────────────────────
        AddSectionHeader(layout, "Sequence", row++);
        AddLabel(layout, "Start / resume at", row, 0);
        _numSequence = new NumericUpDown { Minimum = 1, Maximum = 9999999, Value = 1, Dock = DockStyle.Fill, ThousandsSeparator = true };
        layout.Controls.Add(_numSequence, 1, row);
        AddLabel(layout, "Last printed", row, 2);
        _lblLastPrinted = new Label { Text = "—", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Courier New", 9f) };
        layout.Controls.Add(_lblLastPrinted, 3, row);
        row++;

        Controls.Add(layout);

        // ── Buttons ───────────────────────────────────────────
        var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        _btnSave   = new Button { Text = "Save site", DialogResult = DialogResult.None, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(110, 34), Location = new Point(16, 8) };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(90, 34), Location = new Point(134, 8) };
        _btnSave.Click += OnSave;
        pnlBtns.Controls.AddRange(new Control[] { _btnSave, _btnCancel });
        Controls.Add(pnlBtns);
        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void PopulateFields()
    {
        var s = _existing!;
        _txtLIBCODE.Text       = s.LIBCODE;
        _txtName.Text          = s.SiteName;
        _txtAdd1.Text          = s.SHIP_ADD_1 ?? "";
        _txtAdd2.Text          = s.SHIP_ADD_2 ?? "";
        _txtAdd3.Text          = s.SHIP_ADD_3 ?? "";
        _txtAdd4.Text          = s.SHIP_ADD_4 ?? "";
        _txtZIP9.Text          = s.ZIP9Dash;
        _txtMID9.Text          = s.MID9;
        _txtSTC.Text           = s.STC ?? "";
        _txtChannelAI.Text     = s.ChannelAI ?? "";
        _numSequence.Value     = s.CurrentSequence;
        _lblLastPrinted.Text   = s.LastPrintedDisplay;
    }

    private void OnSave(object? s, EventArgs e)
    {
        var libcode = _txtLIBCODE.Text.Trim();
        var name    = _txtName.Text.Trim();
        var mid9    = _txtMID9.Text.Trim();
        var zip9    = _txtZIP9.Text.Trim();

        if (string.IsNullOrEmpty(libcode) || string.IsNullOrEmpty(name) ||
            string.IsNullOrEmpty(mid9)    || string.IsNullOrEmpty(zip9))
        {
            MessageBox.Show("LIBCODE, Site name, MID9, and ZIP-9 are required.", "Required fields",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int excludeId = _existing?.Id ?? 0;
        if (Program.DB.LIBCODEExists(libcode, excludeId))
        {
            MessageBox.Show($"LIBCODE '{libcode}' is already used by another site.", "Duplicate LIBCODE",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var site = _existing ?? new Site();
        site.LIBCODE        = libcode;
        site.SiteName       = name;
        site.SHIP_ADD_1     = _txtAdd1.Text.Trim();
        site.SHIP_ADD_2     = _txtAdd2.Text.Trim();
        site.SHIP_ADD_3     = _txtAdd3.Text.Trim();
        site.SHIP_ADD_4     = _txtAdd4.Text.Trim();
        site.ZIP9Dash       = zip9;
        site.MID9           = mid9;
        site.STC            = _txtSTC.Text.Trim();
        site.ChannelAI      = _txtChannelAI.Text.Trim();
        site.CurrentSequence = (int)_numSequence.Value;

        try
        {
            if (_existing == null)
                Program.DB.InsertSite(site);
            else
                Program.DB.UpdateSite(site);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error saving site: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Layout helpers ────────────────────────────────────────
    private static void AddLabel(TableLayoutPanel tbl, string text, int row, int col)
    {
        tbl.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(90, 90, 90) }, col, row);
    }

    private static void AddSectionHeader(TableLayoutPanel tbl, string text, int row)
    {
        var lbl = new Label
        {
            Text = text, Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 80, 160),
            Margin = new Padding(0, 10, 0, 2),
            TextAlign = ContentAlignment.BottomLeft
        };
        tbl.Controls.Add(lbl, 0, row);
        tbl.SetColumnSpan(lbl, 4);
        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(200, 220, 240) };
        lbl.Controls.Add(sep);
    }

    private static TextBox AddRow(TableLayoutPanel tbl, string label, string placeholder, int row, int col, int colspan = 1, bool required = false)
    {
        AddLabel(tbl, label, row, col);
        var txt = new TextBox { PlaceholderText = placeholder, Dock = DockStyle.Fill };
        if (required) txt.BackColor = Color.FromArgb(255, 252, 240);
        tbl.Controls.Add(txt, col + 1, row);
        if (colspan > 1) tbl.SetColumnSpan(txt, colspan);
        return txt;
    }
}
