namespace DepremVeriProjesi.Models
{
    public class EarthquakeData
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
        public double Depth { get; set; }
        public double Magnitude { get; set; }
        public double[]? Coordinates { get; set; }
        public string? CityName { get; set; }
    }
}
