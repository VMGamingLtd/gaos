#pragma warning disable 8600, 8602, 8604, 8605

using Gaos.Common;
using Gaos.Dbo;
using Gaos.Dbo.Model;
using Gaos.Mongo;
using Gaos.Routes.Model.DeviceJson;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Cryptography;
using static Gaos.Mongo.GameData;

namespace Gaos.Routes
{
    [System.Serializable]
    public class DeviceKeyRequest
    {
        public string? PublicKey { get; set; }
    }

    [System.Serializable]
    public class DeviceKeyResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? PublicKey { get; set; }

    }

    public static class DeviceRoutes
    {

        public static string CLASS_NAME = typeof(UserRoutes).Name;
        public static RouteGroupBuilder GroupDevice(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/key", async (DeviceKeyRequest request) =>
            {
                const string METHOD_NAME = "device/key";
                try
                {
                    DeviceKeyResponse response;

                    using var myEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                    //using var myEcdh = ECDiffieHellmanOpenSsl.Create(ECCurve.NamedCurves.nistP256);
                    byte[] myPubKeySpkiBytes = myEcdh.ExportSubjectPublicKeyInfo();

                    // Convert the public key to a base64 string
                    string muPubKey = System.Convert.ToBase64String(myPubKeySpkiBytes);



                    Log.Information($"@@@@@@@@@@@@@@@@@@@@@@@@@@@@ cp 400: PublicKey: {request.PublicKey}");

                    byte[] clientPubKeySpkiBytes = Convert.FromBase64String(request.PublicKey);
                    using var clientEcdhPub = ECDiffieHellman.Create();
                    clientEcdhPub.ImportSubjectPublicKeyInfo(clientPubKeySpkiBytes, out _);

                    //byte[] sharedSecret = myEcdh.DeriveKeyMaterial(clientEcdhPub.PublicKey);
                    byte[] sharedSecret = myEcdh.DeriveKeyFromHash(clientEcdhPub.PublicKey, HashAlgorithmName.SHA256);
                    // print sharedSecrent bytes
                    for (int i = 0; i < sharedSecret.Length; i++)
                    {
                        Log.Information($"@@@@@@@@@@@@@@@@@@@@@@@@@@@@ cp 500: sharedSecret[{i}]: {sharedSecret[i]}");
                    }

                    response = new DeviceKeyResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                        PublicKey = muPubKey,
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    DeviceKeyResponse response = new DeviceKeyResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });

            group.MapPost("/register", async (DeviceRegisterRequest deviceRegisterRequest, Db db, UserService userSerice, HttpContext context, GameData gameData) =>
            {
                const string METHOD_NAME = "device/register";
                try
                {
                    DeviceRegisterResponse response;
                    UserInterfaceColors userColors = null;

                    if (deviceRegisterRequest.Identification == null || deviceRegisterRequest.Identification.Trim().Length == 0)
                    {
                        response = new DeviceRegisterResponse
                        {
                            IsError = true,
                            ErrorMessage = "identification is empty",

                        };
                        return Results.Json(response);
                    }

                    string identification = deviceRegisterRequest.Identification;
                    bool isCookie = false;

                    // If the device identification equals 'n/a' use session cookie
                    if (deviceRegisterRequest.Identification == "n/a")
                    {
                        if (context.Items.ContainsKey(Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID))
                        {
                            int sessionId = (int)context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID];
                            Dbo.Model.Session session = await db.Session.FirstOrDefaultAsync(s => s.Id == sessionId);
                            if (session != null)
                            {
                                identification = session.Id.ToString();
                                isCookie = true;

                            }
                            else
                            {
                                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: no session found in database for sessionId: {sessionId}");
                                // report error - no device identification
                                response = new DeviceRegisterResponse
                                {
                                    IsError = true,
                                    ErrorMessage = "no device identification",
                                };
                                return Results.Json(response);
                            }
                        }
                        else
                        {
                            Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: no sessionId in http context");
                            // report error - no device identification
                            response = new DeviceRegisterResponse
                            {
                                IsError = true,
                                ErrorMessage = "no device identification",

                            };
                            return Results.Json(response);
                        }
                    }


                    string platformType = deviceRegisterRequest.PlatformType;

                    // log the device
                    Log.Information($"{CLASS_NAME}:{METHOD_NAME}: identification: {identification} (using cookie: {isCookie}), platformType: {platformType}, buildVersion: {deviceRegisterRequest.BuildVersion}");

                    if (deviceRegisterRequest.BuildVersion == null || deviceRegisterRequest.BuildVersion.Trim().Length == 0)
                    {
                        response = new DeviceRegisterResponse
                        {
                            IsError = true,
                            ErrorMessage = "buildVersion is empty",

                        };
                        return Results.Json(response);
                    }


                    BuildVersion buildVersion = await db.BuildVersion.FirstOrDefaultAsync(b => b.Version == deviceRegisterRequest.BuildVersion);
                    Device device = await db.Device.FirstOrDefaultAsync(d => d.Identification == identification && d.PlatformType == platformType);
                    (Dbo.Model.User?, Dbo.Model.JWT?) user_jwt = (null, null);

                    if (device == null)
                    {
                        device = new Device
                        {
                            Identification = identification,
                            PlatformType = platformType,
                            BuildVersionId = (buildVersion != null) ? buildVersion.Id : null,
                            BuildVersionReported = deviceRegisterRequest.BuildVersion,
                            RegisteredAt = System.DateTime.Now,
                        };
                        db.Device.Add(device);
                        await db.SaveChangesAsync();

                    }
                    else
                    {
                        user_jwt = userSerice.GetDeviceUser(device.Id);

                        device.Identification = identification;
                        device.PlatformType = platformType;
                        device.BuildVersionId = (buildVersion != null) ? buildVersion.Id : null;
                        device.BuildVersionReported = deviceRegisterRequest.BuildVersion;

                        if (user_jwt.Item1 != null)
                        {
                            userColors = await db.UserInterfaceColors.FirstOrDefaultAsync(c => c.UserId == user_jwt.Item1.Id);
                        }

                        await db.SaveChangesAsync();
                    }

                    response = new DeviceRegisterResponse
                    {
                        IsError = false,
                        DeviceId = device.Id,
                        Identification = device.Identification,
                        PlatformType = device.PlatformType.ToString(),
                        BuildVersion = (buildVersion != null) ? buildVersion.Version : "unknown",
                        User = user_jwt.Item1,
                        JWT = user_jwt.Item2,
                        UserInterfaceColors = userColors
                    };

                    if (response.User != null)
                    {
                        // don't send password hash and salt
                        response.User.PasswordHash = null;
                        response.User.PasswordSalt = null;
                    }

                    // Get user slots
                    if (response.User != null)
                    {
                        List<GetUserSlotIdsResult> userSlots = await gameData.GetUserSlotIdsAsync(response.User.Id);
                        DeviceRegisterResponseUserSlot[] deviceRegisterResponseUserSlots = new DeviceRegisterResponseUserSlot[userSlots.Count];
                        for (int i = 0; i < userSlots.Count; i++)
                        {
                            deviceRegisterResponseUserSlots[i] = new DeviceRegisterResponseUserSlot
                            {
                                MongoDocumentId = userSlots[i]._id,
                                MongoDocumentVersion = userSlots[i]._version,
                                SlotId = userSlots[i].SlotId,

                                UserName = userSlots[i].UserName,
                                Seconds = userSlots[i].Seconds,
                                Minutes = userSlots[i].Minutes,
                                Hours = userSlots[i].Hours,
                            };
                        }
                        response.UserSlots = deviceRegisterResponseUserSlots;
                    }

                    // now compute ecdh shared secret
                    if (deviceRegisterRequest.ecdhPublicKey != null)
                    {
                        var ecdhRersult = Gaos.Encryption.Ecdh.DeriveSharedSecret(deviceRegisterRequest.ecdhPublicKey);
                        response.ecdhContext = deviceRegisterRequest.ecdhContext;
                        response.ecdhPublicKey = ecdhRersult.pubKeyBase64;
                        context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_SHARED_CLIENT_SERVER_SECRET] = ecdhRersult.SharedSecret;
                    }

                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    DeviceRegisterResponse response = new DeviceRegisterResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });

            group.MapPost("/getRegistartion", async (DeviceGetRegistrationRequest deviceGetRegistrationRequest, Db db, HttpContext context) =>
            {
                const string METHOD_NAME = "device/getRegistartion";
                try
                {
                    DeviceGetRegistrationResponse response;
                    if (deviceGetRegistrationRequest.Identification == null || deviceGetRegistrationRequest.Identification.Trim().Length == 0)
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = true,
                            ErrorMessage = "deviceId is empty",

                        };
                        return Results.Json(response);
                    }

                    string identification = deviceGetRegistrationRequest.Identification;

                    // If the device identification equals 'n/a' use session cookie
                    if (deviceGetRegistrationRequest.Identification == "n/a")
                    {
                        if (context.Items.ContainsKey(Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID))
                        {
                            int sessionId = (int)context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID];
                            Dbo.Model.Session session = await db.Session.FirstOrDefaultAsync(s => s.Id == sessionId);
                            if (session != null)
                            {
                                identification = session.Id.ToString();
                            }
                        }
                        else
                        {
                            // report error - no device identification
                            response = new DeviceGetRegistrationResponse
                            {
                                IsError = true,
                                ErrorMessage = "no device identification",

                            };
                            return Results.Json(response);
                        }
                    }

                    string platformType = deviceGetRegistrationRequest.PlatformType;

                    Device device = await db.Device.FirstOrDefaultAsync(d => d.Identification == identification && d.PlatformType == platformType);
                    if (device == null)
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = false,
                            IsFound = false,
                        };
                        return Results.Json(response);
                    }
                    else
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = false,
                            IsFound = true,
                            DeviceId = device.Id,
                            Identification = device.Identification,
                            PlatformType = device.PlatformType.ToString(),
                            BuildVersion = device.BuildVersion.Version,
                        };
                        return Results.Json(response);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    DeviceGetRegistrationResponse response = new DeviceGetRegistrationResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });

            group.MapPost("/getRegistartionById", async (DeviceGetRegistrationByIdRequest deviceGetRegistrationByIdRequest, Db db) =>
            {
                const string METHOD_NAME = "device/getRegistartion";
                try
                {
                    DeviceGetRegistrationResponse response;

                    if (deviceGetRegistrationByIdRequest.DeviceId == 0)
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = true,
                            ErrorMessage = "deviceId is empty",

                        };
                        return Results.Json(response);
                    }

                    Device device = await db.Device.FirstOrDefaultAsync(d => d.Id == deviceGetRegistrationByIdRequest.DeviceId);
                    if (device == null)
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = false,
                            IsFound = false,
                        };
                        return Results.Json(response);
                    }
                    else
                    {
                        response = new DeviceGetRegistrationResponse
                        {
                            IsError = false,
                            IsFound = true,
                            DeviceId = device.Id,
                            Identification = device.Identification,
                            PlatformType = device.PlatformType.ToString(),
                            BuildVersion = device.BuildVersion.Version,
                        };
                        return Results.Json(response);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    DeviceGetRegistrationResponse response = new DeviceGetRegistrationResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });


            return group;

        }
    }
}
