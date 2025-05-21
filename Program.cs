using LibraryAPI.Services;
using LibraryAPI.Utils;
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

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.MapGrpcService<LibraryService>();

app.Run();
