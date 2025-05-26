using Microsoft.AspNetCore.Mvc.Testing;
using Grpc.Net.Client;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using LibrarygRPCAPI.Protos;
using Library_gRPC_API.Models;

namespace LibraryAPI.SystemTests
{
    public class OtherBooksBorrowedByUserWhoBorrowedThisBook_SystemTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly GrpcChannel _channel;
        private readonly LibraryService.LibraryServiceClient _client;

        public OtherBooksBorrowedByUserWhoBorrowedThisBook_SystemTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override MongoDB configuration for testing
                    services.Configure<Microsoft.Extensions.Configuration.IConfiguration>(config =>
                    {
                        config["MongoDB:ConnectionString"] = "mongodb://localhost:27017";
                        config["MongoDB:Database"] = "LibraryDB_SystemTest";
                    });
                });
            });

            var client = _factory.CreateClient();
            _channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
            {
                HttpClient = client
            });
            _client = new LibraryService.LibraryServiceClient(_channel);
        }

        [Fact]
        public async Task GetOtherBooksBorrowedByUserWhoBorrowedThisBook_ReturnsOtherBooksSuccessfully()
        {
            // Arrange - Seed test data
            await SeedTestData();

            var request = new OtherBooksBorrowedByUserWhoBorrowedThisBookRequest
            {
                BookId = "book1"
            };

            // Act - Make gRPC call through the public API
            var result = await _client.GetOtherBooksBorrowedByUserWhoBorrowedThisBookAsync(request);

            // Assert - Verify system behavior end-to-end
            Assert.NotNull(result);
            Assert.NotEmpty(result.OtherBooksBorrowedByUsers_);
            
            // Verify that we get books borrowed by users who also borrowed book1
            var firstUser = result.OtherBooksBorrowedByUsers_.First();
            Assert.NotNull(firstUser.User);
            Assert.NotEmpty(firstUser.BorrowedBooks);
            
            // Verify that the returned books are not the original book
            foreach (var userBorrow in result.OtherBooksBorrowedByUsers_)
            {
                foreach (var borrowHistory in userBorrow.BorrowedBooks)
                {
                    Assert.NotEqual("book1", borrowHistory.Book.Id);
                }
            }
        }

        private async Task SeedTestData()
        {
            // This is a true system test - we use the same database as the running application
            // but with a test-specific database name to avoid conflicts
            var mongoClient = new MongoClient("mongodb://localhost:27017");
            var database = mongoClient.GetDatabase("LibraryDB_SystemTest");

            // Clean up any existing test data
            await database.DropCollectionAsync("Books");
            await database.DropCollectionAsync("Users");
            await database.DropCollectionAsync("BorrowedBooks");

            // Seed books
            var booksCollection = database.GetCollection<Book>("Books");
            var books = new List<Book>
            {
                new Book { Id = "book1", Title = "Book 1", Author = "Author 1", NoOfPages = 200, TotalCopies = 5 },
                new Book { Id = "book2", Title = "Book 2", Author = "Author 2", NoOfPages = 300, TotalCopies = 3 },
                new Book { Id = "book3", Title = "Book 3", Author = "Author 3", NoOfPages = 250, TotalCopies = 4 }
            };
            await booksCollection.InsertManyAsync(books);

            // Seed users
            var usersCollection = database.GetCollection<User>("Users");
            var users = new List<User>
            {
                new User { Id = "user1", Name = "User 1", Email = "user1@test.com" },
                new User { Id = "user2", Name = "User 2", Email = "user2@test.com" },
                new User { Id = "user3", Name = "User 3", Email = "user3@test.com" }
            };
            await usersCollection.InsertManyAsync(users);

            // Seed borrowed books - users who borrowed book1 also borrowed other books
            var borrowedBooksCollection = database.GetCollection<UserBorrowedBook>("BorrowedBooks");
            var borrowedBooks = new List<UserBorrowedBook>
            {
                // user1 borrowed book1 and book2
                new UserBorrowedBook 
                { 
                    Id = "borrow1", 
                    BookId = "book1", 
                    UserId = "user1", 
                    BorrowDate = DateTime.UtcNow.AddDays(-10),
                    DueDate = DateTime.UtcNow.AddDays(-3)
                },
                new UserBorrowedBook 
                { 
                    Id = "borrow2", 
                    BookId = "book2", 
                    UserId = "user1", 
                    BorrowDate = DateTime.UtcNow.AddDays(-8),
                    DueDate = DateTime.UtcNow.AddDays(-1)
                },
                // user2 borrowed book1 and book3
                new UserBorrowedBook 
                { 
                    Id = "borrow3", 
                    BookId = "book1", 
                    UserId = "user2", 
                    BorrowDate = DateTime.UtcNow.AddDays(-15),
                    DueDate = DateTime.UtcNow.AddDays(-8)
                },
                new UserBorrowedBook 
                { 
                    Id = "borrow4", 
                    BookId = "book3", 
                    UserId = "user2", 
                    BorrowDate = DateTime.UtcNow.AddDays(-12),
                    DueDate = DateTime.UtcNow.AddDays(-5)
                },
                // user3 only borrowed book2 (should not appear in results for book1)
                new UserBorrowedBook 
                { 
                    Id = "borrow5", 
                    BookId = "book2", 
                    UserId = "user3", 
                    BorrowDate = DateTime.UtcNow.AddDays(-6),
                    DueDate = DateTime.UtcNow.AddDays(1)
                }
            };
            await borrowedBooksCollection.InsertManyAsync(borrowedBooks);
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
