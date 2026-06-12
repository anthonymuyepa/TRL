using TRL.Data;

namespace TRL.Forms;

public class ConnectionForm : Form
{
    public string ConnectionString { get; private set; }

    private TextBox _txtServer = null!, _txtDatabase = null!;
    private CheckBox _chkIntegrated = null!;
    private TextBox _txtUser = null!, _txtPassword = null!;
    private Label _lblStatus = null!;
    private Button _btnTest = null!, _btnSave = null!, _btnCancel = null!;

    public ConnectionForm(string currentCs)
    {
        ConnectionString = currentCs;
        InitializeComponent();
        ParseConnectionString(currentCs);
    }

    private void InitializeComponent()
    {
        Text = "Database Connection";
        Size = new Size(480, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 10 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        layout.Controls.Add(new Label { Text = "SQL Server instance:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        _txtServer = new TextBox { Text = @".\SQLEXPRESS", Dock = DockStyle.Fill };
        layout.Controls.Add(_txtServer, 1, row++);

        layout.Controls.Add(new Label { Text = "Database:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        _txtDatabase = new TextBox { Text = "ShippingLabels", Dock = DockStyle.Fill };
        layout.Controls.Add(_txtDatabase, 1, row++);

        _chkIntegrated = new CheckBox { Text = "Windows Authentication (Integrated Security)", Checked = true, Dock = DockStyle.Fill };
        layout.SetColumnSpan(_chkIntegrated, 2);
        layout.Controls.Add(_chkIntegrated, 0, row++);
        _chkIntegrated.CheckedChanged += (s, e) => { _txtUser.Enabled = !_chkIntegrated.Checked; _txtPassword.Enabled = !_chkIntegrated.Checked; };

        layout.Controls.Add(new Label { Text = "Username:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        _txtUser = new TextBox { Dock = DockStyle.Fill, Enabled = false };
        layout.Controls.Add(_txtUser, 1, row++);

        layout.Controls.Add(new Label { Text = "Password:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        _txtPassword = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*', Enabled = false };
        layout.Controls.Add(_txtPassword, 1, row++);

        _lblStatus = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gray, Text = "Not tested yet." };
        layout.SetColumnSpan(_lblStatus, 2);
        layout.Controls.Add(_lblStatus, 0, row++);

        Controls.Add(layout);

        var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        _btnTest   = new Button { Text = "Test connection", Size = new Size(130, 34), Location = new Point(16, 8) };
        _btnSave   = new Button { Text = "Save & connect", Size = new Size(130, 34), Location = new Point(154, 8), BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnCancel = new Button { Text = "Cancel", Size = new Size(90, 34), Location = new Point(292, 8), DialogResult = DialogResult.Cancel };
        _btnTest.Click += OnTest;
        _btnSave.Click += OnSave;
        pnlBtns.Controls.AddRange(new Control[] { _btnTest, _btnSave, _btnCancel });
        Controls.Add(pnlBtns);
        CancelButton = _btnCancel;
    }

    private string BuildConnectionString()
    {
        var cs = $"Server={_txtServer.Text};Database={_txtDatabase.Text};TrustServerCertificate=true;";
        if (_chkIntegrated.Checked)
            cs += "Integrated Security=true;";
        else
            cs += $"User Id={_txtUser.Text};Password={_txtPassword.Text};";
        return cs;
    }

    private void ParseConnectionString(string cs)
    {
        try
        {
            var parts = cs.Split(';').Select(p => p.Trim()).Where(p => p.Length > 0)
                .Select(p => p.Split('=', 2)).Where(a => a.Length == 2)
                .ToDictionary(a => a[0].ToLower(), a => a[1]);

            if (parts.TryGetValue("server", out var sv)) _txtServer.Text = sv;
            if (parts.TryGetValue("database", out var db)) _txtDatabase.Text = db;
            var integrated = parts.ContainsKey("integrated security") && parts["integrated security"].ToLower() == "true";
            _chkIntegrated.Checked = integrated;
        }
        catch { }
    }

    private void OnTest(object? s, EventArgs e)
    {
        var cs = BuildConnectionString();
        var db = new Database(cs);
        if (db.TestConnection(out var err))
        {
            _lblStatus.ForeColor = Color.DarkGreen;
            _lblStatus.Text = "Connection successful!";
        }
        else
        {
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = "Failed: " + err;
        }
    }

    private void OnSave(object? s, EventArgs e)
    {
        var cs = BuildConnectionString();
        var db = new Database(cs);
        if (!db.TestConnection(out var err))
        {
            if (MessageBox.Show($"Connection failed:\n{err}\n\nSave anyway?", "Connection failed",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        }
        ConnectionString = cs;
        DialogResult = DialogResult.OK;
        Close();
    }
}
