#pragma warning disable 8601
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Gaos.Mongo
{
    public class MongoService
    {
        public static string CLASS_NAME = typeof(MongoService).Name;

        private readonly IConfiguration Configuration;

        private readonly string DbConnectionString = "";
        private readonly string DbNameForGameData = "gaos"; 
        private readonly string DbNameForChat = "gaos"; 

        private readonly string CollectionNameForGameData = "GameData"; 
        private readonly string CollectionNameForGroupGameData = "GroupData"; 


        public MongoService(IConfiguration configuration)
        {
            const string METHOD_NAME = "MongoService";

            Configuration = configuration;

            if (Configuration["mongodb_connection_string"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME} missing configuration value: mongodb_connection_string");
                throw new Exception("missing configuration value: mongodb_connection_string");
            }
            DbConnectionString = Configuration["mongodb_connection_string"];

            if (Configuration["mongodb_database_name"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME} missing configuration value: mongodb_database_name");
                throw new Exception("missing configuration value: mongodb_database_name");
            }
            DbNameForGameData = Configuration["mongodb_database_name"];
        }

        private MongoClient GetClient()
        {
            MongoClient client = new MongoClient(DbConnectionString);
            return client;
        }

        public async Task<IClientSessionHandle> StartSessionAsync()
        {
            var client = GetClient();
            var session = await client.StartSessionAsync();
            return session;
        }

        private IMongoDatabase GetDatabaseForGameData()
        {
            MongoClient client = GetClient();
            IMongoDatabase database = client.GetDatabase($"{DbNameForGameData}");
            return database;
        }

        private IMongoDatabase GetDatabaseForChat()
        {
            MongoClient client = GetClient();
            IMongoDatabase database = client.GetDatabase(DbNameForChat);
            return database;
        }

        private async Task<bool> IsCollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }

        private async Task CreateIndexesForGameData(IMongoDatabase database)
        {
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(CollectionNameForGameData);

            var indexKeysDefinition = Builders<BsonDocument>.IndexKeys.Ascending("UserId").Ascending("SlotId");
            var indexOptions = new CreateIndexOptions { Unique = true, Name = "UserId__SlotId" };
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeysDefinition, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel);
        }

        private async Task CreateIndexesForGroupGameData(IMongoDatabase database)
        {
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(CollectionNameForGroupGameData);

            var indexKeysDefinition = Builders<BsonDocument>.IndexKeys.Ascending("GroupId").Ascending("SlotId").Ascending("_version");
            var indexOptions = new CreateIndexOptions { Unique = true, Name = "GroupId__SlotId__version" };
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeysDefinition, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel);
        }

        public async Task<IMongoCollection<BsonDocument>> GetCollectionForGameData()
        {
            IMongoDatabase database = GetDatabaseForGameData();

            bool isCollectionExists = await IsCollectionExists(database, CollectionNameForGameData);



            if (!isCollectionExists)
            {
                await CreateIndexesForGameData(database);
            }
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(CollectionNameForGameData);
            return collection;
        }

        public async Task<IMongoCollection<BsonDocument>> GetCollectionForGroupGameData()
        {
            IMongoDatabase database = GetDatabaseForGameData();

            bool isCollectionExists = await IsCollectionExists(database, CollectionNameForGroupGameData);

            if (!isCollectionExists)
            {
                await CreateIndexesForGroupGameData(database);
            }
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(CollectionNameForGroupGameData);
            return collection;
        }
    }
}
