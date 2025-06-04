using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SignalRTest.Api;
using SignalRTest.Service;
using SignalRTest.Service.SignalRClient;
using SIgnalRTest.Domain.IServices;

var builder = WebApplication.CreateBuilder(args);
IConfiguration config = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<HangFireMongoOptions>(config.GetSection("HangfireMongoOptions"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<HangFireMongoOptions>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// Add Hangfire Config
builder.Services.AddHangfire((sp, config) =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IOptions<HangFireMongoOptions>>().Value;
    var opt = new MongoStorageOptions
    {
        //CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
        MigrationOptions = new MongoMigrationOptions
        {
            MigrationStrategy = new DropMongoMigrationStrategy(),
            BackupStrategy = new CollectionMongoBackupStrategy(),
        },
        SupportsCappedCollection = false,
        CheckConnection = false,

    };
    config.UseMongoStorage(client, settings.DatabaseName, opt);
});
builder.Services.AddHangfireServer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddScoped<ISignalRService, SignalRService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapHub<SignalRHub>("/signalR");
app.MapControllers();

app.Run();
