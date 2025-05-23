using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibraryAPI.Extensions;
using LibraryAPI.Models;
using LibraryAPI.Protos;
using MongoDB.Driver;

namespace LibraryAPI.Services
{
    public class LibraryService(IMongoDatabase _db,
    ILogger<LibraryService> _logger) : LibraryAPI.Protos.LibraryService.LibraryServiceBase
    {
        public override async Task<BookList> GetAllBooks(Empty request, ServerCallContext context)
        {
            var booksCollection = _db.GetCollection<Book>("Books");
            var books = await booksCollection.Find(FilterDefinition<Book>.Empty).ToListAsync();
            var bookList = new BookList();
            bookList.Books.AddRange(books);
            return bookList;
        }
        public override async Task<BookCopiesStatus> GetBookCopiesStatus(BookCopiesRequest request, ServerCallContext context)
        {
            try
            {
                var book = await _db.GetCollection<Book>("Books")
                        .Find(b => b.Id == request.BookId).FirstOrDefaultAsync();
                if (book is null)
                {
                    _logger.LogError($"Book not found for the given ID: {request.BookId}");
                    throw new RpcException(new Status(StatusCode.NotFound, $"Book not found for the given ID: {request.BookId}"));
                }
                var booksBorrowed = await _db.GetCollection<BorrowedBook>("BorrowedBooks")
                                .Aggregate()
                                .Match(b => b.BookId == request.BookId)
                                .Group(b => b.BookId, g => new
                                {
                                    BorrowedCount = g.Count()
                                })
                                .FirstOrDefaultAsync();
                var borrowedCopies = booksBorrowed?.BorrowedCount ?? 0;
                var availableCopies = book.TotalCopies - borrowedCopies;

                return new BookCopiesStatus
                {
                    BookId = request.BookId,
                    TotalCopies = book.TotalCopies,
                    BorrowedCopies = borrowedCopies,
                    AvailableCopies = availableCopies
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                throw;
            }

        }
        public override async Task<UserBorrowReadingRates> GetBookReadingRate(BookReadingRateRequest request, ServerCallContext context)
        {
            try
            {

                var book = await _db.GetCollection<Book>("Books")
                            .Find(b => b.Id == request.BookId).FirstOrDefaultAsync();
                if (book is null)
                {
                    _logger.LogError($"Book not found for the given ID: {request.BookId}");
                    throw new RpcException(new Status(StatusCode.NotFound, $"Book not found for the given ID: {request.BookId}"));
                }

                // Get all borrowed books for the given bookId where user returned the book
                var booksBorrowedAndReturned = await _db.GetCollection<BorrowedBookEntity>("BorrowedBooks")
                                .Find(b => b.BookId == request.BookId && b.ReturnDate != null)
                                .ToListAsync();

                //Group the borrowed books by userId
                var borrowRecordsByUser = booksBorrowedAndReturned
                                .GroupBy(b => new { b.UserId });

                List<BookReadingRate> bookReadingRates = new();

                foreach (var record in borrowRecordsByUser)
                {
                    foreach (var bookBorrowed in record)
                    {
                        int totalDays = 0;
                        // Calculate the total days between borrow date and return date
                        if (bookBorrowed.ReturnDate.HasValue)
                        {
                            totalDays = (int)(bookBorrowed.ReturnDate.Value - bookBorrowed.BorrowDate).TotalDays;
                        }
                        var bookReadingRate = new BookReadingRate
                        {
                            UserId = bookBorrowed.UserId,
                            ReadingRate = book.NoOfPages / totalDays,
                            BorrowDate = Timestamp.FromDateTime(bookBorrowed.BorrowDate),
                            ReturnDate = bookBorrowed.ReturnDate != null ? Timestamp.FromDateTime(bookBorrowed.ReturnDate.Value) : null,
                        };
                        bookReadingRates.Add(bookReadingRate);
                    }
                }
                ;
                return new UserBorrowReadingRates
                {
                    Book = book,
                    BookReadingRates = { bookReadingRates }
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                throw;
            }

        }
        public override async Task<UserBorrowedBooks> GetBooksBorrowedByUserWithinTimeFrame(UserBorrowedBooksWithinTimeFrameRequest request, ServerCallContext context)
        {
            try
            {
                var startTime = request.StartTime.ToDateTime();
                var endTime = request.EndTime.ToDateTime();
                var userId = request.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError($"User ID is null or empty");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "User ID is null or empty"));
                }
                if (startTime == default || endTime == default)
                {
                    _logger.LogError($"Start time or end time is not valid");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Start time or end time is not valid"));
                }
                if (startTime > endTime)
                {
                    _logger.LogError($"Start time cannot be greater than end time");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Start time cannot be greater than end time"));
                }

                var borrowedBooks = _db.GetCollection<UserBorrowedBook>("BorrowedBooks");
                var filter = Builders<UserBorrowedBook>.Filter.And(
                    Builders<UserBorrowedBook>.Filter.Eq(b => b.UserId, userId),
                    Builders<UserBorrowedBook>.Filter.Gte(b => b.BorrowDate, startTime),
                    Builders<UserBorrowedBook>.Filter.Lte(b => b.BorrowDate, endTime)
                );
                var userBorrowedBooks = await borrowedBooks
                    .Find(filter).ToListAsync();

                if (userBorrowedBooks.Count == 0)
                {
                    _logger.LogError($"No borrowed books found for the given user ID: {userId}");
                    throw new RpcException(new Status(StatusCode.NotFound, $"No borrowed books found for the given user ID: {userId}"));
                }

                var booksSet = new HashSet<Book>();
                var booksCollection = _db.GetCollection<Book>("Books");

                // Get all books with details borrowed by the user within the time frame
                var books = await booksCollection
                    .Find(b => userBorrowedBooks.Select(bb => bb.BookId).Contains(b.Id))
                    .ToListAsync();

                var distinctBooks = books.Distinct().ToList();

                return new UserBorrowedBooks
                {
                    UserId = userId,
                    Books = { distinctBooks },
                    BorrowedBooksCount = books.Count,
                    StartTime = Timestamp.FromDateTime(startTime),
                    EndTime = Timestamp.FromDateTime(endTime)
                };

            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                throw;
            }

        }
        public override async Task<MostBorrowedBooks> GetMostBorrowedBooks(TimeRangeRequest request, ServerCallContext context)
        {
            try
            {
                //fetch borrowed books, books and users collections
                var borrowedBooks = _db.GetCollection<UserBorrowedBook>("BorrowedBooks");
                var bookCollection = _db.GetCollection<Book>("Books");
                var usersCollection = _db.GetCollection<User>("Users");


                var filter = Builders<UserBorrowedBook>.Filter.Empty;
                // Check if the request has a start time and end time
                if (request.StartTime is not null && request.EndTime is not null)
                {
                    filter = Builders<UserBorrowedBook>.Filter.And(
                            Builders<UserBorrowedBook>.Filter.Gte(b => b.BorrowDate, request.StartTime.ToDateTime()),
                            Builders<UserBorrowedBook>.Filter.Lte(b => b.BorrowDate, request.EndTime.ToDateTime()));
                }

                // get aggregate by grouping by bookId and fetching the borrow count along with the users who borrowed the book
                var aggregate = await borrowedBooks.Aggregate()
                        .Match(filter)
                        .Group(b => b.BookId, g => new
                        {
                            BookId = g.Key,
                            // BorrowCount = g.Sum(b => 1)
                            BorrowCount = g.Count(),
                            Users = g.Select(b => b.UserId).Distinct().ToList()
                        })
                        .SortByDescending(g => g.BorrowCount)
                        .ToListAsync();

                var mostBorrowedBooks = new MostBorrowedBooks();

                var bookIds = aggregate.Select(b => b.BookId).ToList();
                var books = await bookCollection
                    .Find(b => bookIds.Contains(b.Id))
                    .ToListAsync();
                var bookDict = books.ToFrozenDictionary(b => b.Id);

                var userIds = aggregate.SelectMany(b => b.Users).Distinct().ToList();
                var usersThoseBorrowedBooks = await usersCollection
                    .Find(u => userIds.Contains(u.Id))
                    .ToListAsync();
                var userDict = usersThoseBorrowedBooks.ToFrozenDictionary(u => u.Id);

                // iterate through the aggregate result and fetch the book details and users who borrowed the book
                foreach (var agr in aggregate)
                {
                    var bookDetails = bookDict.TryGetValue(agr.BookId, out var bookDetail) ? bookDetail : null;
                    if (bookDetails is null)
                        continue;

                    var userSet = new HashSet<string>(agr.Users);
                    var users = userDict.Where(u => userSet.Contains(u.Key)).Select(u => u.Value).ToList();

                    if (users.Count > 0)
                    {
                        var mostBorrowedBook = new MostBorrowedBook
                        {
                            Book = bookDetails,
                            BorrowCount = agr.BorrowCount
                        };
                        mostBorrowedBook.Users.AddRange(users);
                        mostBorrowedBooks.MostBorrowedBooks_.Add(mostBorrowedBook);
                    }
                }
                return mostBorrowedBooks;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                throw;
            }

        }
        public override async Task<OtherBooksBorrowedByUsers> GetOtherBooksBorrowedByUserWhoBorrowedThisBook(OtherBooksBorrowedByUserWhoBorrowedThisBookRequest request, ServerCallContext context)
        {
            try
            {
                var borrowedBooks = _db.GetCollection<UserBorrowedBook>("BorrowedBooks");
                var userCollection = _db.GetCollection<User>("Users");
                var booksCollection = _db.GetCollection<Book>("Books");

                // Get all userIds who borrowed the given book
                var userIds = await borrowedBooks
                    .DistinctAsync<string>("UserId", Builders<UserBorrowedBook>.Filter.Eq(b => b.BookId, request.BookId))
                    .Result
                    .ToListAsync();

                // Get all users from the user collection
                // who borrowed the given book
                var users = await userCollection
                    .Find(u => userIds.Contains(u.Id))
                    .ToListAsync();
                FrozenDictionary<string, User> userDict = users.ToFrozenDictionary(u => u.Id);

                // Get all books in books collection excluding the original bookId
                var books = await booksCollection
                    .Find(b => b.Id != request.BookId)
                    .ToListAsync();
                FrozenDictionary<string, Book> booksDict = books.ToFrozenDictionary(b => b.Id);

                // Get all books borrowed by these users, excluding the original bookId
                var allUsersBorrowedBooks = await borrowedBooks
                    .Find(bb => userIds.Contains(bb.UserId) && bb.BookId != request.BookId)
                    .ToListAsync();


                var response = new OtherBooksBorrowedByUsers();
                var resultBag = new ConcurrentBag<OtherBooksBorrowedByUser>();


                Parallel.ForEach(userIds, userId =>
                {
                    if (!userDict.TryGetValue(userId, out var user) || user == null)
                        return;

                    // Get all books borrowed by this user
                    var userBorrowedBooks = allUsersBorrowedBooks
                        .Where(bb => bb.UserId == userId);

                    // Group by BookId
                    var grouped = userBorrowedBooks.GroupBy(b => b.BookId);
                    var borrowHistoryList = new List<BookBorrowHistory>();

                    foreach (var group in grouped)
                    {
                        if (!booksDict.TryGetValue(group.Key, out var book) || book == null)
                            return;

                        var details = group.Select(b => new BorrowedBookDetail
                        {
                            UserId = b.UserId,
                            BorrowDate = Timestamp.FromDateTime(b.BorrowDate),
                            ReturnDate = b.ReturnDate != null ? Timestamp.FromDateTime(b.ReturnDate.Value) : null,
                            DueDate = Timestamp.FromDateTime(b.DueDate)
                        }).ToList();

                        if (details.Count > 0)
                        {
                            var borrowHistory = new BookBorrowHistory
                            {
                                Book = book,
                                BorrowedBooksDetailList
                                 = { details }

                            };
                            borrowHistoryList.Add(borrowHistory);
                        }
                    }

                    if (borrowHistoryList.Count == 0)
                        return;
                    var otherBook = new OtherBooksBorrowedByUser
                    {
                        User = user,
                        BorrowedBooks = { borrowHistoryList }
                    };
                    resultBag.Add(otherBook);
                });

                response.OtherBooksBorrowedByUsers_.AddRange(resultBag);
                return response;
            }

            catch (System.Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

        }

    }
    // public override Task<BooksBorrowedByUsersWithinTimeFrame> GetUsersBorrowedBooksWithinTimeFrame(BooksBorrowedByUsersWithinTimeFrameRequest request, ServerCallContext context)
    // {
    //     return base.GetUsersBorrowedBooksWithinTimeFrame(request, context);
    // }
}
