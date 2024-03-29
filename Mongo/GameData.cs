﻿using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaos.Mongo
{

    public class GetGameDataAsyncResult
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string GameDataJson { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SaveGameDataAsyncResult
    {
        public string Id { get; set; }
        public string Version { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
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
            public string Version { get; set; }
        }

        public async Task<EnsureNewSlotResult> EnsureNewGameSlot(int userId, int slotId, string userName)
        {
            var _version = "0";

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
                { "_version", _version},
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
                    Version = gameDataBsonExisting["_version"].ToString(),
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
                    Version = gameDataBsonExisting["_version"].ToString(),
                };
            }
        }


        private BsonDocument AddGameDataDiff(BsonDocument gameDataBson, string gameDataDiffJson)
        {
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
                Log.Error(ex, $"{CLASS_NAME}:AddGameDataDiff {ex}");
                throw new Exception("failed to add game data diff");
            }
        }

        // Save the game data to the database at the specified slot for the specified user.

        public async Task<SaveGameDataAsyncResult> SaveGameDataAsync(int userId, int slotId, string gameDataJson, string version, bool isGameDataDiff)
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

                    ulong _version = 0;
                    BsonDocument gameDataBson = BsonDocument.Parse(gameDataJson);


                    var update = Builders<BsonDocument>.Update
                        .Set("_version", _version.ToString())
                        .Set("GameData", gameDataBson);
                    var options = new UpdateOptions { IsUpsert = true };

                    await collection.UpdateOneAsync(filter, update, options);

                    return new SaveGameDataAsyncResult
                    {
                        IsError = false,
                        Version = _version.ToString()
                    };
                }
                else
                {
                    // document exists, update the document if the version matches

                    string docVersion;
                    string docId;
                    if (!doc.Contains("_version"))
                    {
                        // version mismatch
                        return new SaveGameDataAsyncResult
                        {
                            IsError = true,
                            ErrorMessage = "version mismatch (version does not exist)"
                        };
                    } else {
                        docVersion = doc["_version"].ToString();
                        docId = doc["_id"].ToString();
                    }

                    if (version != docVersion)
                    {
                        // version mismatch
                        return new SaveGameDataAsyncResult
                        {
                            IsError = true,
                            ErrorMessage = "version mismatch"
                        };
                    }


                    // increment the version
                    ulong _version = ulong.Parse(docVersion) + 1;

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
                        .Set("_version", _version.ToString())
                        .Set("GameData", gameDataBson);

                    await collection.UpdateOneAsync(filter, update);

                    return new SaveGameDataAsyncResult
                    {
                        IsError = false,
                        Id = docId,
                        Version = _version.ToString()
                    };

                }


            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} {ex}");

                throw new Exception("failed to save gaame data");
            }

        }

        // Get the game data to from database from the specified slot for the specified user.

        public async Task<GetGameDataAsyncResult> GetGameDataAsync(int userId, int slotId, string? version = null)
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

                if (version != null && version != doc["_version"].ToString())
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
                    Version = doc["_version"].ToString(),
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
            public string _version { get; set; }
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
                    _version = gameDataBson["_version"].ToString(),
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
