using MongoDB.Bson;
using MongoDB.Driver;

namespace API.Services
{
    public interface IMongoIdGenerator
    {
        Task<int> NextAsync(string name, CancellationToken cancellationToken = default);
    }

    public class MongoIdGenerator(IMongoDatabase database) : IMongoIdGenerator
    {
        private readonly IMongoCollection<BsonDocument> _counters = database.GetCollection<BsonDocument>("Counters");

        public async Task<int> NextAsync(string name, CancellationToken cancellationToken = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", name);
            var update = Builders<BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var doc = await _counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
            return doc["seq"].AsInt32;
        }
    }
}

