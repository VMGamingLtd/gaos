#pragma warning disable 8600, 8602, 8604, 0162

using Microsoft.EntityFrameworkCore;
using Gaos.Dbo;
using Gaos.Routes;
using Serilog;
using Gaos.Middleware;
using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;



var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();


if (builder.Configuration["db_connection_string"] == null)
{
    throw new Exception("missing configuration value: db_connection_string");

}
var dbConnectionString = builder.Configuration.GetValue<string>("db_connection_string");

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


builder.Services.AddDbContext<Db>(opt => 
    opt.UseMySql(dbConnectionString, dbServerVersion)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors()
);


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.Debug()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<Db>();

if (builder.Configuration["guest_names_file_path"] == null)
{
    throw new Exception("missing configuration value: guest_names_file_path");
}
string guestNamesFilePath = builder.Configuration.GetValue<string>("guest_names_file_path");

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<Gaos.Auth.TokenService>(provider => {
    Db db = provider.GetService<Db>();
    return new Gaos.Auth.TokenService(builder.Configuration,  db);
}); 

builder.Services.AddScoped<Gaos.Common.GuestService>(provider => {
    Db db = provider.GetService<Db>();
    return new Gaos.Common.GuestService(db, guestNamesFilePath);
}); 

builder.Services.AddScoped<Gaos.Common.UserService>(provider => {
    HttpContext context = provider.GetService<IHttpContextAccessor>()?.HttpContext;
    Gaos.Auth.TokenService tokenService = provider.GetService<Gaos.Auth.TokenService>();
    Gaos.Dbo.Db db = provider.GetService<Gaos.Dbo.Db>();
    return new Gaos.Common.UserService(context, tokenService, db);
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


var app = builder.Build();


app.UseMiddleware<CookieMiddleware>();
app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();
app.UseMiddleware<AuthMiddleware>();


app.Map("/", (IConfiguration configuration) => {
    return Results.Ok("hello!");
});
app.MapGroup("/user").GroupUser();
app.MapGroup("/device").GroupDevice();
app.MapGroup("/api").GroupApi();
app.MapGroup("/api/gameData").GroupGameData();
app.MapGroup("/api/chatRoom").GroupChatRoom();
app.MapGroup("/api/friends").GroupFriends();
app.Run();