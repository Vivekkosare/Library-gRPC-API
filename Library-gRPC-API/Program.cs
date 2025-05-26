using Library_gRPC_API.Services;
using Library_gRPC_API.Utils;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var connectionString = builder.Configuration["MongoDB:ConnectionString"];
var database = builder.Configuration["MongoDB:Database"];
var client = new MongoClient(connectionString);
var mongoDatabase = client.GetDatabase(database);

builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// Seed the database with initial data
await Seed.SeedData(mongoDatabase);

var app = builder.Build();

app.MapGrpcService<UserLibraryService>();

app.Run();

public partial class Program { }
