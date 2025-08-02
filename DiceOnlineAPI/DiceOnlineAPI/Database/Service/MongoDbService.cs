using MongoDB.Driver;

namespace DiceOnlineAPI.Database.Service
{
    public class MongoDbService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoDatabase? _database;

        public MongoDbService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetConnectionString("MongoDbConnection");
            var mongoUrl = new MongoUrl(connectionString);
            var client = new MongoClient(mongoUrl);
            _database = client.GetDatabase(mongoUrl.DatabaseName);

        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            if (_database == null)
            {
                throw new InvalidOperationException("Database is not initialized.");
            }
            return _database.GetCollection<T>(collectionName);
        }
    }
}
