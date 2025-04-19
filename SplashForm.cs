using System;
using System.Drawing;
using System.Windows.Forms;

namespace SUBR
{
    public class SplashForm : Form
    {
        public SplashForm()
        {
            BuildSplash();
        }

        private void BuildSplash()
        {
            // Form settings
            this.Text = "S.U.B.R. Splash";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 800;
            this.Height = 500;
            this.MaximizeBox = false;

            // Draw border manually (no GroupBox)
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.Orange, 3))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, this.Width - 3, this.Height - 3));
                }
            };

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Black,
                Padding = new Padding(20),
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Subtitle
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Author
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // Logos
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Checkbox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Launch button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Link

            // Title
            var lblTitle = new Label
            {
                Text = "S.U.B.R.",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.OrangeRed,
            };

            // Subtitle
            var lblSubtitle = new Label
            {
                Text = "Supply Utilization and Bulk Reporting",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
            };

            // Author
            var lblAuthor = new Label
            {
                Text = "An Elite Dangerous Companion App\nBuilt by Bakka (Commander icyleft)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
            };

            // Logos
            var logosLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Black,
            };
            logosLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            logosLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            logosLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

            var logo1 = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                MaximumSize = new Size(150, 150),
                Margin = new Padding(10),
            };
            var logo2 = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                MaximumSize = new Size(150, 150),
                Margin = new Padding(10),
            };
            var logo3 = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                MaximumSize = new Size(150, 150),
                Margin = new Padding(10),
            };

            // TODO: Set your actual images
            logo1.Image = Properties.Resources.sietch;
            logo2.Image = Properties.Resources.powercasuals;
            logo3.Image = Properties.Resources.animatedEDgif;

            logosLayout.Controls.Add(logo1, 0, 0);
            logosLayout.Controls.Add(logo2, 1, 0);
            logosLayout.Controls.Add(logo3, 2, 0);

            // Checkbox
            var chkDontShow = new CheckBox
            {
                Text = "Don't show this screen again",
                Dock = DockStyle.None,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                BackColor = Color.Black,
            };

            // Launch button
            var btnLaunch = new Button
            {
                Text = "Launch",
                Dock = DockStyle.None,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Orange,
                ForeColor = Color.Black,
                Width = 120,
                Height = 40,
                Anchor = AnchorStyles.None
            };
            btnLaunch.Click += (s, e) => this.Close(); // Close the splash

            // Support Link
            var linkSupport = new LinkLabel
            {
                Text = "Support development: buymeacoffee.com/sietchsolutions",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.Orange,
                ActiveLinkColor = Color.Yellow,
                VisitedLinkColor = Color.Orange,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Black
            };
            linkSupport.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://buymeacoffee.com/sietchsolutions",
                UseShellExecute = true
            });

            // Add everything
            mainLayout.Controls.Add(lblTitle);
            mainLayout.Controls.Add(lblSubtitle);
            mainLayout.Controls.Add(lblAuthor);
            mainLayout.Controls.Add(logosLayout);
            mainLayout.Controls.Add(chkDontShow);
            mainLayout.Controls.Add(btnLaunch);
            mainLayout.Controls.Add(linkSupport);

            this.Controls.Add(mainLayout);
        }
    }
}
