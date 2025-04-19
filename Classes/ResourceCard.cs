using SUBR;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class ResourceCard : UserControl
{
    public string MaterialName { get; set; }
    public int Required { get; set; }
    public int Delivered { get; set; }
    public int Remaining { get; set; }

    public ResourceCard(string materialName, int required, int delivered, int remaining)
    {
        bool alreadyTransporting = SqliteHelper.IsCargoInTransit(
   CommanderHelper.GetCommanderName(),
   MainForm.Instance.GetSelectedSystem(),
   MainForm.Instance.GetSelectedStation(),
   MaterialName
);
        this.MaterialName = materialName;
        this.Required = required;
        this.Delivered = delivered;
        this.Remaining = remaining;
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        SetupCard();
       

    }

    private void SetupCard()
    {
        this.Width = 400;
        this.Height = 180;
        this.BackColor = Color.Transparent;

        // ➡️ Outer GroupBox
        var groupBox = new ColoredGroupBox
        {
            Text = "",
            BorderColor = Color.Orange,
            HeaderBackColor = Color.Black,
            HeaderForeColor = Color.Orange,
            BorderThickness = 2,
            CornerRadius = 8,
            Dock = DockStyle.Fill,
            Padding = new Padding(5),
            BackColor = Color.Transparent,
        };

        // ➡️ Material Name Label
        var lblMaterial = new Label
        {
            Text = MaterialName,
            AutoSize = false,
            Width = this.Width - 40,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Consolas", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 5, 0, 5)
        };

        // ➡️ Required / Delivered / Remaining Label
        var lblInfo = new Label
        {
            Text = $"Required: {Required} | Delivered: {Delivered} | Remaining: {Remaining}",
            AutoSize = false,
            Width = this.Width - 40,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Consolas", 10, FontStyle.Regular),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 5, 0, 5)
        };

        // ➡️ Load Buttons Panel
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(3),
            Margin = new Padding(0, 5, 0, 0),
            Anchor = AnchorStyles.None
        };

        var btnLoad400 = new Button
        {
            Text = "Load 400",
            Width = 80,
            Height = 30,
            BackColor = Color.DarkSlateGray,
            ForeColor = Color.White
        };
        btnLoad400.Click += (s, e) => LoadCargo(400);

        var btnLoad784 = new Button
        {
            Text = "Load 784",
            Width = 80,
            Height = 30,
            BackColor = Color.DarkSlateGray,
            ForeColor = Color.White
        };
        btnLoad784.Click += (s, e) => LoadCargo(784);

        var btnLoadCustom = new Button
        {
            Text = "Load Custom",
            Width = 100,
            Height = 30,
            BackColor = Color.DarkSlateGray,
            ForeColor = Color.White
        };
        btnLoadCustom.Click += (s, e) => LoadCustomCargo();

        buttonPanel.Controls.Add(btnLoad400);
        buttonPanel.Controls.Add(btnLoad784);
        buttonPanel.Controls.Add(btnLoadCustom);

        var centerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
        };
        centerPanel.Controls.Add(buttonPanel);
        centerPanel.Resize += (s, e) =>
        {
            buttonPanel.Left = (centerPanel.Width - buttonPanel.Width) / 2;
        };

        // ➡️ Unload Button Panel (hidden unless needed)
        var unloadPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
            Visible = false
        };

        var btnUnload = new Button
        {
            Text = "Unload",
            Width = 100,
            Height = 30,
            BackColor = Color.DarkGreen,
            ForeColor = Color.White
        };
        btnUnload.Click += (s, e) => UnloadCargo();
        unloadPanel.Controls.Add(btnUnload);
        unloadPanel.Resize += (s, e) =>
        {
            btnUnload.Left = (unloadPanel.Width - btnUnload.Width) / 2;
        };

        // ➡️ In Transit Label (hidden unless needed)
        var lblInTransit = new Label
        {
            Text = $"✅ {CommanderHelper.GetCommanderName()} in transit",
            AutoSize = false,
            Height = 30,
            Dock = DockStyle.Top,
            ForeColor = Color.Lime,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        // ➡️ Stack them correctly:
        groupBox.Controls.Add(lblInTransit); // very bottom
        groupBox.Controls.Add(unloadPanel);  // then unload
        groupBox.Controls.Add(centerPanel);  // then Load buttons
        groupBox.Controls.Add(lblInfo);       // requirements
        groupBox.Controls.Add(lblMaterial);   // top

        // ➡️ Add GroupBox to Card
        this.Controls.Add(groupBox);

        // ➡️ Check if already in transit
        bool alreadyInTransit = SqliteHelper.IsCargoInTransit(
            CommanderHelper.GetCommanderName(),
            MainForm.Instance.GetSelectedSystem(),
            MainForm.Instance.GetSelectedStation(),
            MaterialName
        );

        if (alreadyInTransit)
        {
            unloadPanel.Visible = true;
            lblInTransit.Visible = true;
            centerPanel.Enabled = false;
            groupBox.BackColor = Color.Teal;
        }
    }


    private void UnloadCargo()
    {
        string commanderName = CommanderHelper.GetCommanderName();
        string systemName = MainForm.Instance.GetSelectedSystem();
        string stationName = MainForm.Instance.GetSelectedStation();

        int amountInTransit = SqliteHelper.GetCargoInTransitAmount(commanderName, systemName, stationName, MaterialName);

        if (amountInTransit <= 0)
        {
            MessageBox.Show("⚠️ No cargo in transit found for this material.", "Unload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Confirm delivery of {amountInTransit} units of {MaterialName} to {stationName}?",
            "Confirm Delivery",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        // Update all relevant tables
        SqliteHelper.DeleteCargoInTransit(commanderName, systemName, stationName, MaterialName);
        SqliteHelper.InsertDelivery(commanderName, systemName, stationName, MaterialName, amountInTransit);
        SqliteHelper.UpdateStationDelivery(systemName, stationName, MaterialName, amountInTransit);
        SqliteHelper.UpdateCommanderMaterialTotals(commanderName, MaterialName, amountInTransit); // ✅ ADD THIS BACK

        // 🔥 Update memory variables
        Delivered += amountInTransit;
        Remaining = Required - Delivered;

        if (Remaining <= 0)
        {
            this.Visible = false; // fully delivered
        }
        else
        {
            this.Controls.Clear();
            SetupCard(); // rebuild the card with new numbers
        }

        MainForm.Instance.PopulateResourceCardsForStation(MainForm.Instance.GetSelectedSystem(), MainForm.Instance.GetSelectedStation());
        MainForm.Instance.AppendToTerminal($"📦 CMDR {commanderName} delivered {amountInTransit} {MaterialName} to {stationName} in {systemName}.");
    }





    private async void LoadCargo(int requestedAmount)
    {
        int loadAmount = Math.Min(Remaining, requestedAmount);
        if (loadAmount <= 0)
        {
            MessageBox.Show("No materials left to load for this resource.", "Nothing to Load", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string commanderName = CommanderHelper.GetCommanderName();
        string systemName = MainForm.Instance.GetSelectedSystem();
        string stationName = MainForm.Instance.GetSelectedStation();

        // Confirm
        var confirm = MessageBox.Show(
            $"Are you sure you want to load {loadAmount} units of {MaterialName} bound for {stationName} in {systemName}?",
            "Confirm Load",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        // Insert into cargo_in_transit
        using (var conn = SqliteHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO cargo_in_transit (commander_name, system_name, station_name, material_name, amount_loaded, timestamp, status)
            VALUES (@commander, @system, @station, @material, @amount, @timestamp, @status);";

            cmd.Parameters.AddWithValue("@commander", commanderName);
            cmd.Parameters.AddWithValue("@system", systemName);
            cmd.Parameters.AddWithValue("@station", stationName);
            cmd.Parameters.AddWithValue("@material", MaterialName);
            cmd.Parameters.AddWithValue("@amount", loadAmount);
            cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@status", "In Transit");

            cmd.ExecuteNonQuery();
        }

        // 🛑 INSERT FINISHED — NOW WAIT FOR DISK TO CATCH UP
        await Task.Delay(400);

        // ✅ Now refresh
        MainForm.Instance.FullUIRefresh();

        // ✅ Log to terminal
        MainForm.Instance.AppendToTerminal($"✈️ CMDR {commanderName} loaded {loadAmount} units of {MaterialName} for {stationName} in {systemName}.");
    }




    private void LoadCustomCargo()
    {
        string input = PromptCustomAmount();
        if (int.TryParse(input, out int customAmount))
        {
            if (customAmount <= 0)
            {
                MessageBox.Show("Invalid amount.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadCargo(customAmount);
        }
        else
        {
            MessageBox.Show("Invalid input. Please enter a number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    

    private string PromptCustomAmount()
    {
        Form prompt = new Form()
        {
            Width = 300,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = "Load Custom Amount",
            StartPosition = FormStartPosition.CenterScreen
        };
        Label textLabel = new Label() { Left = 10, Top = 10, Text = "Enter custom amount:" };
        TextBox textBox = new TextBox() { Left = 10, Top = 40, Width = 260 };
        Button confirmation = new Button() { Text = "OK", Left = 100, Width = 100, Top = 70 };
        confirmation.DialogResult = DialogResult.OK;
        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
    }
}
