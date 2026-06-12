using TRL.Models;

namespace TRL.Forms;

/// <summary>
/// Small Add / Edit dialog for a single WordTemplate entry (Name + Path).
/// </summary>
public class WordTemplateForm : Form
{
    private TextBox _txtName = null!, _txtPath = null!;
    private Button  _btnSave = null!, _btnCancel = null!;

    public WordTemplate Result { get; private set; } = new();

    public WordTemplateForm(WordTemplate? existing = null)
    {
        InitializeComponent();
        if (existing != null)
        {
            _txtName.Text = existing.Name;
            _txtPath.Text = existing.Path;
        }
    }

    private void InitializeComponent()
    {
        Text = "Word Template";
        Size = new Size(560, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 9.5f);
        BackColor       = Color.FromArgb(245, 246, 248);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            Padding     = new Padding(16, 14, 16, 8),
            ColumnCount = 2,
            RowCount    = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Name row
        layout.Controls.Add(MakeLabel("Template name *"), 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "e.g. 2x4 Single Label", BackColor = Color.FromArgb(255, 252, 240) };
        layout.Controls.Add(_txtName, 1, 0);

        // Path row — textbox + Browse button in a sub-panel
        layout.Controls.Add(MakeLabel("File path *"), 0, 1);
        var pathPanel = new Panel { Dock = DockStyle.Fill, Height = 28 };
        _txtPath = new TextBox { Location = new Point(0, 0), Height = 28, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, BackColor = Color.FromArgb(255, 252, 240) };
        var btnBrowse = new Button
        {
            Text      = "Browse...",
            Width     = 84,
            Height    = 28,
            Anchor    = AnchorStyles.Right | AnchorStyles.Top,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f),
            BackColor = Color.FromArgb(237, 238, 242)
        };
        btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
        btnBrowse.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title    = "Select Word template",
                Filter   = "Word documents (*.docx;*.doc)|*.docx;*.doc",
                FileName = _txtPath.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK) _txtPath.Text = dlg.FileName;
        };
        pathPanel.Controls.AddRange(new Control[] { _txtPath, btnBrowse });
        // Resize textbox when panel resizes
        pathPanel.Resize += (s, e) => { _txtPath.Width = pathPanel.Width - btnBrowse.Width - 6; btnBrowse.Left = pathPanel.Width - btnBrowse.Width; };
        layout.Controls.Add(pathPanel, 1, 1);

        // Hint row
        var lblHint = new Label
        {
            Text      = "Your .docx file with logos, formatting and mail merge fields already set up",
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(130, 130, 130),
            Font      = new Font("Segoe UI", 8f),
            AutoSize  = false
        };
        layout.Controls.Add(new Label(), 0, 2);
        layout.Controls.Add(lblHint, 1, 2);

        Controls.Add(layout);

        // Buttons
        var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(237, 238, 242) };
        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 212, 218) };
        _btnSave = new Button
        {
            Text      = "Save",
            Size      = new Size(90, 32),
            Location  = new Point(16, 10),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f)
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += OnSave;
        _btnCancel = new Button
        {
            Text         = "Cancel",
            Size         = new Size(80, 32),
            Location     = new Point(114, 10),
            FlatStyle    = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel,
            Font         = new Font("Segoe UI", 9.5f)
        };
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
        pnlBtns.Controls.AddRange(new Control[] { divider, _btnSave, _btnCancel });
        Controls.Add(pnlBtns);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void OnSave(object? s, EventArgs e)
    {
        var name = _txtName.Text.Trim();
        var path = _txtPath.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Please enter a template name.", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show("Please select or enter the template file path.", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPath.Focus();
            return;
        }
        if (!File.Exists(path))
        {
            var ans = MessageBox.Show(
                "The file was not found at the specified path:" + Environment.NewLine + path + Environment.NewLine + Environment.NewLine +
                "Save anyway?",
                "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ans != DialogResult.Yes) return;
        }

        Result = new WordTemplate { Name = name, Path = path };
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Label MakeLabel(string text) => new Label
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(50, 50, 50)
    };
}
