#pragma warning disable 8625, 8603, 8629, 8604

using Gaos.Common;
using Gaos.Dbo.Model;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MongoDB.Bson;
using MongoDB.Driver;
using Gaos.Mongo;

namespace gaos.Mongo
{

    public class GetGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Version { get; set; }
        public string? GameData { get; set; }
    }

    public class SaveGroupGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MakeEmptyGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public int Version { get; set; }
        public BsonDocument? Document { get; set; }
    }

    public class EnsureGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public int Version { get; set; }
        public BsonDocument? Document { get; set; }
    }

    public class GroupGameData
    {
        private static string CLASS_NAME = typeof(UserService).Name;

        private readonly MongoService MongoService;
        private readonly GameData GameDataService;

        private Gaos.Dbo.Model.User? User = null;

        public GroupGameData(MongoService mongoService, GameData gameDataService)
        {
            this.MongoService = mongoService;
            this.GameDataService = gameDataService;
        }

        public async Task updateOwnersGameDataInGameData(BsonDocument document, int ownerId, int slotId = 1)
        {
            const string METHOD_NAME = "updateOwnersGameDataInGameData()";

            // fetch the owner's game data
            BsonDocument ownersDocument = null;

            IMongoCollection<BsonDocument> collectionGameData = await MongoService.GetCollectionForGameData();
            try
            {
                var filter = Builders<BsonDocument>.Filter
                    .And(
                        Builders<BsonDocument>.Filter.Eq("UserId", ownerId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                     );

                ownersDocument = await collectionGameData.Find(filter).FirstOrDefaultAsync();
                if (ownersDocument == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error fetching owner's game data: not found");
                    throw new Exception("owner's game data not found");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} error fetching owner's game data: {ex}");
                throw new Exception("owner's game data not found");
            }

            if (!document.Contains("GameData"))
            {
                document.Add("GameData", new BsonDocument());
            }
            BsonDocument gameDataDoc = document.GetValue("GameData").ToBsonDocument();

            if (!gameDataDoc.Contains("OwnersGameData"))
            {
                gameDataDoc.Add("OwnersGameData", ownersDocument);
            }
            else
            {
                gameDataDoc.Set("OwnersGameData", ownersDocument);
            }

        }

        public async Task updateOwnersDataInGroup(Groupp group, int slotId = 1)
        {
            const string METHOD_NAME = "updateOwnersDataInGroup()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                var ensureResult = await EnsureGameData(group, slotId);
                if (ensureResult.IsError == true)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error updating owner's data in group, EnsureGameData() failed");
                    throw new Exception("error updating owner's data in group, EnsureGameData() failed");
                }
                BsonDocument document = ensureResult.Document;
                await updateOwnersGameDataInGameData(document, (int)group.OwnerId, slotId);

                var version = document.GetValue("_version").ToInt32();
                document.Set("_version", version + 1);

                // upsert the document
                var filter = Builders<BsonDocument>.Filter.Eq("_id", document.GetValue("_id"));
                await collection.ReplaceOneAsync(filter, document);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} Error: {ex}");
                throw new Exception("error updating owner's data in group, internal error");
            }
        }

        public async Task<MakeEmptyGameDataResult> MakeEmptyGameData(Groupp group, int slotId = 1)
        {
            const string METHOD_NAME = "MakeEmptyGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                BsonDocument document = new BsonDocument
                {
                    { "GroupId", group.Id },
                    { "_version", new BsonInt32(0) },
                    { "GameData", "{}" }
                };
                await updateOwnersGameDataInGameData(document, (int)group.OwnerId, slotId); 
                await collection.InsertOneAsync(document);
                return new MakeEmptyGameDataResult
                {
                    IsError = false,
                    ErrorMessage = "",
                    Id = document.GetValue("_id").ToString(),
                    Version = document.GetValue("_version").ToInt32(),
                    Document = document
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex}");
                return new MakeEmptyGameDataResult
                {
                    IsError = true,
                    ErrorMessage = "internal error"
                };
            }
        }

        public async Task<EnsureGameDataResult> EnsureGameData(Groupp group, int slotId = 1)
        {
            const string METHOD_NAME = "EnsureGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.Id),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                );
                SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort
                    .Ascending("GroupId")
                    .Ascending("SlotId")
                    .Ascending("_version");
                BsonDocument? document = await collection.Find(filter)
                    .Sort(sort)
                    .FirstOrDefaultAsync();
                if (document == null)
                {
                    var result = await MakeEmptyGameData(group, slotId);
                    if (result.IsError == true)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} error making empty game data");
                        return new EnsureGameDataResult
                        {
                            IsError = result.IsError,
                            ErrorMessage = result.ErrorMessage,
                        };
                    }
                    else
                    {
                        return new EnsureGameDataResult
                        {
                            IsError = false,
                            ErrorMessage = "",
                            Id = result.Id,
                            Version = result.Version,
                            Document = result.Document
                        };
                    }
                }
                return new EnsureGameDataResult
                {
                    IsError = false,
                    ErrorMessage = "",
                    Id = document.GetValue("_id").ToString(),
                    Version = document.GetValue("_version").ToInt32(),
                    Document = document
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex}");
                return new EnsureGameDataResult
                {
                    IsError = true,
                    ErrorMessage = "internal error"
                };
            }
        }

        public async Task<GetGameDataResult> GetGameData(Groupp group, int slotId = 1)
        {
            const string METHOD_NAME = "GetGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                var ensureResult = await EnsureGameData(group, slotId);
                if (ensureResult.IsError == true)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error ensuring group data exists");
                    return new GetGameDataResult
                    {
                        IsError = true,
                        ErrorMessage = ensureResult.ErrorMessage
                    };
                }

                var document = ensureResult.Document;
                return new GetGameDataResult {
                    IsError = false,
                    ErrorMessage = "",
                    Version = document.GetValue("_version").ToString(),
                    GameData = document.GetValue("GameData").ToString()
                };

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} Error: {ex}");
                return new GetGameDataResult
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SaveGroupGameDataResult> SaveGroupGameData(Groupp group, string groupGameDataJson, int version, int slotId = 1)
        {
            const string METHOD_NAME = "SaveGroupGameData()";
            try
            {
                IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
                var ensureResult = await EnsureGameData(group, slotId);
                if (ensureResult.IsError == true)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error ensuring group data exists");
                    return new SaveGroupGameDataResult
                    {
                        IsError = true,
                        ErrorMessage = ensureResult.ErrorMessage
                    };
                }

                var document = ensureResult.Document;
                int documentVersion = document.GetValue("_version").ToInt32();
                if (documentVersion != version)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error: version mismatch");
                    return new SaveGroupGameDataResult
                    {
                        IsError = true,
                        ErrorMessage = "version mismatch"
                    };
                }

                var goupGameDataDoc = BsonDocument.Parse(groupGameDataJson);
                document.GetElement("GameData").ToBsonDocument().Set("GroupData", goupGameDataDoc);

                var filter = Builders<BsonDocument>.Filter.Eq("_id", document.GetValue("_id"));
                await collection.ReplaceOneAsync(filter, document);

                return new SaveGroupGameDataResult
                {
                    IsError = false,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} Error: {ex}");
                return new SaveGroupGameDataResult
                {
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }


    }
}
