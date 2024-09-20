#pragma warning disable 8625, 8603, 8629, 8604

using Gaos.Common;
using Gaos.Dbo.Model;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MongoDB.Bson;
using MongoDB.Driver;
using Gaos.Mongo;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Gaos.Mongo
{

    public class GetGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public long Version { get; set; }
        public string? GameData { get; set; }
    }

    public class SaveGroupGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Id { get; set; }
        public long Version { get; set; }
    }

    public class MakeEmptyGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class EnsureGameDataResult
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public long Version { get; set; }
        public BsonDocument? Document { get; set; }
    }

    public class GroupData
    {
        private static string CLASS_NAME = typeof(UserService).Name;

        private readonly MongoService MongoService;
        private readonly GameData GameDataService;
        private readonly IConfiguration configuration;

        private Gaos.Dbo.Model.User? User = null;

        private static JsonSerializerSettings jsonDiffSerializerSettings = jsondiff.Difference.GetJsonSerializerSettings();

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
                    { "_version", new BsonInt64(0)},
                    { "GameData", new BsonDocument()}
                };
                await collection.InsertOneAsync(document);
                return new MakeEmptyGameDataResult
                {
                    IsError = false,
                    ErrorMessage = ""
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


        public async Task<GetGameDataResult> GetGameData(UserService.GetGroupResult group, long version = -1, bool isJsonDiff = false, int slotId = 1)
        {
            const string METHOD_NAME = "GetGameData()";
            IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();
            try
            {
                // ensure group data exists
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                );
                bool documentExists = await collection.Find(filter).AnyAsync();
                if (!documentExists)
                {
                    var result = await MakeEmptyGameData(group, slotId);
                    if (result.IsError == true)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} error making empty game data");
                        return new GetGameDataResult
                        {
                            IsError = true,
                            ErrorMessage = "internal error"
                        };
                    }
                }

                if (version > -1)
                {
                    // get document at version
                    filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId),
                        Builders<BsonDocument>.Filter.Eq("_version", version)
                    );
                    BsonDocument documentAtVesrion = await collection.Find(filter).FirstOrDefaultAsync();
                    if (documentAtVesrion != null)
                    {
                        if (!isJsonDiff)
                        {
                            return new GetGameDataResult
                            {
                                IsError = false,
                                ErrorMessage = "",
                                Id = documentAtVesrion.GetValue("_id").ToString(),
                                Version = documentAtVesrion.GetValue("_version").ToInt64(),
                                GameData = documentAtVesrion.GetValue("GameData").ToString()
                            };
                        }
                        else
                        {
                            // get latest version of the document
                            filter = Builders<BsonDocument>.Filter.And(
                                Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                                Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                            );

                            // add ordering by  _version in case we have multiple such documents
                            SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("_version");
                            // take the last one
                            BsonDocument documentAtLatest = await collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
                            if (documentAtLatest == null)
                            {
                                Log.Error($"{CLASS_NAME}:{METHOD_NAME} error fetching group game data: latest document not found");
                                return new GetGameDataResult
                                {
                                    IsError = true,
                                    ErrorMessage = "latest document not found"
                                };
                            }

                            string gameDataStrAtVesion = documentAtVesrion.GetValue("GameData").ToString();
                            string gameDataStrAtLatest = documentAtLatest.GetValue("GameData").ToString();

                            // compute the diff from version to latest
                            JObject gameDataJoAtVesion = JObject.Parse(gameDataStrAtVesion);
                            JObject gameDataJoAtLatest = JObject.Parse(gameDataStrAtLatest);
                            var diff = jsondiff.Difference.CompareValues(gameDataJoAtVesion, gameDataJoAtLatest);
                            var strDiff = JsonConvert.SerializeObject(diff, jsonDiffSerializerSettings);

                            return new GetGameDataResult
                            {
                                IsError = false,
                                ErrorMessage = "",
                                Id = documentAtLatest.GetValue("_id").ToString(),
                                Version = documentAtLatest.GetValue("_version").ToInt64(),
                                GameData = strDiff
                            };


                        }
                    }
                    else
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME} error fetching group game data: version not found");
                        return new GetGameDataResult
                        {
                            IsError = true,
                            ErrorMessage = "version not found"
                        };
                    }
                }
                else
                {
                    // get the latest version of the document

                    filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                        Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                    );

                    // add ordering by  _version in case we have multiple such documents
                    SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("_version");
                    // take the last one
                    BsonDocument? documentLatest = await collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
                    if (documentLatest != null)
                    {
                        return new GetGameDataResult
                        {
                            IsError = false,
                            ErrorMessage = "",
                            Id = documentLatest.GetValue("_id").ToString(),
                            Version = documentLatest.GetValue("_version").ToInt64(),
                            GameData = documentLatest.GetValue("GameData").ToString()
                        };
                    }
                    else
                    {
                        return new GetGameDataResult
                        {
                            IsError = true,
                            ErrorMessage = "document not found"
                        };
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} Error: {ex}");
                return new GetGameDataResult
                {
                    IsError = true,
                    ErrorMessage = "internal error"
                };
            }
        }

        public async Task<SaveGroupGameDataResult> SaveGroupGameData(UserService.GetGroupResult group, string groupGameDataJson, long version, bool isJsonDiff, int slotId = 1)
        {
            const string METHOD_NAME = "SaveGroupGameData()";
            try
            {
                IMongoCollection<BsonDocument> collection = await MongoService.GetCollectionForGroupGameData();

                // ensure group game data exists
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                );
                bool documentExists = await collection.Find(filter).AnyAsync();
                if (!documentExists)
                {
                    var result = await MakeEmptyGameData(group, slotId);
                    if (result.IsError == true)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} error making empty group game data");
                        return new SaveGroupGameDataResult
                        {
                            IsError = true,
                            ErrorMessage = "internal error"
                        };
                    }
                }

                // fetch the last version of the group game data

                filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                );
                SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("_version");
                BsonDocument  document = await collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} error fetching group game data: not found");
                    return new SaveGroupGameDataResult
                    {
                        IsError = true,
                        ErrorMessage = "internal error"
                    };
                }

                if (version != document.GetValue("_version").ToInt64())
                {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} error saving group game data: version mismatch");
                    return new SaveGroupGameDataResult
                    {
                        IsError = true,
                        ErrorMessage = "version mismatch"
                    };
                }

                // increment the version
                long _version = version + 1;

                // compute the new game data
                BsonDocument gameDataBson;
                if (isJsonDiff)
                {
                    gameDataBson = GameData.AddGameDataDiff(document["GameData"].AsBsonDocument, groupGameDataJson);
                }
                else
                {
                    gameDataBson = BsonDocument.Parse(groupGameDataJson);
                }

                // insert new vesion of the group game data
                BsonDocument newDocument = new BsonDocument
                {
                    { "GroupId", new BsonInt32(group.GroupId) },
                    { "SlotId", new BsonInt32(slotId) },
                    { "_version", new BsonInt64(_version)},
                    { "GameData", gameDataBson}
                };
                await collection.InsertOneAsync(newDocument);

                // ensurw that we keep anly has 100 version of the group game data
                filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("GroupId", group.GroupId),
                    Builders<BsonDocument>.Filter.Eq("SlotId", slotId)
                );
                sort = Builders<BsonDocument>.Sort.Descending("_version");
                var cursor = collection.Find(filter).Sort(sort).Skip(100);
                await cursor.ForEachAsync(document =>
                {
                    collection.DeleteOne(document);
                });

                return new SaveGroupGameDataResult
                {
                    IsError = false,
                    ErrorMessage = "",
                    Id = newDocument.GetValue("_id").ToString(),
                    Version = _version
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
