using ShippingLabelManager.Models;
using ShippingLabelManager.Services;

namespace ShippingLabelManager.Forms;

/// <summary>
/// Persistent print session dialog.
/// Shows site info + serial range, lets the user pick a template and open Word
/// as many times as needed, then either confirms (advances sequence) or cancels.
/// This form never touches the database — all DB work is done by the caller.
/// </summary>
public class PrintSessionForm : Form
{
    private readonly Site              _site;
    private readonly int               _fromSerial;
    private readonly int               _toSerial;
    private readonly int               _qty;
    private readonly string            _csvPath;
    private readonly List<WordTemplate> _templates;
    private readonly Action<string, string>? _onPrintAttempt;

    /// <summary>
    /// Name of the template from the most recent successful Word open this session.
    /// </summary>
    public string LastTemplateName { get; private set; } = "";

    /// <summary>
    /// All template names used in Merge &amp; Print attempts this session.
    /// </summary>
    public List<string> AttemptedTemplates { get; } = new();

    private ComboBox _cboTemplate      = null!;
    private ListBox  _lstLog           = null!;
    private Label    _lblLogPlaceholder = null!;
    private Button   _btnOpen          = null!, _btnConfirm = null!, _btnCancel = null!;

    public PrintSessionForm(Site site, int fromSerial, int qty, string csvPath,
                            List<WordTemplate> templates, Action<string, string>? onPrintAttempt = null)
    {
        _site            = site;
        _fromSerial      = fromSerial;
        _toSerial        = fromSerial + qty - 1;
        _qty             = qty;
        _csvPath         = csvPath;
        _templates       = templates;
        _onPrintAttempt  = onPrintAttempt;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text            = "Print session — " + _site.SiteName + "  [" + _site.LIBCODE + "]";
        Size            = new Size(620, 490);
        MinimumSize     = new Size(520, 400);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 9.5f);

        // ── Site / serial info banner (Top) ───────────────────
        var pnlInfo = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 72,
            BackColor = Color.FromArgb(240, 244, 252),
            Padding   = new Padding(16, 10, 16, 8)
        };
        var infoDivider = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 1,
            BackColor = Color.FromArgb(200, 210, 230)
        };

        var tblInfo = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 2,
            Padding     = new Padding(0)
        };
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        tblInfo.Controls.Add(MakeInfoKey("Site:"),    0, 0);
        tblInfo.Controls.Add(MakeInfoKey("Serials:"), 0, 1);
        tblInfo.Controls.Add(MakeInfoVal(_site.SiteName + "  [" + _site.LIBCODE + "]"),
            1, 0);
        tblInfo.Controls.Add(MakeInfoVal(
            _fromSerial.ToString("D9") + "  →  " + _toSerial.ToString("D9") +
            "  (" + _qty.ToString("N0") + " labels)"),
            1, 1);

        // Dock order inside pnlInfo: Fill first, Bottom last
        pnlInfo.Controls.Add(tblInfo);     // Fill — first
        pnlInfo.Controls.Add(infoDivider); // Bottom — last

        // ── Template selector + Open button (Top) ─────────────
        var pnlTemplate = new Panel
        {
            Dock    = DockStyle.Top,
            Height  = 54,
            Padding = new Padding(16, 0, 16, 0)
        };
        var tplDivider = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 1,
            BackColor = Color.FromArgb(210, 215, 225)
        };

        var lblTpl = new Label
        {
            Text      = "Template:",
            AutoSize  = true,
            Location  = new Point(0, 17),
            ForeColor = Color.FromArgb(50, 50, 50)
        };
        _cboTemplate = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location      = new Point(82, 13),
            Width         = 270,
            Font          = new Font("Segoe UI", 9.5f)
        };
        foreach (var t in _templates)
            _cboTemplate.Items.Add(t.Name);
        if (_cboTemplate.Items.Count > 0) _cboTemplate.SelectedIndex = 0;

        _btnOpen = new Button
        {
            Text      = "Merge & Print",
            Location  = new Point(362, 11),
            Size      = new Size(150, 30),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.Click += OnOpenInWord;

        pnlTemplate.Controls.AddRange(new Control[] { tplDivider, lblTpl, _cboTemplate, _btnOpen });

        // ── Confirm / Cancel bar (Bottom — add before Fill) ───
        var pnlBtns = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 72,
            BackColor = Color.FromArgb(245, 246, 248)
        };
        var btnsDivider = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = Color.FromArgb(210, 212, 218)
        };
        var lblWhen = new Label
        {
            Text      = "Did all labels print successfully?",
            AutoSize  = true,
            Location  = new Point(16, 10),
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        _btnConfirm = new Button
        {
            Text      = "✓  Confirm — labels printed",
            Size      = new Size(234, 34),
            Location  = new Point(16, 28),
            BackColor = Color.FromArgb(0, 150, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnConfirm.FlatAppearance.BorderSize = 0;
        _btnConfirm.Click += OnConfirm;

        _btnCancel = new Button
        {
            Text         = "✗  Cancel — reprint needed",
            Size         = new Size(190, 34),
            Location     = new Point(258, 28),
            BackColor    = Color.FromArgb(100, 100, 100),
            ForeColor    = Color.White,
            FlatStyle    = FlatStyle.Flat,
            Font         = new Font("Segoe UI", 9.5f),
            DialogResult = DialogResult.Cancel
        };
        _btnCancel.FlatAppearance.BorderSize = 0;

        pnlBtns.Controls.AddRange(new Control[] { btnsDivider, lblWhen, _btnConfirm, _btnCancel });

        // ── Session log (Fill — must be added last) ───────────
        var pnlLog = new Panel
        {
            Dock    = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 8)
        };
        var lblLog = new Label
        {
            Text      = "Print attempts this session:",
            Dock      = DockStyle.Top,
            Height    = 22,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(80, 80, 80),
            AutoSize  = false
        };

        // Single Fill container — swaps between placeholder and listbox
        var pnlLogContent = new Panel { Dock = DockStyle.Fill };

        _lblLogPlaceholder = new Label
        {
            Text      = "No print attempts yet — click Merge & Print to begin",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(160, 160, 160),
            Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
            Visible   = true
        };
        _lstLog = new ListBox
        {
            Dock        = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Courier New", 9f),
            BackColor   = Color.FromArgb(248, 250, 253),
            Visible     = false
        };

        // _lstLog hidden first, placeholder visible on top — added last so it renders above
        pnlLogContent.Controls.Add(_lstLog);            // Fill — first (behind)
        pnlLogContent.Controls.Add(_lblLogPlaceholder); // Fill — last  (on top initially)

        // pnlLog: Fill container first, Top label last
        pnlLog.Controls.Add(pnlLogContent); // Fill — first
        pnlLog.Controls.Add(lblLog);        // Top  — last

        // Form-level dock order: Fill first, Bottom second, Top panels last
        Controls.Add(pnlLog);       // Fill   — first
        Controls.Add(pnlBtns);     // Bottom — second
        Controls.Add(pnlTemplate); // Top    — third
        Controls.Add(pnlInfo);     // Top    — last (topmost)

        CancelButton = _btnCancel;
    }

    // ── Handlers ──────────────────────────────────────────────
    private void OnOpenInWord(object? s, EventArgs e)
    {
        if (_cboTemplate.SelectedIndex < 0)
        {
            MessageBox.Show("Please select a template.", "No template selected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var template = _templates[_cboTemplate.SelectedIndex];
        LastTemplateName = template.Name;
        AttemptedTemplates.Add(template.Name);
        var timestamp = DateTime.Now.ToString("HH:mm");

        // Show immediate feedback in log
        _lblLogPlaceholder.Visible = false;
        _lstLog.Visible            = true;
        _lstLog.Items.Add(timestamp + "  " + template.Name + "  →  Merging & sending to printer...");
        _lstLog.TopIndex           = _lstLog.Items.Count - 1;
        _lstLog.Refresh();

        // Disable button while Word is launching
        _btnOpen.Enabled = false;
        _btnOpen.Text    = "Processing...";

        // Log ATTEMPT to DB immediately
        _onPrintAttempt?.Invoke(template.Name, "ATTEMPT");

        // Capture locals for the background thread
        var csvPath      = _csvPath;
        var templatePath = template.Path;
        var templateName = template.Name;
        var ts           = timestamp;

        Task.Run(() =>
        {
            string resultMsg;
            try
            {
                WordMergeService.OpenInWord(csvPath, templatePath);
                resultMsg = ts + "  " + templateName + "  →  Print dialog shown — waiting for user";
            }
            catch (Exception ex)
            {
                resultMsg = ts + "  " + templateName + "  →  ERROR: " + ex.Message;
            }

            // Marshal back to UI thread
            Invoke(() =>
            {
                var idx = _lstLog.Items.Count - 1;
                if (idx >= 0) _lstLog.Items[idx] = resultMsg;
                _lstLog.TopIndex = _lstLog.Items.Count - 1;
                _lstLog.Refresh();
                _btnOpen.Enabled = true;
                _btnOpen.Text    = "Merge & Print";
                BringToFront();
                Focus();
                Activate();
            });
        });
    }

    private void OnConfirm(object? s, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────
    private static Label MakeInfoKey(string text) => new Label
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(90, 90, 90),
        Font      = new Font("Segoe UI", 9f)
    };

    private static Label MakeInfoVal(string text) => new Label
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(15, 55, 115),
        Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        AutoSize  = false
    };
}
