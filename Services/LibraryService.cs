using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibraryAPI.Models;
using LibraryAPI.Protos;
using MongoDB.Driver;

namespace LibraryAPI.Services
{
    public class LibraryService(IMongoDatabase _db) : LibraryAPI.Protos.LibraryService.LibraryServiceBase
    {
        public override async Task<BookList> GetAllBooks(Empty request, ServerCallContext context)
        {
            var booksCollection = _db.GetCollection<Book>("Books");
            var cursor = await booksCollection.FindAsync(FilterDefinition<Book>.Empty);
            var books = await cursor.ToListAsync();
            var bookList = new BookList();
            foreach (var book in books)
            {
                bookList.Books.Add(book);
            }
            return bookList;
        }
        // public override Task<Book> GetBookById(BookId request, ServerCallContext context)
        // {
        //     return base.GetBookById(request, context);
        // }
        public override async Task<BookCopiesStatus> GetBookCopiesStatus(BookCopiesRequest request, ServerCallContext context)
        {
            var book = await _db.GetCollection<Book>("Books")
                        .Find(b => b.Id == request.BookId).FirstOrDefaultAsync();
            if (book is null)
            {
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
        // public override Task<BookReadingRate> GetBookReadingRate(BookResadingRateRequest request, ServerCallContext context)
        // {
        //     return base.GetBookReadingRate(request, context);
        // }
        public override async Task<UserBorrowedBooks> GetBooksBorrowedByUserWithinTimeFrame(UserBorrowedBooksWithinTimeFrameRequest request, ServerCallContext context)
        {
            try
            {
                var startTime = request.StartTime.ToDateTime();
                var endTime = request.EndTime.ToDateTime();
                var userId = request.UserId;
                var borrowedBooks = _db.GetCollection<UserBorrowedBook>("BorrowedBooks");
                var filter = Builders<UserBorrowedBook>.Filter.And(
                    Builders<UserBorrowedBook>.Filter.Eq(b => b.UserId, userId),
                    Builders<UserBorrowedBook>.Filter.Gte(b => b.BorrowDate, startTime),
                    Builders<UserBorrowedBook>.Filter.Lte(b => b.BorrowDate, endTime)
                );
                var userBorrowedBooks = await borrowedBooks
                    .Find(filter).ToListAsync();

                var books = new HashSet<Book>();
                var booksCollection = _db.GetCollection<Book>("Books");
                foreach (var borrowedBook in userBorrowedBooks)
                {
                    var book = await booksCollection.Find(b => b.Id == borrowedBook.BookId).FirstOrDefaultAsync();
                    if (book != null)
                    {
                        books.Add(book);
                    }
                }
                return new UserBorrowedBooks
                {
                    UserId = userId,
                    Books = { books },
                    BorrowedBooksCount = books.Count,
                    StartTime = Timestamp.FromDateTime(startTime),
                    EndTime = Timestamp.FromDateTime(endTime)
                };

            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

        }
        public override async Task<MostBorrowedBooks> GetMostBorrowedBooks(TimeRangeRequest request, ServerCallContext context)
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

            // iterate through the aggregate result and fetch the book details and users who borrowed the book
            foreach (var book in aggregate)
            {
                var bookDetails = await bookCollection.Find(b => b.Id == book.BookId).FirstOrDefaultAsync();
                var users = await usersCollection.Find(u => book.Users.Contains(u.Id)).ToListAsync();
                if (bookDetails is not null && users is not null)
                {
                    var mostBorrowedBook = new MostBorrowedBook
                    {
                        Book = bookDetails,
                        BorrowCount = book.BorrowCount
                    };
                    mostBorrowedBook.Users.AddRange(users);
                    mostBorrowedBooks.MostBorrowedBooks_.Add(mostBorrowedBook);
                }
            }
            return mostBorrowedBooks;
        }
        public override async Task<OtherBooksBorrowedByUsers> GetOtherBooksBorrowedByUserWhoBorrowedThisBook(OtherBooksBorrowedByUserWhoBorrowedThisBookRequest request, ServerCallContext context)
        {
            //fetch the borrowed books list for a specific book Id
            var borrowedBooks = _db.GetCollection<UserBorrowedBook>("BorrowedBooks");
            var filter = await borrowedBooks
              .FindAsync(b => b.BookId == request.BookId);
            var borrowedBooksList = await filter.ToListAsync();

            //fetch the userIds of the users who borrowed that book with book Id
            var userIds = borrowedBooksList.Select(b => b.UserId).Distinct().ToList();

            //fetch users who borrowed this specific book with bookId in the request
            var userCollection = _db.GetCollection<User>("Users");
            var usersData = userCollection.Find(u => userIds.Contains(u.Id));
            var booksCollection = _db.GetCollection<Book>("Books");
            var borrowedBooksDetailList = new List<BorrowedBookDetail>();
            OtherBooksBorrowedByUsers otherBooks = new();
            foreach (var user in await usersData.ToListAsync())
            {

                var userBorrowedBooks = await (borrowedBooks.FindAsync(b => b.UserId == user.Id));
                foreach (var userBorrowedBook in await userBorrowedBooks.ToListAsync())
                {
                    var book = await booksCollection.Find(b => b.Id == userBorrowedBook.BookId).FirstOrDefaultAsync();
                    BorrowedBookDetail borrowedBookDetail = new()
                    {
                        BookId = userBorrowedBook.BookId,
                        BorrowDate = Timestamp.FromDateTime(userBorrowedBook.BorrowDate),
                        ReturnDate = userBorrowedBook.ReturnDate is not null ?
                            Timestamp.FromDateTime(userBorrowedBook.ReturnDate.Value) : null,
                        BookName = book.Title,
                        DueDate = Timestamp.FromDateTime(userBorrowedBook.DueDate)
                    };
                    borrowedBooksDetailList.Add(borrowedBookDetail);
                }
                OtherBooksBorrowedByUser otherBook = new()
                {
                    User = user,
                    BorrowedBooksDetailList = { borrowedBooksDetailList }
                };
                otherBooks.OtherBooksBorrowedByUsers_.Add(otherBook);
            }
            return otherBooks;
        }
        // public override Task<BooksBorrowedByUsersWithinTimeFrame> GetUsersBorrowedBooksWithinTimeFrame(BooksBorrowedByUsersWithinTimeFrameRequest request, ServerCallContext context)
        // {
        //     return base.GetUsersBorrowedBooksWithinTimeFrame(request, context);
        // }
    }
}