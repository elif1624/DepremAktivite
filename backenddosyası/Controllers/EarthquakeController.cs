using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using DepremVeriProjesi.Services;
using DepremVeriProjesi.Models;

namespace DepremVeriProjesi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EarthquakeController : ControllerBase
    {
        private static readonly HttpClient _client;
        private readonly IMongoDBService _mongoDBService;
        private static readonly string _apiUrl = "https://api.orhanaydogdu.com.tr/deprem/";
        private static readonly JsonSerializerOptions _jsonOptions;

        public EarthquakeController(IMongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        static EarthquakeController()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "DepremProje/1.0");
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        [HttpGet("get-earthquake-data")]
        public async Task<IActionResult> GetEarthquakeData()
        {
            try
            {
                Console.WriteLine("Deprem verisi istendi");
                var response = await _client.GetAsync(_apiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API hatası: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, "Deprem verisi alınamadı.");
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API yanıtı alındı");
                
                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);

                if (apiResponse?.Result == null)
                {
                    Console.WriteLine("Veri bulunamadı");
                    return NotFound("Deprem verisi bulunamadı.");
                }

                var popUpData = new Dictionary<string, List<EarthquakeSummary>>();

                foreach (var quake in apiResponse.Result)
                {
                    if (quake?.GeoJson?.Coordinates?.Count >= 2 && quake.Date != null)
                    {
                        var dateTimeParts = quake.Date.Split(' ');
                        var summary = new EarthquakeSummary
                        {
                            Date = dateTimeParts[0],
                            Time = dateTimeParts.Length > 1 ? dateTimeParts[1] : "",
                            Depth = quake.Depth ?? 0,
                            Magnitude = quake.Magnitude ?? 0,
                            Coordinates = new double[]
                            {
                                quake.GeoJson.Coordinates[0],
                                quake.GeoJson.Coordinates[1]
                            }
                        };

                        var cityName = quake.LocationProperties?.ClosestCity?.Name ?? "Bilinmeyen Lokasyon";
                        
                        if (!popUpData.ContainsKey(cityName))
                        {
                            popUpData[cityName] = new List<EarthquakeSummary>();
                        }
                        
                        popUpData[cityName].Add(summary);
                    }
                }

                Console.WriteLine($"Toplam {popUpData.Count} şehir için veri hazırlandı");
                
                // Dictionary'yi EarthquakeData formatına dönüştür
                var earthquakeDataDict = new Dictionary<string, List<EarthquakeData>>();
                foreach (var cityData in popUpData)
                {
                    earthquakeDataDict[cityData.Key] = cityData.Value.Select(s => new EarthquakeData
                    {
                        Date = s.Date,
                        Time = s.Time,
                        Depth = s.Depth,
                        Magnitude = s.Magnitude,
                        Coordinates = s.Coordinates,
                        CityName = cityData.Key
                    }).ToList();
                }

                // MongoDB'ye kaydet
                await _mongoDBService.SaveEarthquakeDataAsync(earthquakeDataDict);
                Console.WriteLine("Veriler MongoDB'ye kaydedildi");

                return Ok(new
                {
                    success = true,
                    timestamp = DateTime.UtcNow,
                    data = popUpData
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                return StatusCode(500, "Beklenmeyen bir hata oluştu.");
            }
        }

        [HttpGet("get-stored-data")]
        public async Task<IActionResult> GetStoredData()
        {
            try
            {
                var data = await _mongoDBService.GetEarthquakeDataAsync();
                return Ok(new
                {
                    success = true,
                    timestamp = DateTime.UtcNow,
                    data = data
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB'den veri çekme hatası: {ex.Message}");
                return StatusCode(500, "Veritabanından veri çekilirken bir hata oluştu.");
            }
        }
    }

    public class ApiResponse
    {
        [JsonPropertyName("result")]
        public List<Earthquake>? Result { get; set; }
    }

    public class Earthquake
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("mag")]
        public double? Magnitude { get; set; }

        [JsonPropertyName("depth")]
        public double? Depth { get; set; }

        [JsonPropertyName("geojson")]
        public GeoJson? GeoJson { get; set; }

        [JsonPropertyName("location_properties")]
        public LocationProperties? LocationProperties { get; set; }
    }

    public class GeoJson
    {
        [JsonPropertyName("coordinates")]
        public List<double>? Coordinates { get; set; }
    }

    public class LocationProperties
    {
        [JsonPropertyName("closestCity")]
        public ClosestCity? ClosestCity { get; set; }
    }

    public class ClosestCity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class EarthquakeSummary
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
        public double Depth { get; set; }
        public double Magnitude { get; set; }
        public double[]? Coordinates { get; set; }
    }
}
