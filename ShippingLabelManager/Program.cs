using ShippingLabelManager.Data;
using ShippingLabelManager.Forms;
using ShippingLabelManager.Services;

namespace ShippingLabelManager;

internal static class Program
{
    public static Database DB { get; private set; } = null!;
    public static string ConnectionString => AppSettings.ConnectionString;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppSettings.Load();

        DB = new Database(AppSettings.ConnectionString);

        if (!DB.TestConnection(out var err))
        {
            using var dlg = new ConnectionForm(AppSettings.ConnectionString);
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Cannot connect to database. Exiting.", "TRL",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            AppSettings.ConnectionString = dlg.ConnectionString;
            AppSettings.Save();
            DB = new Database(AppSettings.ConnectionString);
        }

        try { DB.InitializeDatabase(); }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Could not initialize database:\n\n" + ex.Message,
                "Database setup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm());
    }
}
