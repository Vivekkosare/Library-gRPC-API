using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibraryAPI.Extensions;
using LibraryAPI.Models;
using LibraryAPI.Protos;
using LibraryAPI.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace LibraryAPI.UnitTests;

public class LibraryService_UnitTests(LibraryServiceFixture _fixture) : IClassFixture<LibraryServiceFixture>
{
    [Fact]
    public async Task GetBookReadingRate_ReturnsRates_WhenBookAndBorrowedBooksExist()
    {
        // Arrange
        var mockDb = _fixture.MockDb;
        var mockLogger = _fixture.MockLogger;
        var bookId = "book1";
        var userId = "user1";
        var book = new Book { Id = bookId, NoOfPages = 100 };
        var booksCursor = TestHelpers.MockCursor(new List<Book> { book });
        var mockBooksCollection = new Mock<IMongoCollection<Book>>();

        mockBooksCollection.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<Book>>(),
            It.IsAny<FindOptions<Book, Book>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(booksCursor);
        mockDb.Setup(x => x.GetCollection<Book>("Books", null)).Returns(mockBooksCollection.Object);

        var borrowDate = DateTime.UtcNow.AddDays(-5);
        var returnDate = DateTime.UtcNow;
        var borrowedBooks = new List<BorrowedBookEntity>
            {
                new BorrowedBookEntity
                {
                    Id = "borrowed1",
                    BookId = bookId,
                    UserId = userId,
                    BorrowDate = borrowDate,
                    ReturnDate = returnDate,
                    DueDate = borrowDate.AddDays(7)
                }
            };
        var borrowedBooksCursor = TestHelpers.MockCursor(borrowedBooks);
        var mockBorrowedBooksCollection = new Mock<IMongoCollection<BorrowedBookEntity>>();

        mockBorrowedBooksCollection.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<BorrowedBookEntity>>(),
            It.IsAny<FindOptions<BorrowedBookEntity, BorrowedBookEntity>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(borrowedBooksCursor);
        mockDb.Setup(x => x.GetCollection<BorrowedBookEntity>("BorrowedBooks", null)).Returns(mockBorrowedBooksCollection.Object);

        var service = new Services.LibraryService(mockDb.Object, mockLogger.Object);
        var request = new BookReadingRateRequest { BookId = bookId };

        // Act
        var result = await service.GetBookReadingRate(request, TestHelpers.TestServerCallContext.Create());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookId, result.Book.Id);
        Assert.Single(result.BookReadingRates);
        Assert.Equal(userId, result.BookReadingRates[0].UserId);
    }


}



