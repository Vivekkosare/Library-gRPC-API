using Grpc.Core;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Google.Protobuf.WellKnownTypes;
using LibrarygRPCAPI.Protos;
using Library_gRPC_API.Models;

namespace LibraryAPI.IntegrationTests;

public class UserBorrowedBooksWithinTimeFrame_IntegrationTests
{
    [Fact]
    public async Task ReturnsBooksBorrowedByUserWithinTimeFrame()
    {
        // Arrange: Use a real MongoDB instance (or a test container if available)
        var mongoClient = new MongoClient("mongodb://localhost:27017");
        var db = mongoClient.GetDatabase("LibraryAPI_IntegrationTestDb");
        await db.DropCollectionAsync("Books");
        await db.DropCollectionAsync("BorrowedBooks");
        var booksCollection = db.GetCollection<Book>("Books");
        var borrowedBooksCollection = db.GetCollection<UserBorrowedBook>("BorrowedBooks");

        var book = new Book { Id = "book1", Title = "Integration Test Book", Author = "Author", NoOfPages = 100, TotalCopies = 5 };
        await booksCollection.InsertOneAsync(book);

        var borrowDate = DateTime.UtcNow.AddDays(-2);
        var borrowedBook = new UserBorrowedBook
        {
            Id = "borrow1",
            BookId = "book1",
            UserId = "user1",
            BorrowDate = borrowDate,
            DueDate = borrowDate.AddDays(7)
        };
        await borrowedBooksCollection.InsertOneAsync(borrowedBook);

        var logger = new LoggerFactory().CreateLogger<Library_gRPC_API.Services.UserLibraryService>();
        var service = new Library_gRPC_API.Services.UserLibraryService(db, logger);
        var request = new UserBorrowedBooksWithinTimeFrameRequest
        {
            UserId = "user1",
            StartTime = Timestamp.FromDateTime(borrowDate.AddDays(-1).ToUniversalTime()),
            EndTime = Timestamp.FromDateTime(borrowDate.AddDays(1).ToUniversalTime())
        };

        // Act
        var result = await service.GetBooksBorrowedByUserWithinTimeFrame(request, TestServerCallContext.Create());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1", result.UserId);
        Assert.Single(result.Books);
        Assert.Equal("book1", result.Books[0].Id);
        Assert.Equal("Integration Test Book", result.Books[0].Title);
    }

    // Minimal ServerCallContext for integration test
    private class TestServerCallContext : ServerCallContext
    {
        public static ServerCallContext Create() => new TestServerCallContext();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null!;
        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "localhost";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; } = null;
        protected override AuthContext AuthContextCore => null!;
    }
}
