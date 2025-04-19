using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SUBR

{
    public static class CommanderHelper
    {
        public static (string Commander, string Ship, string Squadron, DateTime LastSeen) GetCommanderInfo()

        {
            string commander = "Unknown";
            string ship = "Unknown";
            string squadron = "Unknown";
            DateTime lastSeen = DateTime.MinValue;

            try
            {
                string logPath = ConfigHelper.GetLogFilePath();
                if (string.IsNullOrWhiteSpace(logPath) || !Directory.Exists(logPath))
                    return (commander, ship, squadron, lastSeen);

                var journalFiles = Directory.GetFiles(logPath, "Journal.*.log")
                    .OrderByDescending(File.GetLastWriteTime)
                    .Take(50);

                foreach (var file in journalFiles)
                {
                    try
                    {
                        lastSeen = File.GetLastWriteTime(file);

                        foreach (var line in File.ReadLines(file))
                        {
                            if (line.Contains("\"event\":\"Commander\"") && commander == "Unknown")
                            {
                                var json = JObject.Parse(line);
                                commander = json["Name"]?.ToString() ?? "Unknown";
                            }

                            if (line.Contains("\"event\":\"Loadout\"") && ship == "Unknown")
                            {
                                var json = JObject.Parse(line);
                                ship = json["Ship"]?.ToString() ?? "Unknown";
                            }

                            if (line.Contains("\"event\":\"SquadronStartup\""))
                            {
                                var json = JObject.Parse(line);
                                squadron = json["SquadronName"]?.ToString() ?? "Unknown";
                            }
                        }

                        if (commander != "Unknown" && ship != "Unknown")
                            break;
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error reading logs: " + ex.Message);
            }

            return (commander, ship, squadron, lastSeen);
        }



        public static string GetCommanderName()
        {
            return GetCommanderInfo().Commander;
        }

        public static string GetStationAndSystemFromMarket()
        {
            string result = "Unknown Station [Unknown System]";

            try
            {
                string logPath = ConfigHelper.GetLogFilePath();
                if (string.IsNullOrWhiteSpace(logPath)) return result;

                string marketJson = Path.Combine(logPath, "market.json");
                if (File.Exists(marketJson))
                {
                    var market = JObject.Parse(File.ReadAllText(marketJson));
                    string station = market["stationName"]?.ToString() ?? "Unknown Station";
                    string system = market["starSystem"]?.ToString() ?? "Unknown System";

                    result = $"{station} [{system}]";
                }
            }
            catch
            {
                // Fail silently
            }

            return result;
        }

        public static string GetSquadronName()
        {
            string journalPath = ConfigHelper.GetLogFilePath();
            if (string.IsNullOrWhiteSpace(journalPath)) return "Unaffiliated";

            try
            {
                var journalFiles = Directory.GetFiles(journalPath, "Journal.*.log")
                                            .OrderByDescending(File.GetLastWriteTime);

                foreach (var file in journalFiles)
                {
                    try
                    {
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            var lines = new List<string>();
                            while (!reader.EndOfStream)
                                lines.Add(reader.ReadLine());

                            foreach (var line in lines.AsEnumerable().Reverse())
                            {
                                if (line.Contains("\"event\":\"SquadronStartup\""))
                                {
                                    var json = JObject.Parse(line);
                                    return json["SquadronName"]?.ToString() ?? "Unaffiliated";
                                }
                            }
                        }
                    }
                    catch { continue; }
                }
            }
            catch { }

            return "Unaffiliated";
        }
        public static CommanderSummary GetCommanderSummary(string commanderName)
        {
            var summary = new CommanderSummary();

            using (var conn = SqliteHelper.GetConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM commanders WHERE commander_name = @name;";
                cmd.Parameters.AddWithValue("@name", commanderName);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        summary.CommanderName = reader["commander_name"].ToString();
                        summary.SquadronName = reader["squadron_name"].ToString();
                        summary.TotalDeliveredAllMaterials = reader["total_all_materials"] != DBNull.Value
                            ? Convert.ToInt32(reader["total_all_materials"])
                            : 0;
                        summary.LastSeen = reader["last_seen"] != DBNull.Value
                            ? DateTime.Parse(reader["last_seen"].ToString())
                            : DateTime.UtcNow;

                        
                        
                    }
                }
            }

            return summary;
        }



    }
}

