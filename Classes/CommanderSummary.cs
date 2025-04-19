using System;
using System.Collections.Generic;

namespace SUBR
{
    public class CommanderSummary
    {
        public string CommanderName { get; set; }
        public string SquadronName { get; set; }
        public int TotalDeliveredAllMaterials { get; set; }
        public DateTime LastSeen { get; set; }
        public Dictionary<string, int> MaterialsDelivered { get; set; } = new Dictionary<string, int>();
    }
}
