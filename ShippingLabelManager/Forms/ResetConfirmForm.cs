namespace ShippingLabelManager.Forms;

public class ResetConfirmForm : Form
{
    private readonly string _scope;
    private TextBox _txtPhrase = null!, _txtPin = null!;
    private NumericUpDown _numStartAt = null!;
    private CheckBox _chkClearHistory = null!;
    private Label _lblError = null!;
    private Button _btnConfirm = null!, _btnCancel = null!;

    public int StartAt => (int)_numStartAt.Value;
    public bool ClearHistory => _chkClearHistory.Checked;

    public ResetConfirmForm(string scope)
    {
        _scope = scope;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Reset sequence — " + _scope;
        Size = new Size(440, 370);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 9
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Warning
        var lblWarning = new Label
        {
            Text = "Resetting: " + _scope,
            Dock = DockStyle.Fill,
            ForeColor = Color.DarkRed,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize = false,
            Height = 24
        };
        layout.Controls.Add(lblWarning, 0, row);
        layout.SetColumnSpan(lblWarning, 2);
        row++;

        var sep = new Panel { Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(200, 200, 200), Margin = new Padding(0, 4, 0, 8) };
        layout.Controls.Add(sep, 0, row);
        layout.SetColumnSpan(sep, 2);
        row++;

        // Start at
        AddLabel(layout, "Reset sequence to:", row);
        _numStartAt = new NumericUpDown
        {
            Minimum = 1, Maximum = 9999999, Value = 1,
            ThousandsSeparator = true, Dock = DockStyle.Fill
        };
        layout.Controls.Add(_numStartAt, 1, row);
        row++;

        // Hint
        var lblHint = new Label
        {
            Text = "Set to 1 for full reset, or any number to resume from a specific serial.",
            Dock = DockStyle.Fill,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = false,
            Height = 32
        };
        layout.Controls.Add(lblHint, 0, row);
        layout.SetColumnSpan(lblHint, 2);
        row++;

        // Clear history checkbox
        _chkClearHistory = new CheckBox
        {
            Text = "Also clear print history for this scope",
            Checked = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            Height = 28
        };
        layout.Controls.Add(_chkClearHistory, 0, row);
        layout.SetColumnSpan(_chkClearHistory, 2);
        row++;

        // Divider
        var sep2 = new Panel { Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(200, 200, 200), Margin = new Padding(0, 8, 0, 8) };
        layout.Controls.Add(sep2, 0, row);
        layout.SetColumnSpan(sep2, 2);
        row++;

        // Phrase
        AddLabel(layout, "Type RESET to confirm:", row);
        _txtPhrase = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_txtPhrase, 1, row);
        row++;

        // PIN
        AddLabel(layout, "Admin PIN:", row);
        _txtPin = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*' };
        layout.Controls.Add(_txtPin, 1, row);
        row++;

        // Error
        _lblError = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.DarkRed,
            AutoSize = false,
            Height = 22,
            Text = ""
        };
        layout.Controls.Add(_lblError, 0, row);
        layout.SetColumnSpan(_lblError, 2);

        Controls.Add(layout);

        // Buttons
        var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(16, 10, 16, 0) };
        _btnConfirm = new Button
        {
            Text = "Confirm reset",
            Size = new Size(130, 32),
            Location = new Point(16, 10),
            BackColor = Color.DarkRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(90, 32),
            Location = new Point(154, 10),
            DialogResult = DialogResult.Cancel
        };
        _btnConfirm.Click += OnConfirm;
        pnlBtns.Controls.AddRange(new Control[] { _btnConfirm, _btnCancel });
        Controls.Add(pnlBtns);
        CancelButton = _btnCancel;
    }

    private void OnConfirm(object? s, EventArgs e)
    {
        _lblError.Text = "";

        if (_txtPhrase.Text.Trim() != "RESET")
        {
            _lblError.Text = "You must type RESET exactly (uppercase).";
            _txtPhrase.Focus();
            return;
        }

        var pin = ShippingLabelManager.Services.AppSettings.AdminPin;
        if (string.IsNullOrEmpty(pin))
        {
            _lblError.Text = "No Admin PIN set. Go to File > Settings first.";
            return;
        }

        if (_txtPin.Text != pin)
        {
            _lblError.Text = "Incorrect Admin PIN.";
            _txtPin.Clear();
            _txtPin.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddLabel(TableLayoutPanel tbl, string text, int row)
    {
        tbl.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(60, 60, 60)
        }, 0, row);
    }
}
