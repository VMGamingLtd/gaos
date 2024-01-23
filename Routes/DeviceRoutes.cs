#pragma warning disable 8600, 8602, 8604

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.DeviceJson;
using Gaos.Dbo.Model;
using Gaos.Common;

namespace Gaos.Routes
{

    public static class DeviceRoutes
    {

        public static string CLASS_NAME = typeof(UserRoutes).Name;
        public static RouteGroupBuilder GroupDevice(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/register", async (DeviceRegisterRequest deviceRegisterRequest, Db db, UserService userSerice) =>
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

                    string platformType = deviceRegisterRequest.PlatformType;

                    // log the device
                    Log.Information($"{CLASS_NAME}:{METHOD_NAME}: identification: {deviceRegisterRequest.Identification}, platformType: {platformType}, buildVersion: {deviceRegisterRequest.BuildVersion}");

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
                    Device device = await db.Device.FirstOrDefaultAsync(d => d.Identification == deviceRegisterRequest.Identification && d.PlatformType == platformType);
                    (Dbo.Model.User?, Dbo.Model.JWT?) user_jwt = (null, null);

                    if (device == null)
                    {
                        device = new Device
                        {
                            Identification = deviceRegisterRequest.Identification,
                            PlatformType = platformType,
                            BuildVersionId = (buildVersion != null) ? buildVersion.Id : null,
                            BuildVersionReported = deviceRegisterRequest.BuildVersion,
                            RegisteredAt = System.DateTime.Now,
                        };
                        db.Device.Add(device);
                        await db.SaveChangesAsync();

                    } else {
                        user_jwt  = userSerice.GetDeviceUser(device.Id);

                        device.Identification = deviceRegisterRequest.Identification;
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

            group.MapPost("/getRegistartion", async (DeviceGetRegistrationRequest deviceGetRegistrationRequest, Db db) =>
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

                    string platformType = deviceGetRegistrationRequest.PlatformType;

                    Device device = await db.Device.FirstOrDefaultAsync(d => d.Identification == deviceGetRegistrationRequest.Identification && d.PlatformType == platformType);
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
