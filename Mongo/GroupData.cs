#pragma warning disable 8625, 8603, 8629, 8604

using Gaos.Common;
using Gaos.Dbo.Model;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MongoDB.Bson;
using MongoDB.Driver;
using Gaos.Mongo;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Gaos.Mongo
{

    public class GetGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public int Version { get; set; }
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

    public class GroupData
    {
        private static string CLASS_NAME = typeof(UserService).Name;

        private readonly MongoService MongoService;
        private readonly GameData GameDataService;
        private readonly IConfiguration configuration;

        private Gaos.Dbo.Model.User? User = null;

        public GroupData(MongoService mongoService, GameData gameDataService, IConfiguration configuration)
        {
            this.MongoService = mongoService;
            this.GameDataService = gameDataService;
            this.configuration = configuration;
        }

        public async Task<String> GetOwnersData(UserService.GetGroupResult group, int slotId = 1)
        {
            const string METHOD_NAME = "GetOwnersData()";
            try
            {
                IMongoCollection<BsonDocument> collectionGameData = await MongoService.GetCollectionForGameData();
                BsonDocument ownersDocument = null;

                var filter = Builders<BsonDocument>.Filter
                    .And(
                        Builders<BsonDocument>.Filter.Eq("UserId", group.GroupOwnerId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                     );

                ownersDocument = await collectionGameData.Find(filter).FirstOrDefaultAsync();
                if (ownersDocument == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error fetching owner's game data: not found");
                    throw new Exception("owner's game data not found");
                }

                return ownersDocument.ToString();

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} Error: {ex}");
                throw new Exception("error updating owner's data in group, internal error");
            }
        }

        public async Task<MakeEmptyGameDataResult> MakeEmptyGameData(UserService.GetGroupResult group, int slotId = 1)
        {
            const string METHOD_NAME = "MakeEmptyGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                BsonDocument document = new BsonDocument
                {
                    { "GroupId", new BsonInt32(group.GroupId) },
                    { "SlotId", new BsonInt32(slotId) },
                    { "_version", new BsonInt32(0)},
                    { "GameData", new BsonDocument()}
                };
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

        public async Task<EnsureGameDataResult> EnsureGameData(UserService.GetGroupResult group, int slotId = 1)
        {
            const string METHOD_NAME = "EnsureGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
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

        public async Task<GetGameDataResult> GetGameData(UserService.GetGroupResult group, int slotId = 1)
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
                    Id = document.GetValue("_id").ToString(),
                    Version = document.GetValue("_version").ToInt32(),
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

        public async Task<SaveGroupGameDataResult> SaveGroupGameData(UserService.GetGroupResult group, string groupGameDataJson, int version, int slotId = 1)
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
                document.Set("GameData", goupGameDataDoc);

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
