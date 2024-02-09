using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Gaos.Mongo
{
    public class GameData
    {
        public static string CLASS_NAME = typeof(GameData).Name;

        private readonly MongoService MongoService;

        public GameData(MongoService mongoService)
        {
            MongoService = mongoService;
        }

        public async Task EnsureNewGameSlot(int userId, int slotId, string userName)
        {

            BsonDocument gameData = new BsonDocument
            {
                { "username", userName },
                { "seconds", 0 },
                { "minutes", 0 },
                { "hours", 0 }
            };

            BsonDocument doc = new BsonDocument
            {
                { "UserId", userId },
                { "SlotId", slotId },
                { "IsNewSlot", true },
                { "GameData", gameData }
            };

            // Check if document with same userId and slotId already exists
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();
            var filter = Builders<BsonDocument>.Filter
                .And(
                    Builders<BsonDocument>.Filter.Eq("UserId", userId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                 );
            BsonDocument gameDataBsonExisting = await collection.Find(filter).FirstOrDefaultAsync();
            if (gameDataBsonExisting != null)
            {
                // Notthing to do, game slot already exists
                return;
            }
            else
            {
                // Insert new document
                await collection.InsertOneAsync(doc);
            }
        }

        // Save the game data to the database at the specified slot for the specified user.

        public async Task SaveGameDataAsync(int userId, int slotId, string gameDataJson)
        {
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();


            BsonDocument gameDataBson = BsonDocument.Parse(gameDataJson);

            var filter = Builders<BsonDocument>.Filter
                .And(
                    Builders<BsonDocument>.Filter.Eq("UserId", userId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                 );
            var update = Builders<BsonDocument>.Update.Set("GameData", gameDataBson);
            var options = new UpdateOptions { IsUpsert = true };

            await collection.UpdateOneAsync(filter, update, options);

        }

        // Get the game data to from database from the specified slot for the specified user.

        public async Task<string?> GetGameDataAsync(int userId, int slotId)
        {

            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();

            var filter = Builders<BsonDocument>.Filter
                .And(
                    Builders<BsonDocument>.Filter.Eq("UserId", userId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                 );

            BsonDocument gameDataBson = await collection.Find(filter).FirstOrDefaultAsync();

            if (gameDataBson == null)
            {
                return null;
            }

            return gameDataBson["GameData"].ToJson();
        }

        public class GetUserSlotIdsResult {
            public string _id { get; set; }
            public int UserId { get; set; }
            public int SlotId { get; set; }

            public string UserName { get; set; }
            public int Seconds { get; set; }
            public int Minutes { get; set; }
            public int Hours { get; set; }
        }

        // For given user, get all users slots ids

        public async Task<List<GetUserSlotIdsResult>> GetUserSlotIdsAsync(int userId)
        {
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();

            var filter = Builders<BsonDocument>.Filter
                .And(
                    Builders<BsonDocument>.Filter.Eq("UserId", userId)
                 );

            var projection = Builders<BsonDocument>.Projection
                .Include("SlotId")
                .Include("IsNewSlot")
                .Include("GameData.username")
                .Include("GameData.seconds")
                .Include("GameData.minutes")
                .Include("GameData.hours")
                ;

            List<BsonDocument> gameDataBsonList = await collection.Find(filter).Project(projection).ToListAsync();

            List<GetUserSlotIdsResult> slotIds = new List<GetUserSlotIdsResult>();

            foreach (BsonDocument gameDataBson in gameDataBsonList)
            {
                var doc = gameDataBson["GameData"].AsBsonDocument;

                bool IsNewSlot = false;
                if (gameDataBson.Contains("IsNewSlot"))
                {
                    IsNewSlot = gameDataBson["IsNewSlot"].ToBoolean();
                }

                // slotIds.Add(gameDataBson["SlotId"].ToInt32());
                slotIds.Add(new GetUserSlotIdsResult {
                    _id = gameDataBson["_id"].ToString(),
                    UserId = userId,
                    SlotId = gameDataBson["SlotId"].ToInt32(),


                    UserName = doc["username"].ToString(),
                    Seconds = doc["seconds"].ToInt32(),
                    Minutes = doc["minutes"].ToInt32(),
                    Hours = doc["hours"].ToInt32()
                });
            }

            return slotIds;
        }
    }
}
