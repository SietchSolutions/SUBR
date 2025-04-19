using SUBR;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace SUBR
{
    static class Program
    {
        public static string DbPath;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "SUBR_MUTEX_UNIQUE_KEY", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("SUBR is already running.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SUBR"
                );

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                DbPath = Path.Combine(appDataPath, "subr_data.db");

                string connectionString = $"Data Source={DbPath};";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS YourTableName (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 🔥 Open SplashForm only if needed
                if (!ConfigHelper.GetSkipSplash())
                {
                    var splash = new SplashForm();
                    splash.ShowDialog();
                }

                // 🚀 Launch main app
              


                Application.Run(new MainForm());
            }
        }
    }
}
