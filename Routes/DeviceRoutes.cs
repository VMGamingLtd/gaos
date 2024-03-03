#pragma warning disable 8600, 8602, 8604, 8605

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.DeviceJson;
using Gaos.Dbo.Model;
using Gaos.Common;
using Gaos.Mongo;
using static Gaos.Mongo.GameData;

namespace Gaos.Routes
{

    public static class DeviceRoutes
    {

        public static string CLASS_NAME = typeof(UserRoutes).Name;
        public static RouteGroupBuilder GroupDevice(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/register", async (DeviceRegisterRequest deviceRegisterRequest, Db db, UserService userSerice, HttpContext context, GameData gameData) =>
            {
                const string METHOD_NAME = "device/register";
                try
                {
                    DeviceRegisterResponse response;


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
                        }
                        else
                        {
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

                    } else {
                        user_jwt  = userSerice.GetDeviceUser(device.Id);

                        device.Identification = identification;
                        device.PlatformType = platformType;
                        device.BuildVersionId = (buildVersion != null) ? buildVersion.Id : null;
                        device.BuildVersionReported = deviceRegisterRequest.BuildVersion;

                        await db.SaveChangesAsync();
                    }




                    response = new DeviceRegisterResponse
                    {
                        IsError = false,
                        DeviceId = device.Id,
                        Identification = device.Identification,
                        PlatformType = device.PlatformType.ToString(),
                        BuildVersion = ( buildVersion != null ) ? buildVersion.Version : "unknown",
                        User = user_jwt.Item1,
                        JWT = user_jwt.Item2,
                    };

                    if (response.User != null)
                    {
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
