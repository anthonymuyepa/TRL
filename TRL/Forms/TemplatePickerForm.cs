using TRL.Models;

namespace TRL.Forms;

/// <summary>
/// Shown when multiple Word templates are configured.
/// Displays a list of template names; user picks one and clicks Select.
/// </summary>
public class TemplatePickerForm : Form
{
    private readonly List<WordTemplate> _templates;
    private ListView  _list  = null!;
    private Label     _lblPath = null!;
    private Button    _btnSelect = null!, _btnCancel = null!;

    public WordTemplate? SelectedTemplate { get; private set; }

    public TemplatePickerForm(List<WordTemplate> templates)
    {
        _templates = templates;
        InitializeComponent();
        PopulateList();
    }

    private void InitializeComponent()
    {
        Text            = "Select template";
        Size            = new Size(560, 340);
        MinimumSize     = new Size(440, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 9.5f);
        BackColor       = Color.FromArgb(245, 246, 248);

        // ── Header ────────────────────────────────────────────
        var pnlTop = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 50,
            BackColor = Color.FromArgb(240, 244, 252),
            Padding   = new Padding(16, 10, 16, 8)
        };
        pnlTop.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(210, 218, 235) });
        pnlTop.Controls.Add(new Label
        {
            Text      = "Select the Word template to use for this export:",
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(20, 60, 120),
            Font      = new Font("Segoe UI", 9.5f),
            AutoSize  = false
        });
        Controls.Add(pnlTop);

        // ── Buttons ───────────────────────────────────────────
        var pnlBtns = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 54,
            BackColor = Color.FromArgb(237, 238, 242)
        };
        pnlBtns.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 212, 218) });

        _btnSelect = new Button
        {
            Text      = "Select",
            Size      = new Size(90, 32),
            Location  = new Point(16, 11),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Enabled   = false
        };
        _btnSelect.FlatAppearance.BorderSize = 0;
        _btnSelect.Click += (s, e) => Confirm();

        _btnCancel = new Button
        {
            Text         = "Cancel",
            Size         = new Size(80, 32),
            Location     = new Point(114, 11),
            FlatStyle    = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel,
            Font         = new Font("Segoe UI", 9.5f)
        };
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);

        pnlBtns.Controls.AddRange(new Control[] { _btnSelect, _btnCancel });
        Controls.Add(pnlBtns);

        // ── Path hint ─────────────────────────────────────────
        var pnlPath = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(12, 6, 12, 0) };
        _lblPath = new Label
        {
            Dock      = DockStyle.Fill,
            AutoSize  = false,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font      = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlPath.Controls.Add(_lblPath);
        Controls.Add(pnlPath);

        // ── List ──────────────────────────────────────────────
        _list = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            MultiSelect   = false,
            HeaderStyle   = ColumnHeaderStyle.Nonclickable,
            BorderStyle   = BorderStyle.None,
            Font          = new Font("Segoe UI", 10f),
            BackColor     = SystemColors.Window,
            GridLines     = false,
            ShowItemToolTips = true
        };
        _list.Columns.Add("Template name", -2, HorizontalAlignment.Left);
        _list.SelectedIndexChanged += OnSelectionChanged;
        _list.DoubleClick          += (s, e) => { if (_btnSelect.Enabled) Confirm(); };
        Controls.Add(_list);

        AcceptButton = _btnSelect;
        CancelButton = _btnCancel;
    }

    private void PopulateList()
    {
        foreach (var t in _templates)
        {
            var item = new ListViewItem(t.Name) { ToolTipText = t.Path, Tag = t };
            _list.Items.Add(item);
        }
        // Stretch the single column to fill
        _list.Columns[0].Width = -2;

        // Auto-select if there's only one
        if (_list.Items.Count == 1)
            _list.Items[0].Selected = true;
    }

    private void OnSelectionChanged(object? s, EventArgs e)
    {
        var selected = _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as WordTemplate : null;
        _btnSelect.Enabled = selected != null;
        _lblPath.Text      = selected != null ? "Path: " + selected.Path : "";
    }

    private void Confirm()
    {
        if (_list.SelectedItems.Count == 0) return;
        SelectedTemplate = _list.SelectedItems[0].Tag as WordTemplate;
        DialogResult     = DialogResult.OK;
        Close();
    }
}
