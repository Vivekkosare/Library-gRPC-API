using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using Mongo2Go;
using Microsoft.Extensions.DependencyInjection;
using LibrarygRPCAPI.Protos;
using Library_gRPC_API.Models;

namespace LibraryAPI.FunctionalTests;

public class LibraryService_FunctionalTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private GrpcChannel _channel = default!;
    private MongoDbRunner _mongoRunner = default!;
    private IMongoDatabase _testDb = default!;

    public LibraryService_FunctionalTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace MongoDB with a test instance
                _mongoRunner = MongoDbRunner.Start();
                var mongoClient = new MongoClient(_mongoRunner.ConnectionString);
                _testDb = mongoClient.GetDatabase("TestDb");
                services.AddSingleton<IMongoDatabase>(_testDb);
            });
        });
    }

    public async Task InitializeAsync()
    {
        var client = _factory.CreateDefaultClient();
        _channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions { HttpClient = client });

        // Seed test data
        var booksCollection = _testDb.GetCollection<Book>("Books");
        await booksCollection.InsertOneAsync(new Book
        {
            Id = "book1",
            Title = "Functional Test Book",
            Author = "Test Author",
            NoOfPages = 123,
            TotalCopies = 5
        });

        var borrowedBooksCollection = _testDb.GetCollection<UserBorrowedBook>("BorrowedBooks");
        await borrowedBooksCollection.InsertOneAsync(new UserBorrowedBook
        {
            Id = "borrow1",
            BookId = "book1",
            UserId = "user1",
            BorrowDate = System.DateTime.UtcNow.AddDays(-2),
            DueDate = System.DateTime.UtcNow.AddDays(5)
        });
    }

    public Task DisposeAsync()
    {
        _mongoRunner?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetBookCopiesStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var client = new LibrarygRPCAPI.Protos.LibraryService.LibraryServiceClient(_channel);


        // Act
        var response = await client.GetBookCopiesStatusAsync(new BookCopiesRequest { BookId = "book1" });

        // Assert
        Assert.Equal("book1", response.BookId);
        Assert.Equal(5, response.TotalCopies);
        Assert.Equal(1, response.BorrowedCopies);
        Assert.Equal(4, response.AvailableCopies);
    }
}
