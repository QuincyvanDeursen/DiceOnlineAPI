using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.Models;
using MongoDB.Driver;

namespace DiceOnlineAPI.Database.Indexes
{
    public class MongoDbIndex
    {
        public static void EnsureIndexes(MongoDbService mongoDbService)
        {
            var collection = mongoDbService.GetCollection<Lobby>("lobbies");

            // TTL-index op UpdatedAt
            var indexKeys = Builders<Lobby>.IndexKeys.Ascending(l => l.UpdatedAt);
            var indexOptions = new CreateIndexOptions
            {
                ExpireAfter = TimeSpan.FromMinutes(180)
            };
            var indexModel = new CreateIndexModel<Lobby>(indexKeys, indexOptions);
            collection.Indexes.CreateOne(indexModel);
        }
    }
}
