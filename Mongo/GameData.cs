#pragma warning disable 8600, 8601, 8618, 8604

using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Gaos.Mongo
{

    public class GetGameDataAsyncResult
    {
        public string Id { get; set; }
        public long Version { get; set; }
        public string GameDataJson { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum SaveGameDataAsyncResultErrorKind
    {
        JsonDiffBaseMismatchError,
        VersionMismatchError,
        InternalError,
    }

    public class SaveGameDataAsyncResult
    {
        public string Id { get; set; }
        public long Version { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public SaveGameDataAsyncResultErrorKind? ErrorKind { get; set; }

        public string GameDataJson { get; set; }
    }



    public class GameData
    {
        public static string CLASS_NAME = typeof(GameData).Name;

        private readonly MongoService MongoService;

        public GameData(MongoService mongoService)
        {
            MongoService = mongoService;
        }

        public class EnsureNewSlotResult
        {
            public bool IsError { get; set; }
            public string ErrorMessage { get; set; }

            public string Id { get; set; }
            public long Version { get; set; }
        }

        public async Task<EnsureNewSlotResult> EnsureNewGameSlot(int userId, int slotId, string userName, string country)
        {
            long _version = 0;

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
                { "Country", country },
                { "IsNewSlot", true },
                { "_version", new BsonInt64(_version)},
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
                return new EnsureNewSlotResult
                {
                    IsError = false,
                    Id = gameDataBsonExisting["_id"].ToString(),
                    Version = (int)gameDataBsonExisting["_version"].ToInt64(),
                };
            }
            else
            {
                // Insert new document
                await collection.InsertOneAsync(doc);
                gameDataBsonExisting = await collection.Find(filter).FirstOrDefaultAsync();
                return new EnsureNewSlotResult
                {
                    IsError = false,
                    Id = gameDataBsonExisting["_id"].ToString(),
                    Version = gameDataBsonExisting["_version"].ToInt64(),
                };
            }
        }

        public async Task DeleteGameSlot(int userId, int slotId)
        {
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();
            var filter = Builders<BsonDocument>.Filter
                .And(
                    Builders<BsonDocument>.Filter.Eq("UserId", userId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                 );
            await collection.DeleteOneAsync(filter);
        }


        static public BsonDocument AddGameDataDiff(BsonDocument gameDataBson, string gameDataDiffJson)
        {
            const string METHOD_NAME = "AddGameDataDiff";
            try
            {
                var serializerSettings = jsondiff.Difference.GetJsonSerializerSettings();

                var objA = JObject.Parse(gameDataBson.ToJson());
                var diff = JsonConvert.DeserializeObject<jsondiff.DiffValue>(gameDataDiffJson, serializerSettings);

                // objB = objA + diff
                var objB = jsondiff.Difference.AddDiff(objA, diff);
                var objB_json = JsonConvert.SerializeObject(objB);
                var objB_bson = BsonDocument.Parse(objB_json);
                return objB_bson;
            }

            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} {ex}");
                Log.Information($"{CLASS_NAME}:{METHOD_NAME} gameDataBson: {gameDataBson.ToJson()}");
                Log.Information($"{CLASS_NAME}:{METHOD_NAME} gameDataDiffJson: {gameDataDiffJson}");
                throw new Exception("failed to add game data diff");
            }
        }

        // Save the game data to the database at the specified slot for the specified user.

        public async Task<SaveGameDataAsyncResult> SaveGameDataAsync(int userId, int slotId, string gameDataJson, long version, bool isGameDataDiff, string gameDataJsonDiffBase = "")
        {
            const string METHOD_NAME = "SaveGameDataAsync";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();

            try
            {

                // read the document from the database
                var filter = Builders<BsonDocument>.Filter
                    .And(
                        Builders<BsonDocument>.Filter.Eq("UserId", userId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                     );

                BsonDocument doc = await collection.Find(filter).FirstOrDefaultAsync();



                if (doc == null)
                {
                    // document does not exist, insert new document
                    if (isGameDataDiff)
                    {
                        throw new Exception("game data diff is not supported for new game slot");
                    }

                    long _version = 0;
                    BsonDocument gameDataBson = BsonDocument.Parse(gameDataJson);


                    var update = Builders<BsonDocument>.Update
                        .Set("_version", new BsonInt64(_version))
                        .Set("GameData", gameDataBson);
                    var options = new UpdateOptions { IsUpsert = true };

                    await collection.UpdateOneAsync(filter, update, options);

                    if (Common.Context.IS_DEBUG && Common.Context.IS_DEBUG_SEND_GAMEDATA_ON_SAVE)
                    {
                        doc = await collection.Find(filter).FirstOrDefaultAsync();
                        return new SaveGameDataAsyncResult
                        {
                            IsError = false,
                            Version = _version,
                            GameDataJson = doc["GameData"].ToJson()
                        };
                    }
                    else
                    {

                        return new SaveGameDataAsyncResult
                        {
                            IsError = false,
                            Version = _version
                        };
                    }
                }
                else
                {
                    if (isGameDataDiff && gameDataJsonDiffBase != "")
                    {
                        Log.Information($"diff base: {gameDataJsonDiffBase}");
                        JObject gameDataJsonDiffBaseJObject = JObject.Parse(gameDataJsonDiffBase);
                        Log.Information($"doc: {doc["GameData"].ToJson()}");
                        JObject gameDataJsontMongoJObject = JObject.Parse(doc["GameData"].ToJson());
                        var isBasesEqual = jsondiff.Difference.IsEqualValues(gameDataJsonDiffBaseJObject, gameDataJsontMongoJObject);
                        if (!isBasesEqual.IsEqual)
                        {
                            Log.Error($"{CLASS_NAME}:{METHOD_NAME} game data diff base mismatch");
                            Log.Information($"{gameDataJsonDiffBase}");
                            Log.Information($"{gameDataJsontMongoJObject}");
                            return new SaveGameDataAsyncResult
                            {
                                IsError = true,
                                ErrorMessage = "game data diff base mismatch",
                                ErrorKind = SaveGameDataAsyncResultErrorKind.JsonDiffBaseMismatchError
                            };
                        }
                        else
                        {
                            Log.Information($"{CLASS_NAME}:{METHOD_NAME} game data diff base match");
                        }
                    }

                    // document exists, update the document if the version matches

                    long docVersion;
                    string docId;
                    if (!doc.Contains("_version"))
                    {
                        // version mismatch
                        return new SaveGameDataAsyncResult
                        {
                            IsError = true,
                            ErrorMessage = "version mismatch (version does not exist)",
                            ErrorKind = SaveGameDataAsyncResultErrorKind.VersionMismatchError
                        };
                    }
                    else
                    {
                        docVersion = doc["_version"].ToInt64();
                        docId = doc["_id"].ToString();
                    }

                    if (version != docVersion)
                    {
                        // version mismatch
                        return new SaveGameDataAsyncResult
                        {
                            IsError = true,
                            ErrorMessage = "version mismatch",
                            ErrorKind = SaveGameDataAsyncResultErrorKind.VersionMismatchError
                        };
                    }


                    // increment the version
                    long _version = docVersion + 1;

                    // compute the new game data
                    BsonDocument gameDataBson;
                    if (isGameDataDiff)
                    {
                        gameDataBson = AddGameDataDiff(doc["GameData"].AsBsonDocument, gameDataJson);
                    }
                    else
                    {
                        gameDataBson = BsonDocument.Parse(gameDataJson);
                    }

                    // save the new game data
                    var update = Builders<BsonDocument>.Update
                        .Set("_version", new BsonInt64(_version))
                        .Set("GameData", gameDataBson);

                    await collection.UpdateOneAsync(filter, update);

                    if (Common.Context.IS_DEBUG && Common.Context.IS_DEBUG_SEND_GAMEDATA_ON_SAVE)
                    {
                        doc = await collection.Find(filter).FirstOrDefaultAsync();
                        return new SaveGameDataAsyncResult
                        {
                            IsError = false,
                            Id = docId,
                            Version = _version,
                            GameDataJson = doc["GameData"].ToJson()
                        };
                    }
                    else
                    {
                        return new SaveGameDataAsyncResult
                        {
                            IsError = false,
                            Id = docId,
                            Version = _version
                        };
                    }

                }


            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} {ex}");

                throw new Exception("failed to save gaame data");
            }

        }

        // Get the game data to from database from the specified slot for the specified user.

        public async Task<GetGameDataAsyncResult> GetGameDataAsync(int userId, int slotId, long? version = null)
        {
            const string METHOD_NAME = "GetGameDataAsync";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGameData();
            try
            {


                var filter = Builders<BsonDocument>.Filter
                    .And(
                        Builders<BsonDocument>.Filter.Eq("UserId", userId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                     );

                BsonDocument doc = await collection.Find(filter).FirstOrDefaultAsync();

                if (doc == null)
                {
                    return new GetGameDataAsyncResult
                    {
                        IsError = true,
                        ErrorMessage = "not found"
                    };
                }

                if (version != null && version != doc["_version"].ToInt64())
                {
                    return new GetGameDataAsyncResult
                    {
                        IsError = true,
                        ErrorMessage = "version mismatch"
                    };
                }


                return new GetGameDataAsyncResult
                {
                    IsError = false,
                    Id = doc["_id"].ToString(),
                    Version = doc["_version"].ToInt64(),
                    GameDataJson = doc["GameData"].ToJson()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} {ex}");
                throw new Exception("failed to get game data");
            }
        }

        public class GetUserSlotIdsResult
        {
            public string _id { get; set; }
            public long _version { get; set; }
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
                .Include("_version")
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
                slotIds.Add(new GetUserSlotIdsResult
                {
                    _id = gameDataBson["_id"].ToString(),
                    _version = gameDataBson["_version"].ToInt64(),
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
