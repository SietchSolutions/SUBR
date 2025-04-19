public class StationDetails
{
    public string StationName { get; set; }
    public string StationType { get; set; }
    public string Owner { get; set; }
    public int RequiredMaterials { get; set; }
    public int DeliveredMaterials { get; set; }
    public int PercentComplete { get; set; }
    public string LastDelivery { get; set; }
    public int CargoInTransit { get; set; }
    public int Trips400 { get; set; }
    public int Trips784 { get; set; }
}
