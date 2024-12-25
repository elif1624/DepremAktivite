using MongoDB.Driver;
using Microsoft.Extensions.Options;
using DepremVeriProjesi.Models;

namespace DepremVeriProjesi.Services
{
    public interface IMongoDBService
    {
        Task SaveEarthquakeDataAsync(Dictionary<string, List<EarthquakeData>> data);
        Task<Dictionary<string, List<EarthquakeData>>> GetEarthquakeDataAsync();
    }

    public class MongoDBService : IMongoDBService
    {
        private readonly IMongoCollection<EarthquakeData> _earthquakeCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _earthquakeCollection = mongoDatabase.GetCollection<EarthquakeData>(mongoDBSettings.Value.CollectionName);
        }

        public async Task SaveEarthquakeDataAsync(Dictionary<string, List<EarthquakeData>> data)
        {
            var allEarthquakes = new List<EarthquakeData>();
            foreach (var cityData in data)
            {
                foreach (var earthquake in cityData.Value)
                {
                    earthquake.CityName = cityData.Key;
                    allEarthquakes.Add(earthquake);
                }
            }

            if (allEarthquakes.Any())
            {
                // Mevcut verileri temizle
                await _earthquakeCollection.DeleteManyAsync(Builders<EarthquakeData>.Filter.Empty);
                // Yeni verileri ekle
                await _earthquakeCollection.InsertManyAsync(allEarthquakes);
            }
        }

        public async Task<Dictionary<string, List<EarthquakeData>>> GetEarthquakeDataAsync()
        {
            var allEarthquakes = await _earthquakeCollection.Find(_ => true).ToListAsync();
            return allEarthquakes.GroupBy(e => e.CityName)
                                .ToDictionary(
                                    g => g.Key ?? "Bilinmeyen",
                                    g => g.ToList()
                                );
        }
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string CollectionName { get; set; } = null!;
    }
}
