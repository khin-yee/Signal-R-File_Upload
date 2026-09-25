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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost7272", policy =>
    {
        policy.WithOrigins("https://localhost:7272")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<HangFireMongoOptions>(config.GetSection("HangfireMongoOptions"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<HangFireMongoOptions>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// Add Hangfire Config
builder.Services.AddHangfire((sp, hangfireConfig) =>
{
    var options = sp.GetRequiredService<IOptions<HangFireMongoOptions>>().Value;
    hangfireConfig.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseMongoStorage(options.ConnectionString, options.DatabaseName, new MongoStorageOptions
          {
              MigrationOptions = new MongoMigrationOptions
              {
                  MigrationStrategy = new MigrateMongoMigrationStrategy(),
                  BackupStrategy = new CollectionMongoBackupStrategy()
              },
              Prefix = "hangfire",
              CheckConnection = true
          });
});
builder.Services.AddHangfireServer();

builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<SignalRHub>();
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
app.UseCors("AllowLocalhost7272");
app.Run();
