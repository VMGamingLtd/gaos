#pragma warning disable 8600, 8602, 8604, 0162

using Gaos.Dbo;
using Gaos.Middleware;
using Gaos.Routes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;

if (false)
{
    gaos.Tests.Test.TestAll();
    Console.WriteLine("Press any key to exit program");
    Console.ReadKey();
    Environment.Exit(0);
}



var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();



if (builder.Configuration["db_connection_string"] == null)
{
    throw new Exception("missing configuration value: db_connection_string");

}
if (builder.Configuration["db_user"] == null)
{
    throw new Exception("missing configuration value: db_user");

}
if (builder.Configuration["db_password"] == null)
{
    throw new Exception("missing configuration value: db_password");

}


if (builder.Configuration["db_major_version"] == null)
{
    throw new Exception("missing configuration value: db_major_version");
}
var dbMajorVersion = builder.Configuration.GetValue<int>("db_major_version");

if (builder.Configuration["db_minor_version"] == null)
{
    throw new Exception("missing configuration value: db_minor_version");
}
var dbMinorVersion = builder.Configuration.GetValue<int>("db_minor_version");

var dbServerVersion = new MariaDbServerVersion(new Version(dbMajorVersion, dbMinorVersion));

var dbConnectionString = builder.Configuration.GetValue<string>("db_connection_string");
dbConnectionString += $";user={builder.Configuration.GetValue<string>("db_user")}";
dbConnectionString += $";password={Gaos.Encryption.EncryptionHelper.Decrypt(builder.Configuration.GetValue<string>("db_password"))}";

if (builder.Environment.EnvironmentName == "Test")
{
    var section = builder.Configuration.GetSection("Kestrel:Certificates:Default");
    var encryptedPassword = section.GetValue<string>("Password");
    var decryptedPassword = Gaos.Encryption.EncryptionHelper.Decrypt(encryptedPassword);
    builder.Configuration["Kestrel:Certificates:Default:Password"] = decryptedPassword;
}

builder.Services.AddDbContext<Db>(opt => {


        opt.UseMySql(dbConnectionString, dbServerVersion)
        //.LogTo(Console.WriteLine, LogLevel.Information)
        .LogTo(Console.WriteLine, LogLevel.Warning)
        //.LogTo(Console.WriteLine, LogLevel.Debug)
        //.EnableSensitiveDataLogging()
        .EnableDetailedErrors();
    }
);
builder.Services.AddMySqlDataSource(dbConnectionString);


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    //.MinimumLevel.Warning()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Debug()
    .CreateLogger();
builder.Host.UseSerilog();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<Db>();

if (builder.Configuration["guest_names_file_path"] == null)
{
    throw new Exception("missing configuration value: guest_names_file_path");
}
string guestNamesFilePath = builder.Configuration.GetValue<string>("guest_names_file_path");

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<Gaos.Auth.TokenService>(provider =>
{
    MySqlDataSource dataSource = provider.GetService<MySqlDataSource>();
    return new Gaos.Auth.TokenService(builder.Configuration, dataSource);
});

builder.Services.AddScoped<Gaos.Common.GuestService>(provider =>
{
    Db db = provider.GetService<Db>();
    return new Gaos.Common.GuestService(db, guestNamesFilePath);
});

builder.Services.AddScoped<Gaos.Common.UserService>(provider =>
{
    HttpContext context = provider.GetService<IHttpContextAccessor>()?.HttpContext;
    Gaos.Auth.TokenService tokenService = provider.GetService<Gaos.Auth.TokenService>();
    Gaos.Dbo.Db db = provider.GetService<Gaos.Dbo.Db>();
    MySqlConnection dbConn = provider.GetService<MySqlConnection>();
    MySqlDataSource dataSource = provider.GetService<MySqlDataSource>();
    return new Gaos.Common.UserService(context, tokenService, db, dataSource);
});

builder.Services.AddScoped<Gaos.Common.WebsiteService>(provider =>
{
    Gaos.Dbo.Db db = provider.GetService<Gaos.Dbo.Db>();
    MySqlDataSource dataSource = provider.GetService<MySqlDataSource>();
    return new Gaos.Common.WebsiteService(db, dataSource);
});

builder.Services.AddScoped<Gaos.Mongo.MongoService>(provider =>
{
    return new Gaos.Mongo.MongoService(builder.Configuration);
});

builder.Services.AddScoped<Gaos.Mongo.GameData>(provider =>
{
    Gaos.Mongo.MongoService mongoService = provider.GetService<Gaos.Mongo.MongoService>();
    return new Gaos.Mongo.GameData(mongoService);
});

builder.Services.AddScoped<Gaos.Mongo.GroupData>(provider =>
{
    Gaos.Mongo.MongoService mongoService = provider.GetService<Gaos.Mongo.MongoService>();
    Gaos.Mongo.GameData gameDataService = new Gaos.Mongo.GameData(mongoService);
    return new Gaos.Mongo.GroupData(mongoService, gameDataService, builder.Configuration);
});

builder.Services.AddScoped<Gaos.Templates.TemplateService>(provider =>
{
    Gaos.Dbo.Db db = provider.GetService<Gaos.Dbo.Db>();
    Gaos.Lang.LanguageService languageService = provider.GetService<Gaos.Lang.LanguageService>();
    return new Gaos.Templates.TemplateService(builder.Configuration, db, languageService);
});

builder.Services.AddScoped<Gaos.Lang.LanguageService>(provider =>
{
    return new Gaos.Lang.LanguageService();
});

builder.Services.AddScoped<Gaos.Email.EmailService>(provider =>
{
    Gaos.Lang.LanguageService languageService = provider.GetService<Gaos.Lang.LanguageService>();
    Gaos.Templates.TemplateService templateService = provider.GetService<Gaos.Templates.TemplateService>();
    return new Gaos.Email.EmailService(builder.Configuration, languageService, templateService);
});

builder.Services.AddScoped<Gaos.UserVerificationCode.UserVerificationCodeService>(provider =>
{
    Gaos.Dbo.Db db = provider.GetService<Gaos.Dbo.Db>();
    return new Gaos.UserVerificationCode.UserVerificationCodeService(db);
});


// Set the JSON serializer options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});


// Change this from AddHostedService to AddSingleton
builder.Services.AddSingleton<Gaos.wsrv.WsrConnectionPoolService>(provider =>
{
    Gaos.wsrv.WsrConnectionPoolService wsrConnectionPoolService = new Gaos.wsrv.WsrConnectionPoolService(builder.Configuration);
    return wsrConnectionPoolService;
});

// Add this line to register the hosted service
builder.Services.AddHostedService<Gaos.wsrv.WsrConnectionPoolService>( provider => {
    Gaos.wsrv.WsrConnectionPoolService wsrConnectionPoolService = provider.GetRequiredService<Gaos.wsrv.WsrConnectionPoolService>();
    return wsrConnectionPoolService;
});

builder.Services.AddScoped<Gaos.wsrv.messages.GroupBroadcastService>(provider =>
{
    Gaos.wsrv.WsrConnectionPoolService connectionPool = provider.GetRequiredService<Gaos.wsrv.WsrConnectionPoolService>();
    return new Gaos.wsrv.messages.GroupBroadcastService(connectionPool);
});


var app = builder.Build();


app.UseMiddleware<CookieMiddleware>();
//app.UseWebSockets();
//app.UseMiddleware<WebSocketMiddleware>();
app.UseMiddleware<AuthMiddleware>();


app.Map("/", (IConfiguration configuration) =>
{
    return Results.Ok("hello!");
});

app.Map("/test", (MySqlDataSource dataSource) =>
{
    {
        using var connection = dataSource.OpenConnection();

        using var command_rm = connection.CreateCommand();
        command_rm.CommandText = "DELETE FROM Jwt WHERE DeviceId = @deviceId";
        command_rm.Parameters.AddWithValue("@deviceId", 1);
        command_rm.ExecuteNonQuery();


        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Jwt (Token, UserId, DeviceId, CreatedAt, ExpiresAt)
            VALUES (@token, @userId, @deviceId, NOW(), DATE_ADD(NOW(), INTERVAL @validitySeconds SECOND));
            SELECT LAST_INSERT_ID()";

        command.Parameters.AddWithValue("@token", "bla bla");
        command.Parameters.AddWithValue("@userId", 1);
        command.Parameters.AddWithValue("@deviceId", 1);
        command.Parameters.AddWithValue("@validitySeconds", 10);

        var result = command.ExecuteScalar();
        int jwtId;
        if (result == null)
        {
            Log.Error($"Could not insert guest JWT into database: result is null");
            throw new Exception("Could not insert guest JWT into database: result is null");
        }
        else
        if (!int.TryParse(result.ToString(), out jwtId))
        {
            Log.Error($"Could not insert guest JWT into database: could not parse result to int");
            throw new Exception("Could not insert guest JWT into database: could not parse result to int");
        }
    }
    return Results.Ok("hello!");
});

app.MapGroup("/user").GroupUser();
app.MapGroup("/device").GroupDevice();
app.MapGroup("/api").GroupApi();
app.MapGroup("/api1").GroupApi1();
app.MapGroup("/api/gameData").GameData();
app.MapGroup("/api/groupData").GroupData();
app.MapGroup("/api/groupData1").GroupData1();
app.MapGroup("/api/chatRoom").GroupChatRoom();
app.MapGroup("/api/groups").GroupFriends();

app.Run();
