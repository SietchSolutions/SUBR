
using SUBR;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

namespace SUBR
{
    public static class SqliteHelper
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subr_data.db");
        private static readonly string ConnectionString = $"Data Source={DbPath};";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                Console.WriteLine("🆕 Creating SQLite database...");
            }

            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS systems (
            system_id INTEGER PRIMARY KEY AUTOINCREMENT,
            system_name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS stations (
            station_id INTEGER PRIMARY KEY AUTOINCREMENT,
            station_name TEXT NOT NULL,
            system_name TEXT NOT NULL,
            station_type TEXT,
            created_by TEXT,
            created_at TEXT
        );

        CREATE TABLE IF NOT EXISTS station_templates (
    template_id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_type TEXT NOT NULL,
    material_name TEXT NOT NULL,
    material_required INTEGER NOT NULL
);

        CREATE TABLE IF NOT EXISTS station_requirements (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            station_id INTEGER,
            material_name TEXT NOT NULL,
            required_amount INTEGER DEFAULT 0,
            delivered_amount INTEGER DEFAULT 0
        );

       CREATE TABLE IF NOT EXISTS commanders (
    commander_id INTEGER PRIMARY KEY AUTOINCREMENT,
    commander_name TEXT NOT NULL UNIQUE,
    squadron_name TEXT,
    total_all_materials INTEGER DEFAULT 0,
    last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,

    Aluminium INTEGER DEFAULT 0,
    Agri_Medicines INTEGER DEFAULT 0,
    Advanced_Catalysers INTEGER DEFAULT 0,
    Animal_Meat INTEGER DEFAULT 0,
    Basic_Medicines INTEGER DEFAULT 0,
    Battle_Weapons INTEGER DEFAULT 0,
    Beer INTEGER DEFAULT 0,
    Bioreducing_Lichen INTEGER DEFAULT 0,
    Biowaste INTEGER DEFAULT 0,
    Building_Fabricators INTEGER DEFAULT 0,
    CMM_Composite INTEGER DEFAULT 0,
    Ceramic_Composites INTEGER DEFAULT 0,
    Coffee INTEGER DEFAULT 0,
    Combat_Stabilizers INTEGER DEFAULT 0,
    Computer_Components INTEGER DEFAULT 0,
    Copper INTEGER DEFAULT 0,
    Crop_Harvesters INTEGER DEFAULT 0,
    Emergency_Power_Cells INTEGER DEFAULT 0,
    Evacuation_Shelter INTEGER DEFAULT 0,
    Fish INTEGER DEFAULT 0,
    Food_Cartridges INTEGER DEFAULT 0,
    Fruit_and_Vegetables INTEGER DEFAULT 0,
    Geological_Equipment INTEGER DEFAULT 0,
    Grain INTEGER DEFAULT 0,
    H_E_Suits INTEGER DEFAULT 0,
    Insulating_Membrane INTEGER DEFAULT 0,
    Land_Enrichment_Systems INTEGER DEFAULT 0,
    Liquid_Oxygen INTEGER DEFAULT 0,
    Liquor INTEGER DEFAULT 0,
    Medical_Diagnostic_Equipment INTEGER DEFAULT 0,
    Micro_Controllers INTEGER DEFAULT 0,
    Microbial_Furnaces INTEGER DEFAULT 0,
    Military_Grade_Fabrics INTEGER DEFAULT 0,
    Mineral_Extractors INTEGER DEFAULT 0,
    Muon_Imager INTEGER DEFAULT 0,
    Non_Lethal_Weapons INTEGER DEFAULT 0,
    Pesticides INTEGER DEFAULT 0,
    Polymers INTEGER DEFAULT 0,
    Power_Generators INTEGER DEFAULT 0,
    Reactive_Armour INTEGER DEFAULT 0,
    Resonating_Separators INTEGER DEFAULT 0,
    Robotics INTEGER DEFAULT 0,
    Semiconductors INTEGER DEFAULT 0,
    Steel INTEGER DEFAULT 0,
    Structural_Regulators INTEGER DEFAULT 0,
    Superconductors INTEGER DEFAULT 0,
    Surface_Stabilisers INTEGER DEFAULT 0,
    Survival_Equipment INTEGER DEFAULT 0,
    Tea INTEGER DEFAULT 0,
    Thermal_Cooling_Units INTEGER DEFAULT 0,
    Titanium INTEGER DEFAULT 0,
    Water INTEGER DEFAULT 0,
    Water_Purifiers INTEGER DEFAULT 0,
    Wine INTEGER DEFAULT 0
);
 CREATE TABLE IF NOT EXISTS cargo_in_transit (
    cargo_id INTEGER PRIMARY KEY AUTOINCREMENT,
    commander_name TEXT NOT NULL,
    system_name TEXT NOT NULL,
    station_name TEXT NOT NULL,
    material_name TEXT NOT NULL,
    amount_loaded INTEGER NOT NULL,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    status TEXT DEFAULT 'in_transit'
);       
CREATE TABLE IF NOT EXISTS logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    message TEXT NOT NULL
);
        CREATE TABLE IF NOT EXISTS deliveries (
            delivery_id INTEGER PRIMARY KEY AUTOINCREMENT,
            commander_name TEXT NOT NULL,
            material_name TEXT NOT NULL,
            amount_delivered INTEGER NOT NULL,
            system_name TEXT,
            station_name TEXT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        ";

                try
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("✅ Tables created/verified.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Table creation failed: " + ex.Message);
                }

                var verify = conn.CreateCommand();
                verify.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using (var reader = verify.ExecuteReader())
                {
                    Console.WriteLine("📋 Tables present:");
                    while (reader.Read())
                        Console.WriteLine("• " + reader.GetString(0));
                }

                conn.Close();
            }

            // ✅ Only do this once when DB is freshly created
            ImportStationTemplatesFromCsv();
        }


        public static void SaveLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            // Remove any existing timestamp-looking strings from message
            if (message.StartsWith("[2025-") || message.StartsWith("[2026-"))
            {
                // Remove everything up to first "]"
                int firstClose = message.IndexOf("]");
                if (firstClose >= 0 && firstClose + 1 < message.Length)
                {
                    message = message.Substring(firstClose + 1).Trim();
                }
            }

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO logs (timestamp, message) VALUES (@time, @msg);";
                cmd.Parameters.AddWithValue("@time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@msg", message); // ✅ Cleaned message
                cmd.ExecuteNonQuery();
            }
        }

        public static List<string> GetRecentLogs(int minutesBack = 60)
        {
            var logs = new List<string>();
            var since = DateTime.UtcNow.AddMinutes(-minutesBack).ToString("yyyy-MM-dd HH:mm:ss");

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT timestamp, message FROM logs WHERE timestamp >= @since ORDER BY timestamp ASC;";
                cmd.Parameters.AddWithValue("@since", since);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ts = reader.GetString(0);
                        string msg = reader.GetString(1);
                        logs.Add($"[{ts}] {msg}");
                    }
                }
            }

            return logs;
        }
        public static SqliteConnection GetConnection() // ✅ Microsoft.Data.Sqlite
        {
            return new SqliteConnection(ConnectionString);
        }
        public static void CreateSystemWithStationAndRequirements(string systemName, string stationName, string stationType)
        {
            string commanderName = CommanderHelper.GetCommanderName();
            string createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Ensure system exists
                    var insertSystem = conn.CreateCommand();
                    insertSystem.CommandText = @"
                INSERT OR IGNORE INTO systems (system_name) 
                VALUES ($system);";
                    insertSystem.Parameters.AddWithValue("$system", systemName);
                    insertSystem.ExecuteNonQuery();

                    // 2. Insert station
                    var insertStation = conn.CreateCommand();
                    insertStation.CommandText = @"
                INSERT INTO stations (system_name, station_name, station_type, created_by, created_at) 
                VALUES ($system, $station, $type, $createdBy, $createdAt);";
                    insertStation.Parameters.AddWithValue("$system", systemName);
                    insertStation.Parameters.AddWithValue("$station", stationName);
                    insertStation.Parameters.AddWithValue("$type", stationType);
                    insertStation.Parameters.AddWithValue("$createdBy", commanderName);
                    insertStation.Parameters.AddWithValue("$createdAt", createdAt);
                    insertStation.ExecuteNonQuery();

                    // 3. Get the inserted station_id
                    long stationId = -1;
                    var getStationId = conn.CreateCommand();
                    getStationId.CommandText = "SELECT last_insert_rowid();";
                    stationId = (long)(getStationId.ExecuteScalar() ?? -1);

                    // 4. Load template for station type
                    var loadTemplate = conn.CreateCommand();
                    loadTemplate.CommandText = @"
                SELECT material_name, material_required 
                FROM station_templates 
                WHERE station_type = $type;";
                    loadTemplate.Parameters.AddWithValue("$type", stationType);

                    using (var reader = loadTemplate.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string materialName = reader.GetString(0);
                            int requiredAmount = reader.GetInt32(1);

                            // 5. Insert into station_requirements
                            var insertRequirement = conn.CreateCommand();
                            insertRequirement.CommandText = @"
    INSERT INTO station_requirements
    (station_id, material_name, required_amount, delivered_amount) 
    VALUES ($stationId, $material, $required, 0);"; // ✅ correct
                            insertRequirement.Parameters.AddWithValue("$stationId", stationId); // ✅ correct

                            insertRequirement.Parameters.AddWithValue("$type", stationType);
                            insertRequirement.Parameters.AddWithValue("$material", materialName);
                            insertRequirement.Parameters.AddWithValue("$required", requiredAmount);
                            insertRequirement.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                }
            }
        }
        public static bool StationExists(string systemName, string stationName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM stations WHERE system_name = $system AND station_name = $station;";
                cmd.Parameters.AddWithValue("$system", systemName);
                cmd.Parameters.AddWithValue("$station", stationName);
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public static void UpdateStation(string oldStationName, string newStationName, string newStationType)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            UPDATE stations 
            SET station_name = @newName, station_type = @newType 
            WHERE station_name = @oldName;
        ";

                cmd.Parameters.AddWithValue("@newName", newStationName);
                cmd.Parameters.AddWithValue("@newType", newStationType);
                cmd.Parameters.AddWithValue("@oldName", oldStationName);

                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateStationNameAndType(string systemName, string oldStationName, string newStationName, string newType)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            UPDATE stations
            SET station_name = @newName,
                station_type = @newType
            WHERE system_name = @systemName
              AND station_name = @oldName;";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@oldName", oldStationName);
                cmd.Parameters.AddWithValue("@newName", newStationName);
                cmd.Parameters.AddWithValue("@newType", newType);
                cmd.ExecuteNonQuery();
            }
        }


        public static List<string> GetAllSystemNames()
        {
            var systems = new List<string>();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT system_name FROM stations ORDER BY system_name ASC;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        systems.Add(reader["system_name"].ToString());
                    }
                }
            }

            return systems;
        }
        public static List<string> GetAllSystems()
        {
            var systems = new List<string>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT DISTINCT system_name FROM systems ORDER BY system_name", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        systems.Add(reader.GetString(0));
                }
            }
            return systems;
        }
        public static List<string> GetStationsForSystem(string systemName)
        {
            var stations = new List<string>();

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT station_name FROM stations WHERE system_name = @systemName;";
                    cmd.Parameters.AddWithValue("@systemName", systemName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stations.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return stations;
        }
        




        


       


        
        

        public static void ImportStationTemplatesFromCsv()
        {
            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "station_templates.csv");

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"❌ station_templates.csv not found at: {csvPath}");
                return;
            }

            using (var conn = GetConnection())
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                using (var reader = new StreamReader(csvPath))
                {
                    bool headerSkipped = false;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!headerSkipped)
                        {
                            headerSkipped = true;
                            continue; // skip header
                        }

                        var parts = line.Split(',');
                        if (parts.Length < 4) continue;

                        if (!int.TryParse(parts[0].Trim(), out int id)) continue;
                        string type = parts[1].Trim();
                        string material = parts[2].Trim();
                        if (!int.TryParse(parts[3].Trim(), out int required)) continue;

                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                    INSERT OR IGNORE INTO station_templates 
                    (template_id, station_type, material_name, material_required) 
                    VALUES ($id, $type, $material, $required)";
                        cmd.Parameters.AddWithValue("$id", id);
                        cmd.Parameters.AddWithValue("$type", type);
                        cmd.Parameters.AddWithValue("$material", material);
                        cmd.Parameters.AddWithValue("$required", required);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            Console.WriteLine("✅ station_templates loaded from CSV.");
        }
        public static void UpsertCommander(string name, string squadron)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Try update first
                var update = conn.CreateCommand();
                update.CommandText = @"
            UPDATE commanders 
            SET squadron_name = $squadron, last_seen = CURRENT_TIMESTAMP 
            WHERE commander_name = $name;";
                update.Parameters.AddWithValue("$name", name);
                update.Parameters.AddWithValue("$squadron", squadron);
                int rows = update.ExecuteNonQuery();

                // If no rows updated, insert
                if (rows == 0)
                {
                    var insert = conn.CreateCommand();
                    insert.CommandText = @"
                INSERT INTO commanders (commander_name, squadron_name, last_seen)
                VALUES ($name, $squadron, CURRENT_TIMESTAMP);";
                    insert.Parameters.AddWithValue("$name", name);
                    insert.Parameters.AddWithValue("$squadron", squadron);
                    insert.ExecuteNonQuery();
                }
            }
        }
        public static bool SystemExists(string systemName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM systems WHERE system_name = @name;";
                cmd.Parameters.AddWithValue("@name", systemName);

                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }
        public static void CreateSystem(string systemName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            INSERT OR IGNORE INTO systems (system_name)
            VALUES ($system);";

                cmd.Parameters.AddWithValue("$system", systemName);
                cmd.ExecuteNonQuery();

            }
        }
        public static void CreateStation(string systemName, string stationName, string stationType)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO stations (station_name, system_name, station_type, created_by, created_at)
            VALUES ($station, $system, $type, $createdBy, $createdAt);";
                cmd.Parameters.AddWithValue("$station", stationName);
                cmd.Parameters.AddWithValue("$system", systemName);
                
                cmd.Parameters.AddWithValue("$type", stationType);
                cmd.Parameters.AddWithValue("$createdBy", "System"); // you can pass commander later if you want
                cmd.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
        }

        public static string GetStationType(string systemName, string stationName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT station_type
            FROM stations
            WHERE system_name = @systemName
              AND station_name = @stationName
            LIMIT 1;
        ";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@stationName", stationName);

                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }

        public static void DeleteSystemAndStations(string systemName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Get station IDs linked to the system
                    var getStationIds = conn.CreateCommand();
                    getStationIds.CommandText = @"
                SELECT station_id 
                FROM stations 
                WHERE system_name = @systemName;";
                    getStationIds.Parameters.AddWithValue("@systemName", systemName);

                    var stationIds = new List<long>();
                    using (var reader = getStationIds.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stationIds.Add(reader.GetInt64(0));
                        }
                    }

                    // 2. Delete related station_requirements
                    foreach (var stationId in stationIds)
                    {
                        var deleteRequirements = conn.CreateCommand();
                        deleteRequirements.CommandText = "DELETE FROM station_requirements WHERE station_id = @stationId;";
                        deleteRequirements.Parameters.AddWithValue("@stationId", stationId);
                        deleteRequirements.ExecuteNonQuery();
                    }

                    // 3. Delete stations
                    var deleteStations = conn.CreateCommand();
                    deleteStations.CommandText = "DELETE FROM stations WHERE system_name = @systemName;";
                    deleteStations.Parameters.AddWithValue("@systemName", systemName);
                    deleteStations.ExecuteNonQuery();

                    // 4. Delete system
                    var deleteSystem = conn.CreateCommand();
                    deleteSystem.CommandText = "DELETE FROM systems WHERE system_name = @systemName;";
                    deleteSystem.Parameters.AddWithValue("@systemName", systemName);
                    deleteSystem.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
        public static string GetSystemOwner(string systemName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT stations.created_by
            FROM stations
            WHERE system_name = @systemName
            LIMIT 1;";
                cmd.Parameters.AddWithValue("@systemName", systemName);

                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }
        public static void UpdateSystemName(string oldSystemName, string newSystemName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Update systems table
                    var updateSystem = conn.CreateCommand();
                    updateSystem.CommandText = @"
                UPDATE systems
                SET system_name = @newName
                WHERE system_name = @oldName;";
                    updateSystem.Parameters.AddWithValue("@newName", newSystemName);
                    updateSystem.Parameters.AddWithValue("@oldName", oldSystemName);
                    updateSystem.ExecuteNonQuery();

                    // 2. Update stations table
                    var updateStations = conn.CreateCommand();
                    updateStations.CommandText = @"
                UPDATE stations
                SET system_name = @newName
                WHERE system_name = @oldName;";
                    updateStations.Parameters.AddWithValue("@newName", newSystemName);
                    updateStations.Parameters.AddWithValue("@oldName", oldSystemName);
                    updateStations.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
        public static void DeleteStationAndRequirements(string systemName, string stationName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // 1. Find station ID
                    var getStationId = conn.CreateCommand();
                    getStationId.CommandText = @"
                SELECT station_id 
                FROM stations 
                WHERE system_name = @systemName
                  AND station_name = @stationName
                LIMIT 1;";
                    getStationId.Parameters.AddWithValue("@systemName", systemName);
                    getStationId.Parameters.AddWithValue("@stationName", stationName);

                    var stationIdObj = getStationId.ExecuteScalar();
                    if (stationIdObj != null)
                    {
                        long stationId = (long)stationIdObj;

                        // 2. Delete material requirements linked to that station
                        var deleteRequirements = conn.CreateCommand();
                        deleteRequirements.CommandText = "DELETE FROM station_requirements WHERE station_id = @stationId;";
                        deleteRequirements.Parameters.AddWithValue("@stationId", stationId);
                        deleteRequirements.ExecuteNonQuery();
                    }

                    // 3. Delete the station itself
                    var deleteStation = conn.CreateCommand();
                    deleteStation.CommandText = @"
                DELETE FROM stations 
                WHERE system_name = @systemName
                  AND station_name = @stationName;";
                    deleteStation.Parameters.AddWithValue("@systemName", systemName);
                    deleteStation.Parameters.AddWithValue("@stationName", stationName);
                    deleteStation.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
        public static void LogCargoInTransit(string commanderName, string systemName, string stationName, string materialName, int amount)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO cargo_in_transit (commander_name, system_name, station_name, material_name, amount_loaded, status)
            VALUES (@commander, @system, @station, @material, @amount, 'in_transit');";
                cmd.Parameters.AddWithValue("@commander", commanderName);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName);
                cmd.Parameters.AddWithValue("@amount", amount);

                cmd.ExecuteNonQuery();
            }
        }
       
        public static int GetCargoInTransitAmount(string commanderName, string systemName, string stationName, string materialName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT amount_loaded 
            FROM cargo_in_transit 
            WHERE commander_name = @commander 
              AND system_name = @system 
              AND station_name = @station 
              AND material_name = @material
              LIMIT 1;";

                cmd.Parameters.AddWithValue("@commander", commanderName);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName);

                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
        public static void DeleteCargoInTransit(string commanderName, string systemName, string stationName, string materialName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            DELETE FROM cargo_in_transit 
            WHERE commander_name = @commander 
              AND system_name = @system 
              AND station_name = @station 
              AND material_name = @material;";

                cmd.Parameters.AddWithValue("@commander", commanderName);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName);

                cmd.ExecuteNonQuery();
            }
        }
        public static void UpdateDeliveredAmount(string systemName, string stationName, string materialName, int amountDelivered)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            UPDATE station_requirements
            SET delivered_amount = delivered_amount + @amount
            WHERE station_id = (
                SELECT station_id FROM stations
                WHERE system_name = @system AND station_name = @station
            )
              AND material_name = @material;";

                cmd.Parameters.AddWithValue("@amount", amountDelivered);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName);

                cmd.ExecuteNonQuery();
            }
        }
        public static void InsertDelivery(string commanderName, string systemName, string stationName, string materialName, int amountDelivered)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO deliveries (commander_name, material_name, amount_delivered, system_name, station_name, timestamp)
            VALUES (@commander, @material, @amount, @system, @station, @timestamp);";

                cmd.Parameters.AddWithValue("@commander", commanderName);
                cmd.Parameters.AddWithValue("@material", materialName);
                cmd.Parameters.AddWithValue("@amount", amountDelivered);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")); // Store UTC like everything else

                cmd.ExecuteNonQuery();
            }
        }
        public static bool IsCargoInTransit(string commanderName, string systemName, string stationName, string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return false; // Nothing to check if material is blank

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT COUNT(*) FROM cargo_in_transit
            WHERE commander_name = @commander
            AND system_name = @system
            AND station_name = @station
            AND material_name = @material
            AND status = 'In Transit';";

                cmd.Parameters.AddWithValue("@commander", commanderName);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName ?? "");

                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }
        public static void UpdateStationDelivery(string systemName, string stationName, string materialName, int amountDelivered)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            UPDATE station_requirements
            SET delivered_amount = delivered_amount + @delivered
            WHERE station_id = (
                SELECT station_id 
                FROM stations 
                WHERE system_name = @system 
                  AND station_name = @station
                LIMIT 1
            )
            AND material_name = @material;";

                cmd.Parameters.AddWithValue("@delivered", amountDelivered);
                cmd.Parameters.AddWithValue("@system", systemName);
                cmd.Parameters.AddWithValue("@station", stationName);
                cmd.Parameters.AddWithValue("@material", materialName);

                cmd.ExecuteNonQuery();
            }
        }
        public static void UpdateCommanderMaterialTotals(string commanderName, string materialName, int amountDelivered)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
            UPDATE commanders
            SET 
                total_all_materials = total_all_materials + @amount,
                [{materialName}] = COALESCE([{materialName}], 0) + @amount
            WHERE commander_name = @commander;";

                cmd.Parameters.AddWithValue("@amount", amountDelivered);
                cmd.Parameters.AddWithValue("@commander", commanderName);

                cmd.ExecuteNonQuery();
            }
        }

        public static StationDetails GetStationDetails(string systemName, string stationName)
        {
            var details = new StationDetails();

            using (var conn = GetConnection())
            {
                conn.Open();

                // 1. Basic station info
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT station_type, created_by
            FROM stations
            WHERE system_name = @systemName AND station_name = @stationName
            LIMIT 1;";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@stationName", stationName);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        details.StationName = stationName;
                        details.StationType = reader.GetString(0);
                        details.Owner = reader.GetString(1);
                    }
                    else
                    {
                        return null; // station not found
                    }
                }

                // 2. Materials summary
                var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"
            SELECT SUM(required_amount), SUM(delivered_amount)
            FROM station_requirements
            WHERE station_id = (
                SELECT station_id FROM stations
                WHERE system_name = @systemName AND station_name = @stationName
            );";
                cmd2.Parameters.AddWithValue("@systemName", systemName);
                cmd2.Parameters.AddWithValue("@stationName", stationName);

                using (var reader = cmd2.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        details.RequiredMaterials = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        details.DeliveredMaterials = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        details.PercentComplete = details.RequiredMaterials == 0 ? 0 :
                            (int)(((double)details.DeliveredMaterials / details.RequiredMaterials) * 100);
                    }
                }

                // 3. Last Delivery
                var cmd3 = conn.CreateCommand();
                cmd3.CommandText = @"
            SELECT MAX(timestamp)
            FROM deliveries
            WHERE system_name = @systemName AND station_name = @stationName;";
                cmd3.Parameters.AddWithValue("@systemName", systemName);
                cmd3.Parameters.AddWithValue("@stationName", stationName);

                var lastDelivery = cmd3.ExecuteScalar()?.ToString();
                details.LastDelivery = string.IsNullOrEmpty(lastDelivery) ? "(none)" : lastDelivery;

                // 4. Cargo In Transit
                var cmd4 = conn.CreateCommand();
                cmd4.CommandText = @"
            SELECT SUM(amount_loaded)
            FROM cargo_in_transit
            WHERE system_name = @systemName AND station_name = @stationName;";
                cmd4.Parameters.AddWithValue("@systemName", systemName);
                cmd4.Parameters.AddWithValue("@stationName", stationName);

                var cargoInTransit = cmd4.ExecuteScalar();
                details.CargoInTransit = cargoInTransit != DBNull.Value ? Convert.ToInt32(cargoInTransit) : 0;

                // 5. Trip Calculations
                details.Trips400 = details.RequiredMaterials / 400;
                details.Trips784 = details.RequiredMaterials / 784;
            }

            return details;
        }
        public static int GetStationTotalRequired(string systemName, string stationName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT SUM(required_amount) 
            FROM station_requirements 
            WHERE station_id = (
                SELECT station_id 
                FROM stations 
                WHERE system_name = @systemName 
                  AND station_name = @stationName
                LIMIT 1
            );";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@stationName", stationName);

                object result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        public static int GetStationTotalDelivered(string systemName, string stationName)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT SUM(delivered_amount) 
            FROM station_requirements 
            WHERE station_id = (
                SELECT station_id 
                FROM stations 
                WHERE system_name = @systemName 
                  AND station_name = @stationName
                LIMIT 1
            );";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@stationName", stationName);

                object result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
        public static CommanderSummary GetCommanderSummary(string commanderName)
        {
            var summary = new CommanderSummary
            {
                CommanderName = commanderName,
                MaterialsDelivered = new Dictionary<string, int>()
            };

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();

                // Get all material columns
                var columnCmd = conn.CreateCommand();
                columnCmd.CommandText = "PRAGMA table_info(commanders);";

                var materialColumns = new List<string>();
                using (var reader = columnCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string colName = reader.GetString(1);
                        if (!new[] { "commander_id", "commander_name", "squadron_name", "total_all_materials", "last_seen" }.Contains(colName))
                        {
                            materialColumns.Add(colName);
                        }
                    }
                }

                // Read the commander's row
                var dataCmd = conn.CreateCommand();
                dataCmd.CommandText = "SELECT * FROM commanders WHERE commander_name = @commander LIMIT 1;";
                dataCmd.Parameters.AddWithValue("@commander", commanderName);

                using (var reader = dataCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        summary.SquadronName = reader["squadron_name"].ToString();
                        summary.TotalDeliveredAllMaterials = reader["total_all_materials"] != DBNull.Value ? Convert.ToInt32(reader["total_all_materials"]) : 0;
                        summary.LastSeen = reader["last_seen"] != DBNull.Value ? DateTime.Parse(reader["last_seen"].ToString()) : DateTime.UtcNow;

                        foreach (var mat in materialColumns)
                        {
                            int amt = reader[mat] != DBNull.Value ? Convert.ToInt32(reader[mat]) : 0;
                            if (amt > 0)
                                summary.MaterialsDelivered[mat.Replace('_', ' ')] = amt;
                        }
                    }
                }
            }

            return summary;
        }




        public static List<string> GetSystemsOwnedByCommander(string commanderName)
        {
            var systems = new List<string>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT DISTINCT system_name
            FROM stations
            WHERE created_by = @commanderName;";

                cmd.Parameters.AddWithValue("@commanderName", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        systems.Add(reader.GetString(0));
                    }
                }
            }

            return systems;
        }
        public static List<string> GetSystemsWithActiveCargo(string commanderName)
        {
            var systems = new List<string>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT DISTINCT system_name
            FROM cargo_in_transit
            WHERE commander_name = @commanderName
              AND status = 'In Transit';";

                cmd.Parameters.AddWithValue("@commanderName", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        systems.Add(reader.GetString(0));
                    }
                }
            }

            return systems;
        }
        public static List<string> GetCommanderMaterialColumns()
        {
            var materials = new List<string>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(commanders);";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string colName = reader.GetString(1);
                        if (colName != "commander_id" && colName != "commander_name" && colName != "squadron_name" && colName != "total_all_materials" && colName != "last_seen")
                        {
                            materials.Add(colName);
                        }
                    }
                }
            }
            return materials;
        }
        public class CargoInTransit
        {
            public string System { get; set; }
            public string Station { get; set; }
            public string Material { get; set; }
            public int Amount { get; set; }
        }

        public static List<CargoInTransit> GetActiveCargoForCommander(string commanderName)
        {
            var cargoList = new List<CargoInTransit>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT system_name, station_name, material_name, amount_loaded
            FROM cargo_in_transit
            WHERE commander_name = @commander
              AND status = 'In Transit';
        ";
                cmd.Parameters.AddWithValue("@commander", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cargoList.Add(new CargoInTransit
                        {
                            System = reader["system_name"].ToString(),
                            Station = reader["station_name"].ToString(),
                            Material = reader["material_name"].ToString(),
                            Amount = Convert.ToInt32(reader["amount_loaded"])
                        });
                    }
                }
            }

            return cargoList;
        }
        public static List<(string MaterialName, string StationName, int AmountDelivered)> GetDeliveriesForCommander(string commanderName)
        {
            var results = new List<(string, string, int)>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT material_name, station_name, SUM(amount_delivered)
            FROM deliveries
            WHERE commander_name = @commander
            GROUP BY material_name, station_name;";
                cmd.Parameters.AddWithValue("@commander", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetInt32(2)
                        ));
                    }
                }
            }

            return results;
        }

        public static List<(string MaterialName, string StationName, int AmountLoaded)> GetCargoInTransitForCommander(string commanderName)
        {
            var results = new List<(string, string, int)>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT material_name, station_name, amount_loaded
            FROM cargo_in_transit
            WHERE commander_name = @commander
              AND status = 'In Transit';";
                cmd.Parameters.AddWithValue("@commander", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetInt32(2)
                        ));
                    }
                }
            }

            return results;
        }
        public static List<(string MaterialName, int Required, int Delivered)> GetStationMaterialBreakdown(string systemName, string stationName)
        {
            var result = new List<(string, int, int)>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT material_name, required_amount, delivered_amount
            FROM station_requirements
            INNER JOIN stations ON station_requirements.station_id = stations.station_id
            WHERE stations.system_name = @systemName
            AND stations.station_name = @stationName
            ORDER BY material_name;
        ";
                cmd.Parameters.AddWithValue("@systemName", systemName);
                cmd.Parameters.AddWithValue("@stationName", stationName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string mat = reader.GetString(0);
                        int required = reader.GetInt32(1);
                        int delivered = reader.GetInt32(2);
                        result.Add((mat, required, delivered));
                    }
                }
            }
            return result;
        }

    }

}