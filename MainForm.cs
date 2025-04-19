using SUBR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace SUBR
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        private string commander = "Unknown";
        private string squadron = "Unknown";
        private string originalStationName = "";
        private DateTime lastSeen;
        private System.Windows.Forms.Timer gameTimeTimer;
        private bool firstLoadComplete = false;
        private Label lblStationName;
        private Label lblStationType;
        private Label lblStationOwner;
        private Label lblStationRequired;
        private Label lblStationDelivered;
        private Label lblStationCompletion;
        private Label lblLastDelivery;
        private Label lblCargoInTransit;
        private Label lblTrips400;
        private Label lblTrips784;
        public MainForm()
        {
            
            InitializeComponent();
            this.Load += MainForm_Load;
            this.cmbSystemDetails.SelectedIndexChanged += cmbSystemDetails_SelectedIndexChanged;

            Instance = this;
            this.txtEditSystemName.LostFocus += txtEditSystemName_LostFocus;
            Instance = this;
            this.txtEditSystemName.LostFocus += txtEditSystemName_LostFocus;
            SqliteHelper.InitializeDatabase();
            LoadAllSystems();
            if (cmbSelectSystem.Items.Count > 0)
                cmbSelectSystem.SelectedIndex = 0;
            txtSystemName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtSystemName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            SetupAutoComplete(txtSystemName);
            cmbSelectSystem.SelectedIndexChanged += cmbSelectSystem_SelectedIndexChanged;
            txtEditSystemName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtEditSystemName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            SetupAutoComplete(txtEditSystemName);
            var systemNames = SqliteHelper.GetAllSystemNames();
            var autoComplete = new AutoCompleteStringCollection();
            autoComplete.AddRange(systemNames.ToArray());
            txtSystemName.AutoCompleteCustomSource = autoComplete;
            var styleHelper = new ApplyCustomStyles();
            styleHelper.ApplyCustomStyle(this);
            cmbCurrentStationName.SelectedIndexChanged += cmbCurrentStationName_SelectedIndexChanged;
            RefreshAllPanels();
            LoadExistingSystems();
            cmbSelectExistingSystem.SelectedIndexChanged += cmbSelectExistingSystem_SelectedIndexChanged;
            cmbSelectExistingStation.SelectedIndexChanged += cmbSelectExistingStation_SelectedIndexChanged;
            
            LoadSystems();
           
            var (commanderName, shipName, squadronName, lastSeen) = CommanderHelper.GetCommanderInfo();
            pnlSystemDetails.Visible = true;
            flpCmdrDetails.Visible = false; // 🔥 hide it on startup
            btnCmdStation.Text = "Show Commander Details"; // 🔥 match the visible panel
            this.commander = commanderName;
            this.squadron = squadronName;
            cmbSystemDetails.SelectedIndexChanged += cmbSystemDetails_SelectedIndexChanged;
            // ADD THIS
            string stationName = "Example Station";
            string stationType = "Outpost";
            string owner = "Unknown";
            int required = 100;
            int delivered = 0;
            
            // END ADD

            var card = new StationCard(stationName, stationType, owner, required, delivered);

            // 🎯 WIRE EVENT
            card.OnStationCardClicked += StationCard_OnStationCardClicked;

            //flpStationCards.Controls.Add(card);
            SqliteHelper.UpsertCommander(commander, squadron);

            LoadTerminalLog();     // 🛑 Load existing logs BEFORE adding anything new

            AppendToTerminal($"CMDR {commander} of {squadron} connected."); // ✅ Only after logs loaded!
            this.Shown += MainForm_Shown;
            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();

                var verifyCmd = conn.CreateCommand();
                verifyCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";

                using (var reader = verifyCmd.ExecuteReader())
                {
                    Console.WriteLine("📋 Tables present in DB:");
                    while (reader.Read())
                    {
                        Console.WriteLine("• " + reader.GetString(0));
                    }
                }
            }
            if (commander == "Unknown")
            {
                // 🚫 Disable Create system fields
                txtSystemName.Enabled = false;
                cmbStationType.Enabled = false;
                txtStationName.Enabled = false;
                btnSubmit.Enabled = false;

                // 🚫 Optionally also disable edit panel if you want
                // btnEditSystem.Enabled = false;
                // btnEditStation.Enabled = false;

                // 🚨 Alert user
                MessageBox.Show("Commander name is Unknown. Please check your logConfig.json for correct log location before creating or editing systems.", "Access Denied");
                RefreshAllPanels();
            }
            LoadStationTypes();

            gameTimeTimer = new Timer();
            gameTimeTimer.Interval = 1000;
            gameTimeTimer.Tick += GameTimeTimer_Tick;
            gameTimeTimer.Start();

            (commander, _, squadron, lastSeen) = CommanderHelper.GetCommanderInfo();
            lblCommanderName.Text = $"CMDR: {commander}";
            lblSquadronName.Text = $"Squadron: {squadron}";
            // After everything is initialized
            this.ActiveControl = null;
            flpStations.FlowDirection = FlowDirection.TopDown;
            flpStations.WrapContents = false;
            flpStations.AutoScroll = true;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            pnlSystemDetails.Visible = true;
            

            var systems = SqliteHelper.GetAllSystemNames();
            cmbSystemDetails.Items.Clear();
            cmbSystemDetails.Items.AddRange(systems.ToArray());

            if (systems.Count > 0)
            {
                cmbSystemDetails.SelectedIndex = 0;
                BuildStationCards(systems[0]);
            }

            // 🛠 ADD THIS
            
        }

        private void LoadSystems()
        {
            var systems = SqliteHelper.GetAllSystems(); // ✅
            cmbSystemDetails.Items.Clear();
            foreach (var system in systems)
            {
                cmbSystemDetails.Items.Add(system);
            }
        }
        private void StationCard_OnStationCardClicked(StationCard clickedCard)
        {
            string selectedSystem = cmbSystemDetails.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedSystem) || clickedCard == null)
                return;

            flpStations.Visible = false;
            flpStationDetails.Visible = true;

            PopulateStationDetails(selectedSystem, clickedCard.StationName);
        }

        private void PopulateStationDetails(string systemName, string stationName)
        {
            flpStationDetails.Controls.Clear();

            var details = SqliteHelper.GetStationDetails(systemName, stationName);
            if (details == null)
                return;

            flpStationDetails.FlowDirection = FlowDirection.TopDown;
            flpStationDetails.WrapContents = false;
            flpStationDetails.AutoScroll = true;

            // 🔹 STATION NAME
            flpStationDetails.Controls.Add(new Label
            {
                Text = $"Station: {details.StationName}",
                Font = new Font("Consolas", 16, FontStyle.Bold),
                ForeColor = Color.DeepSkyBlue,
                AutoSize = true,
                Margin = new Padding(5, 5, 5, 10)
            });

            // 🔹 COMMANDER NAME
            flpStationDetails.Controls.Add(new Label
            {
                Text = $"Commander: {details.Owner}",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.Orange,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 10)
            });

            // 🔹 Overall Required and Delivered
            flpStationDetails.Controls.Add(CreateInfoLabel($"Total Tonnage Required: {details.RequiredMaterials} tons"));
            flpStationDetails.Controls.Add(CreateInfoLabel($"Total Tonnage Delivered: {details.DeliveredMaterials} tons"));
            flpStationDetails.Controls.Add(CreateInfoLabel($"Completion: {details.PercentComplete}%"));
            flpStationDetails.Controls.Add(CreateInfoLabel($"Last Delivery: {details.LastDelivery}"));

            // 🔹 Cargo In Transit
            flpStationDetails.Controls.Add(new Label
            {
                Text = $"Cargo In Transit: {details.CargoInTransit} tons",
                Font = new Font("Consolas", 12, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = true,
                Margin = new Padding(5, 10, 5, 10)
            });

            // 🔹 Overall Trips Panel
            var pnlTrips = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5, 5, 5, 15),
                Padding = new Padding(5)
            };
            pnlTrips.Controls.Add(new Label
            {
                Text = $"Trips (400 Capacity): {details.Trips400}",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                AutoSize = true
            });
            pnlTrips.Controls.Add(new Label
            {
                Text = $"Trips (784 Capacity): {details.Trips784}",
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                AutoSize = true
            });
            flpStationDetails.Controls.Add(pnlTrips);

            // 🔹 Materials Header
            flpStationDetails.Controls.Add(new Label
            {
                Text = "Required Materials:",
                Font = new Font("Consolas", 14, FontStyle.Bold),
                ForeColor = Color.DeepSkyBlue,
                AutoSize = true,
                Margin = new Padding(5, 10, 5, 5)
            });

            // 🔹 Material List
            var materials = SqliteHelper.GetStationMaterialBreakdown(systemName, stationName);
            foreach (var mat in materials)
            {
                int required = mat.Required;
                int delivered = mat.Delivered;
                int remaining = required - delivered;

                if (remaining <= 0)
                    continue;

                int trips400 = (int)Math.Ceiling(remaining / 400.0);
                int trips784 = (int)Math.Ceiling(remaining / 784.0);

                // Material Name (Big Bold Blue)
                flpStationDetails.Controls.Add(new Label
                {
                    Text = $"{mat.MaterialName}",
                    Font = new Font("Consolas", 12, FontStyle.Bold),
                    ForeColor = Color.DeepSkyBlue,
                    AutoSize = true,
                    Margin = new Padding(5, 10, 5, 0)
                });

                // Material Details
                flpStationDetails.Controls.Add(CreateInfoLabel($"• Required: {required} tons"));
                flpStationDetails.Controls.Add(CreateInfoLabel($"• Delivered: {delivered} tons"));
                flpStationDetails.Controls.Add(CreateInfoLabel($"• Remaining: {remaining} tons"));

                // Mini Trips Panel for this material
                var pnlMatTrips = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    BackColor = Color.FromArgb(50, 50, 50),
                    AutoSize = true,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(10, 0, 10, 10),
                    Padding = new Padding(5)
                };
                pnlMatTrips.Controls.Add(new Label
                {
                    Text = $"Trips (400 Capacity): {trips400}",
                    Font = new Font("Consolas", 10),
                    ForeColor = Color.White,
                    AutoSize = true
                });
                pnlMatTrips.Controls.Add(new Label
                {
                    Text = $"Trips (784 Capacity): {trips784}",
                    Font = new Font("Consolas", 10),
                    ForeColor = Color.White,
                    AutoSize = true
                });
                flpStationDetails.Controls.Add(pnlMatTrips);
            }
        }


        private Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Consolas", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(5, 2, 5, 2)
            };
        }

        private void cmbSystemDetails_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSystemDetails.SelectedItem == null)
                return;

            string selectedSystem = cmbSystemDetails.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedSystem))
                return;

            // ⚡ DO NOT touch Required Materials panel!!
            flpStations.Controls.Clear();
            flpStations.Visible = true;
            flpStationDetails.Visible = false;

            // ⚡ Build station cards for selected system
            var stationNames = SqliteHelper.GetStationsForSystem(selectedSystem);

            foreach (var stationName in stationNames)
            {
                string stationType = SqliteHelper.GetStationType(selectedSystem, stationName);
                string owner = SqliteHelper.GetSystemOwner(selectedSystem);
                int required = SqliteHelper.GetStationTotalRequired(selectedSystem, stationName);
                int delivered = SqliteHelper.GetStationTotalDelivered(selectedSystem, stationName);

                var card = new StationCard(stationName, stationType, owner, required, delivered);
                card.OnStationCardClicked += StationCard_OnStationCardClicked; // hook up click event

                flpStations.Controls.Add(card);
            }
        }



        private void txtEditSystemName_LostFocus(object sender, EventArgs e)
        {
            string systemName = txtEditSystemName.Text.Trim();
            if (string.IsNullOrEmpty(systemName))
                return;

            var stationNames = SqliteHelper.GetStationsForSystem(systemName);

            cmbCurrentStationName.Items.Clear();
            cmbCurrentStationName.Items.AddRange(stationNames.ToArray());

            if (stationNames.Count == 1)
            {
                cmbCurrentStationName.SelectedIndex = 0; // Auto-select if only one station
            }
        }


        private void cmbEditStationName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCurrentStationName.SelectedItem == null)
                return;

            string selectedStation = cmbCurrentStationName.SelectedItem.ToString();

            

           
        }















        private void BtnLoadTemplates_Click(object sender, EventArgs e)
        {
            SqliteHelper.ImportStationTemplatesFromCsv();
            AppendToTerminal("✅ Manually loaded station templates from CSV.");
            MessageBox.Show("Templates imported!", "Done");
            RefreshAllPanels();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.ActiveControl = null;  // ✅ Clear focus once the Form is really shown
        }
        private void GameTimeTimer_Tick(object sender, EventArgs e)
        {
            lblGameTime.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        }
        private void SetupPlaceholder(TextBox tb, string placeholder)
        {
            tb.Tag = placeholder;
            tb.Text = placeholder;
            tb.ForeColor = Color.Gray;

            tb.GotFocus += (s, e) =>
            {
                if (tb.Text == tb.Tag.ToString())
                {
                    tb.Text = "";
                    tb.ForeColor = Color.Lime; // or your active color
                }
            };

            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag.ToString();
                    tb.ForeColor = Color.Gray;
                }
            };
        }
        private void LoadTerminalLog()
        {
            terminalLive.Clear();
            var recentLogs = SqliteHelper.GetRecentLogs();

            foreach (var logEntry in recentLogs)
            {
                if (logEntry.Length < 20 || !logEntry.StartsWith("[2025-"))
                {
                    // If it doesn't have a timestamp, just add it raw
                    terminalLive.AppendText(logEntry + "\n");
                    continue;
                }

                try
                {
                    // Extract the timestamp part
                    string utcTimestampRaw = logEntry.Substring(1, 19); // "2025-04-18 15:30:04"
                    DateTime utcTime = DateTime.ParseExact(utcTimestampRaw, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    DateTime localTime = utcTime.ToLocalTime();

                    // Get the rest of the message
                    string restOfMessage = logEntry.Substring(21); // skip past [timestamp]

                    // Rebuild it
                    string rebuilt = $"[{utcTime:yyyy-MM-dd HH:mm:ss}] [{localTime:HH:mm}] {restOfMessage}";

                    terminalLive.AppendText(rebuilt + "\n");
                }
                catch
                {
                    // In case of any weird line format
                    terminalLive.AppendText(logEntry + "\n");
                }
            }

            terminalLive.ScrollToCaret();
        }


        private void LoadStationTypes()
        {
            cmbStationType.Items.Clear();

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT station_type FROM station_templates ORDER BY station_type;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cmbStationType.Items.Add(reader.GetString(0));
                    }
                }
            }

            if (cmbStationType.Items.Count > 0)
                cmbStationType.SelectedIndex = 0; // Optional: select first item
        }
        private void SetupAutoComplete(TextBox textBox)
        {

            textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var systemNames = SqliteHelper.GetAllSystemNames();
            var autoComplete = new AutoCompleteStringCollection();
            autoComplete.AddRange(systemNames.ToArray());
            textBox.AutoCompleteCustomSource = autoComplete;
        }








        // Inside AppendToTerminal
        public async void AppendToTerminal(string rawMessage)
        {
            string cleanedMessage = rawMessage.Trim();
            bool alreadyHasTimestamps = cleanedMessage.StartsWith("[2025-") || cleanedMessage.StartsWith("[2026-");

            string utcTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string localTimestamp = DateTime.Now.ToString("HH:mm");

            string fullLog;

            if (alreadyHasTimestamps)
            {
                fullLog = cleanedMessage; // if rawMessage is already formatted correctly
            }
            else
            {
                fullLog = $"[{utcTimestamp}] [{localTimestamp}] {cleanedMessage}";
            }

            terminalLive.AppendText(fullLog + Environment.NewLine);

            // 🛑 THIS IS THE CORRECT CALL:
            SqliteHelper.SaveLog(cleanedMessage);
            // NOT fullLog — you save the CLEAN MESSAGE ONLY without extra timestamps!!

            terminalLive.SelectionStart = terminalLive.Text.Length;
            terminalLive.ScrollToCaret();
        }




















        private void btnLoadCSV_Click(object sender, EventArgs e)
        {

        }

        private void SYSTEMSOPERATIONSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            {
                this.Hide(); // Hide the MainForm

                var systemOpsForm = new SystemOperations();
                systemOpsForm.FormClosed += (s, args) => this.Show(); // Show MainForm again when SystemOperations closes
                systemOpsForm.Show();
            }
        }

        private bool isEditMode = false; // Track if we're in Edit mode

        private void btnEditSystemStation_Click(object sender, EventArgs e)
        {
            pnlEditSystemandStation.Visible = true;
            pnlCreateSystemandStation.Visible = false;

            if (!isEditMode)
            {
                // Switch to Edit Mode
                pnlCreateSystemandStation.Visible = false;
                pnlEditSystemandStation.Visible = true;

                isEditMode = true;
            }
            else
            {
                // Switch back to Create Mode
                pnlEditSystemandStation.Visible = false;
                pnlCreateSystemandStation.Visible = true;

                isEditMode = false;
            }
        }

        private void btnSubmitEdit_Click(object sender, EventArgs e)
        {

        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            string systemName = txtSystemName.Text.Trim();
            string stationName = txtStationName.Text.Trim();
            string stationType = cmbStationType.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(systemName) ||
                string.IsNullOrWhiteSpace(stationName) ||
                string.IsNullOrWhiteSpace(stationType))
            {
                MessageBox.Show("Please fill in all fields before submitting.", "Missing Information");
                RefreshAllPanels();
                return;
            }

            // Check if system exists first
            if (!SqliteHelper.SystemExists(systemName))
            {
                SqliteHelper.CreateSystem(systemName);
            }

            // Check if station already exists under this system
            if (SqliteHelper.StationExists(systemName, stationName))
            {
                MessageBox.Show($"Station '{stationName}' already exists in system '{systemName}'.", "Duplicate Station");
                RefreshAllPanels();
                return;
            }

            // Create system, station, and requirements
            SqliteHelper.CreateSystemWithStationAndRequirements(systemName, stationName, stationType);
            string commanderName = CommanderHelper.GetCommanderName();
            AppendToTerminal($"✅ CMDR {commanderName} created new station '{stationName}' in system '{systemName}' as type '{stationType}'.");

            MessageBox.Show(
    $"✅ System and Station Created!\n\n" +
    $"System: {systemName}\n" +
    $"Station: {stationName}\n" +
    $"Type: {stationType}",
    "Success",
    MessageBoxButtons.OK,
    MessageBoxIcon.Information
);
            RefreshAllPanels();
            // Clear fields after successful creation
            txtSystemName.Clear();
            txtStationName.Clear();
            if (cmbStationType.Items.Count > 0)
                cmbStationType.SelectedIndex = 0;
            // Full UI Refresh After Creating System
            LoadAllSystems();
            RefreshAllPanels();
            SetupAutoComplete(txtSystemName);
            SetupAutoComplete(txtEditSystemName);

            cmbSelectSystem.SelectedIndex = cmbSelectSystem.Items.Count > 0 ? 0 : -1;
            cmbSelectExistingSystem.SelectedIndex = cmbSelectExistingSystem.Items.Count > 0 ? 0 : -1;
            LoadSystems(); // << reload the dropdowns immediately

        }
        private void LoadAllSystems()
        {
            cmbSelectSystem.Items.Clear();

            var systemList = SqliteHelper.GetAllSystems();

            

            cmbSelectSystem.Items.AddRange(systemList.ToArray());
            firstLoadComplete = true; // Mark that the first successful load has completed
        }
        public void FullUIRefresh()
        {
            RefreshAllPanels();
            LoadAllSystems();
            SetupAutoComplete(txtSystemName);
            SetupAutoComplete(txtEditSystemName);

            cmbSelectSystem.SelectedIndex = cmbSelectSystem.Items.Count > 0 ? 0 : -1;
            cmbSelectExistingSystem.SelectedIndex = cmbSelectExistingSystem.Items.Count > 0 ? 0 : -1;
        }


        private void cmbSelectSystem_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbCurrentStationName.Items.Clear();
            flpRequiredMaterials.Controls.Clear();

            if (cmbSelectSystem.SelectedItem == null)
                return;

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            var stationList = SqliteHelper.GetStationsForSystem(selectedSystem);

            if (stationList.Count == 0)
            {
                MessageBox.Show($"⚠️ No stations found for {selectedSystem}.", "No Stations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cmbCurrentStationName.Items.AddRange(stationList.ToArray());

            // Optional: auto-select first station
            if (cmbCurrentStationName.Items.Count > 0)
                cmbCurrentStationName.SelectedIndex = 0;
        }

        private void cmbCurrentStationName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCurrentStationName.SelectedItem == null || cmbSelectSystem.SelectedItem == null)
                return;

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            string selectedStation = cmbCurrentStationName.SelectedItem.ToString();

            PopulateResourceCardsForStation(selectedSystem, selectedStation);

            // 🛡️ Load Station Type (readonly)
            string stationType = SqliteHelper.GetStationType(selectedSystem, selectedStation);

            cmbCurrentStationType.Items.Clear();

            if (!string.IsNullOrEmpty(stationType))
            {
                cmbCurrentStationType.Items.Add(stationType);
                cmbCurrentStationType.SelectedIndex = 0;
            }
            else
            {
                cmbCurrentStationType.Items.Add("Unknown");
                cmbCurrentStationType.SelectedIndex = 0;
            }

            cmbCurrentStationType.Enabled = false; // 🛡️ Make sure it's READONLY
        }


        private void btnLoadSystemStationInfo_Click(object sender, EventArgs e)
        {
            if (cmbSelectSystem.SelectedItem == null || cmbCurrentStationName.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Please select both a System and Station first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            string selectedStation = cmbCurrentStationName.SelectedItem.ToString();

            // Populate edit fields
            txtEditSystemName.Text = selectedSystem;
            txtEditStationName.Text = selectedStation;
        }

        private void LoadStationTypesIntoCombo(ComboBox combo)
        {
            combo.Items.Clear();

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT station_type FROM station_templates ORDER BY station_type;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        combo.Items.Add(reader.GetString(0));
                    }
                }
            }
        }

        private void btnDeleteSystem_Click(object sender, EventArgs e)
        {
            if (cmbSelectSystem.SelectedItem == null)
            {
                
                MessageBox.Show("⚠️ Please select a System to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshAllPanels();
                return;
            }

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();

            var confirm = MessageBox.Show(
                $"⚠️ WARNING: Deleting system '{selectedSystem}' will also delete ALL associated stations and requirements.\n\nThis action CANNOT be undone.\n\nAre you absolutely sure?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            RefreshAllPanels();

            if (confirm != DialogResult.Yes)
                return;
            
            string owner = SqliteHelper.GetSystemOwner(selectedSystem);
            string currentCommander = CommanderHelper.GetCommanderName();

            if (!string.Equals(owner, currentCommander, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"⚠️ You do not have permission to modify system '{selectedSystem}'.\nOnly CMDR {owner} can edit or delete it.",
                                "Permission Denied",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                RefreshAllPanels();
                return;
            }

            // Do the deletion
            SqliteHelper.DeleteSystemAndStations(selectedSystem);

            MessageBox.Show($"✅ System '{selectedSystem}' deleted successfully.", "Deletion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAllPanels();
            string commanderName = CommanderHelper.GetCommanderName();
            AppendToTerminal($"⚠️ System '{selectedSystem}' and all associated stations were deleted by CMDR {commanderName}.");

            // Refresh system list and clear fields
            LoadAllSystems();
            cmbCurrentStationName.Items.Clear();
            cmbCurrentStationType.Items.Clear();
            txtEditSystemName.Clear();
            txtEditStationName.Clear();
           
        }

        private void btnEditSystem_Click(object sender, EventArgs e)
        {
            if (cmbSelectSystem.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Please select a System to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            string newSystemName = txtEditSystemName.Text.Trim();
            string owner = SqliteHelper.GetSystemOwner(selectedSystem);
            string currentCommander = CommanderHelper.GetCommanderName();

            // 1. Permission check
            if (!string.Equals(owner, currentCommander, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"⚠️ You do not have permission to edit system '{selectedSystem}'.\nOnly CMDR {owner} can edit it.",
                                "Permission Denied",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                RefreshAllPanels();
                return;
            }

            // 2. Validation
            if (string.IsNullOrEmpty(newSystemName))
            {
                MessageBox.Show("⚠️ Please enter a new System name.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshAllPanels();
                return;
            }

            if (string.Equals(selectedSystem, newSystemName, StringComparison.OrdinalIgnoreCase))
            {
                
                MessageBox.Show("⚠️ System name did not change.", "No Changes Detected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAllPanels();
                return;
            }

            // 3. Confirm rename
            var confirm = MessageBox.Show(
                $"Are you sure you want to rename system:\n\n'{selectedSystem}' ➔ '{newSystemName}'?",
                "Confirm System Rename",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);


            if (confirm != DialogResult.Yes)
                return;
            RefreshAllPanels();
            // 4. Proceed with update
            SqliteHelper.UpdateSystemName(selectedSystem, newSystemName);

            MessageBox.Show($"✅ System '{selectedSystem}' renamed to '{newSystemName}'.", "Rename Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAllPanels();
            // Log rename into terminal
            AppendToTerminal($"✏️ CMDR {currentCommander} renamed system '{selectedSystem}' to '{newSystemName}'.");

            // Refresh system list
            LoadAllSystems();
            cmbCurrentStationName.Items.Clear();
            cmbCurrentStationType.Items.Clear();
            txtEditSystemName.Clear();
            txtEditStationName.Clear();
            
        }
        private void RefreshAllPanels()
        {
            LoadAllSystems();

            cmbCurrentStationName.Items.Clear();
            cmbCurrentStationType.Items.Clear();
            txtEditSystemName.Clear();
            txtEditStationName.Clear();
           

            // You can also clear other fields if you want later
        }

        private void btnDeleteStation_Click(object sender, EventArgs e)
        {
            if (cmbSelectSystem.SelectedItem == null || cmbCurrentStationName.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Please select both a System and a Station to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            string selectedStation = cmbCurrentStationName.SelectedItem.ToString();
            string owner = SqliteHelper.GetSystemOwner(selectedSystem);
            string currentCommander = CommanderHelper.GetCommanderName();

            // 🛡️ Permission check
            if (!string.Equals(owner, currentCommander, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"⚠️ You do not have permission to delete stations from system '{selectedSystem}'.\nOnly CMDR {owner} can delete it.",
                                "Permission Denied",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // 🛡️ Confirm deletion
            var confirm = MessageBox.Show(
                $"⚠️ WARNING: Deleting station '{selectedStation}' will also delete all its material requirements.\n\nThis action CANNOT be undone.\n\nAre you absolutely sure?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            // 🛡️ Proceed with deletion
            SqliteHelper.DeleteStationAndRequirements(selectedSystem, selectedStation);

            MessageBox.Show($"✅ Station '{selectedStation}' deleted successfully.", "Deletion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 🛡️ Log to live terminal
            AppendToTerminal($"⚠️ CMDR {currentCommander} deleted station '{selectedStation}' from system '{selectedSystem}'.");

            // 🛡️ Refresh UI after delete
            RefreshAllPanels();
        }

        private void btnEditStation_Click(object sender, EventArgs e)
        {
            if (cmbSelectSystem.SelectedItem == null || cmbCurrentStationName.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Please select both a System and a Station to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSystem = cmbSelectSystem.SelectedItem.ToString();
            string oldStationName = cmbCurrentStationName.SelectedItem.ToString();
            string newStationName = txtEditStationName.Text.Trim();
           
            string owner = SqliteHelper.GetSystemOwner(selectedSystem);
            string currentCommander = CommanderHelper.GetCommanderName();

            // 🛡️ Permission check
            if (!string.Equals(owner, currentCommander, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"⚠️ You do not have permission to edit stations in system '{selectedSystem}'.\nOnly CMDR {owner} can edit it.",
                                "Permission Denied",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // 🛡️ Validation
            if (string.IsNullOrEmpty(newStationName))
            {
                MessageBox.Show("⚠️ Please enter a new Station name and select a Station type.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.Equals(oldStationName, newStationName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("⚠️ No changes detected. Station name and type are the same.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 🛡️ Confirm edit
            var confirm = MessageBox.Show(
    $"Are you sure you want to rename station:\n\n'{oldStationName}' ➔ '{newStationName}'?",
    "Confirm Station Rename",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            
            MessageBox.Show($"✅ Station '{oldStationName}' updated successfully.", "Edit Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 🛡️ Log to live terminal
            AppendToTerminal($"✏️ CMDR {currentCommander} updated station '{oldStationName}' ➔ '{newStationName}'.");

            // 🛡️ Refresh UI
            RefreshAllPanels();
        }
       public void PopulateResourceCardsForStation(string systemName, string stationName)
        {
            flpRequiredMaterials.Controls.Clear(); // Clear old cards
            Console.WriteLine("?? Clearing old ResourceCards...");

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT material_name, required_amount, delivered_amount
            FROM station_requirements
            INNER JOIN stations ON station_requirements.station_id = stations.station_id
            WHERE stations.station_name = @stationName
            AND stations.system_name = @systemName;
        ";
                cmd.Parameters.AddWithValue("@stationName", stationName);
                cmd.Parameters.AddWithValue("@systemName", systemName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string materialName = reader.GetString(0);
                        int required = reader.GetInt32(1);
                        int delivered = reader.GetInt32(2);
                        int remaining = required - delivered;

                        Console.WriteLine($"?? Loading card: {materialName}, Req: {required}, Del: {delivered}");

                        if (remaining > 0)
                        {
                            var card = new ResourceCard(materialName, required, delivered, remaining);
                            flpRequiredMaterials.Controls.Add(card);
                        }
                    }
                }
            }
        }


        private void LoadExistingSystems()
        {
            cmbSelectExistingSystem.Items.Clear();

            var systemList = SqliteHelper.GetAllSystems(); // Already exists!

            

            cmbSelectExistingSystem.Items.AddRange(systemList.ToArray());
        }
        private void cmbSelectExistingSystem_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbSelectExistingStation.Items.Clear();
            flpRequiredMaterials.Controls.Clear(); // clear cards

            if (cmbSelectExistingSystem.SelectedItem == null)
                return;

            string selectedSystem = cmbSelectExistingSystem.SelectedItem.ToString();
            var stationList = SqliteHelper.GetStationsForSystem(selectedSystem);

            if (stationList.Count == 0)
            {
                MessageBox.Show($"⚠️ No stations found for {selectedSystem}.", "No Stations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cmbSelectExistingStation.Items.AddRange(stationList.ToArray());

            // Optional: auto-select first station
            if (cmbSelectExistingStation.Items.Count > 0)
            {
                cmbSelectExistingStation.SelectedIndex = 0;
            }
        }


        private void cmbSelectExistingStation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSelectExistingSystem.SelectedItem == null || cmbSelectExistingStation.SelectedItem == null)
                return;

            string selectedSystem = cmbSelectExistingSystem.SelectedItem.ToString();
            string selectedStation = cmbSelectExistingStation.SelectedItem.ToString();

            PopulateResourceCardsForStation(selectedSystem, selectedStation); // ✅ THIS
        }
        public string GetSelectedSystem()
        {
            return cmbSelectExistingSystem.SelectedItem?.ToString() ?? "";
        }

        public string GetSelectedStation()
        {
            return cmbSelectExistingStation.SelectedItem?.ToString() ?? "";
        }

        private void btnCmdStation_Click(object sender, EventArgs e)
        {
            if (flpCmdrDetails.Visible)
            {
                flpCmdrDetails.Visible = false;
                pnlSystemDetails.Visible = true;
                btnCmdStation.Text = "Commander Info";
            }
            else
            {
                var commanderData = CommanderHelper.GetCommanderSummary(commander);
                BuildCommanderDetailPanel(commanderData.CommanderName);

                flpCmdrDetails.Visible = true;
                pnlSystemDetails.Visible = false;
                btnCmdStation.Text = "Station Info";
            }
        }











        private void BuildStationCards(string systemName)
        {
            flpStations.Controls.Clear(); // not flpStationDetails!

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT station_name, station_type, created_by,
                   (SELECT SUM(required_amount) FROM station_requirements WHERE station_id = stations.station_id),
                   (SELECT SUM(delivered_amount) FROM station_requirements WHERE station_id = stations.station_id)
            FROM stations
            WHERE system_name = @system
            ORDER BY station_name;";
                cmd.Parameters.AddWithValue("@system", systemName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string stationName = reader.GetString(0);
                        string stationType = reader.IsDBNull(1) ? "(Unknown)" : reader.GetString(1);
                        string owner = reader.IsDBNull(2) ? "(Unknown)" : reader.GetString(2);
                        int required = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        int delivered = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                        var card = new StationCard(stationName, stationType, owner, required, delivered);
                        card.OnStationCardClicked += StationCard_OnStationCardClicked;
                        flpStations.Controls.Add(card);
                    }
                }
            }
        }
        private void cmbSystemDetail_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSystemDetails.SelectedItem == null)
                return;

            string selectedSystem = cmbSystemDetails.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedSystem))
                return;

            flpStations.Controls.Clear();
            flpStations.Visible = true;
            flpStationDetails.Visible = false; // Hide details while showing station cards

            var stationNames = SqliteHelper.GetStationsForSystem(selectedSystem);

            foreach (var stationName in stationNames)
            {
                string stationType = SqliteHelper.GetStationType(selectedSystem, stationName);
                string owner = SqliteHelper.GetSystemOwner(selectedSystem);
                int required = SqliteHelper.GetStationTotalRequired(selectedSystem, stationName);
                int delivered = SqliteHelper.GetStationTotalDelivered(selectedSystem, stationName);

                var card = new StationCard(stationName, stationType, owner, required, delivered);
                card.OnStationCardClicked += StationCard_OnStationCardClicked; // wire up click
                flpStations.Controls.Add(card);
            }
        }
        private void BuildCommanderDetailPanel(string commanderName)
        {
            flpCmdrDetails.Controls.Clear();

            var summary = CommanderHelper.GetCommanderSummary(commanderName);
            if (summary == null)
                return;

            // Commander + Squadron
            var lblCommander = new Label
            {
                Text = $"Commander: {summary.CommanderName}",
                ForeColor = Color.OrangeRed,
                AutoSize = true,
                Font = new Font("Consolas", 14, FontStyle.Bold)
            };
            flpCmdrDetails.Controls.Add(lblCommander);

            var lblSquadron = new Label
            {
                Text = $"Squadron: {summary.SquadronName}",
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Consolas", 10)
            };
            flpCmdrDetails.Controls.Add(lblSquadron);

            var lblTotalDelivered = new Label
            {
                Text = $"Total Delivered: {summary.TotalDeliveredAllMaterials} tons",
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Consolas", 10)
            };
            flpCmdrDetails.Controls.Add(lblTotalDelivered);

            // 🔥 Cargo In Transit Section FIRST
            flpCmdrDetails.Controls.Add(new Label
            {
                Text = "Cargo In Transit:",
                ForeColor = Color.Orange,
                AutoSize = true,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 5)
            });

            var cargoRows = SqliteHelper.GetCargoInTransitForCommander(commanderName);

            foreach (var cargo in cargoRows)
            {
                string material = cargo.MaterialName;
                string station = cargo.StationName;
                int amount = cargo.AmountLoaded;

                int trips400 = (int)Math.Ceiling(amount / 400.0);
                int trips784 = (int)Math.Ceiling(amount / 784.0);

                var lblCargo = new Label
                {
                    Text = $"- {amount} units of {material} ➔ {station}",
                    ForeColor = Color.LightBlue,
                    AutoSize = true,
                    Font = new Font("Consolas", 10)
                };
                flpCmdrDetails.Controls.Add(lblCargo);

                var lblTripsCargo = new Label
                {
                    Text = $"  Trips (400 tons): {trips400} | Trips (784 tons): {trips784}",
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Font = new Font("Consolas", 9),
                    Margin = new Padding(20, 0, 0, 5)
                };
                flpCmdrDetails.Controls.Add(lblTripsCargo);
            }

            // 🛡️ Delivered Materials Section SECOND
            flpCmdrDetails.Controls.Add(new Label
            {
                Text = "Delivered Materials:",
                ForeColor = Color.Orange,
                AutoSize = true,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 5)
            });

            var deliveredRows = SqliteHelper.GetDeliveriesForCommander(commanderName);

            foreach (var delivery in deliveredRows)
            {
                string material = delivery.MaterialName;
                string station = delivery.StationName;
                int amount = delivery.AmountDelivered;

                int trips400 = (int)Math.Ceiling(amount / 400.0);
                int trips784 = (int)Math.Ceiling(amount / 784.0);

                var lblDelivered = new Label
                {
                    Text = $"- {amount} units of {material} ➔ {station}",
                    ForeColor = Color.White,
                    AutoSize = true,
                    Font = new Font("Consolas", 10)
                };
                flpCmdrDetails.Controls.Add(lblDelivered);

                var lblTripsDelivered = new Label
                {
                    Text = $"  Trips (400 tons): {trips400} | Trips (784 tons): {trips784}",
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Font = new Font("Consolas", 9),
                    Margin = new Padding(20, 0, 0, 5)
                };
                flpCmdrDetails.Controls.Add(lblTripsDelivered);
            }
        }
        private void txtEnterStationName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSubmit.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }













    }
}
