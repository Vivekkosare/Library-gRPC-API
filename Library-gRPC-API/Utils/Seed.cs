using Google.Protobuf.WellKnownTypes;
using Library_gRPC_API.Extensions;
using Library_gRPC_API.Models;
using LibrarygRPCAPI.Protos;
using MongoDB.Driver;

namespace Library_gRPC_API.Utils;

public static class Seed
{
    public static async Task SeedData(IMongoDatabase database)
        {
            var booksCollection = database.GetCollection<Book>("Books");
            var usersCollection = database.GetCollection<User>("Users");

            if (booksCollection.CountDocuments(FilterDefinition<Book>.Empty) == 0)
            {
                var books = new List<Book>
                {
                    new Book { Id="book1", Title = "Book 1", Author = "Author 1", Genre = "Action", PublicationYear = 2020 },
                    new Book { Id="book2", Title = "Book 2", Author = "Author 2", Genre = "Thriller", PublicationYear = 2021 },
                    new Book { Id="book3", Title = "Book 3", Author = "Author 3", Genre = "Drama", PublicationYear = 2021 },
                    new Book { Id="book4", Title = "Book 4", Author = "Author 4", Genre = "History", PublicationYear = 2021 },
                    new Book { Id="book5", Title = "Book 5", Author = "Author 5", Genre = "Thriller", PublicationYear = 2021 }
                };
                booksCollection.InsertMany(books);
            }

            await booksCollection.UpdateManyAsync(
                Builders<Book>.Filter.Or(
                    Builders<Book>.Filter.Exists(b => b.TotalCopies, false),
                    Builders<Book>.Filter.Eq(b => b.TotalCopies, 0)
                ),
                Builders<Book>.Update.Set(b => b.TotalCopies, 70)
            );


            if (usersCollection.CountDocuments(FilterDefinition<User>.Empty) == 0)
            {
                var users = new List<User>
                {
                    new User { Id="user1", Name = "User 1", Email = "user1@email.com" },
                    new User { Id="user2", Name = "User 2", Email = "user2@email.com" },
                    new User { Id="user3", Name = "User 3", Email = "user3@email.com" },
                    new User { Id="user4", Name = "User 4", Email = "user4@email.com" },
                    new User { Id="user5", Name = "User 5", Email = "user5@email.com" },
                };
                usersCollection.InsertMany(users);
            }

            IMongoCollection<UserBorrowedBook> userBorrowedBooks = database.GetCollection<UserBorrowedBook>("BorrowedBooks");

            if (userBorrowedBooks.CountDocuments(FilterDefinition<UserBorrowedBook>.Empty) == 0)
            {
                var borrowedBooksList = new List<BorrowedBook>();
                var userIds = new[] { "user1", "user2", "user3", "user4", "user5" };
                var bookIds = new[] { "book1", "book2", "book3", "book4", "book5" };

                // Example: user1 borrows book1 10 times, book2 5 times, book3 3 times, book4 2 times, book5 1 time
                // user2 borrows book1 1 time, book2 10 times, etc.
                int[,] borrowMatrix = new int[5, 5]
                {
                    { 10, 5, 3, 2, 1 }, // user1
                    { 1, 10, 5, 3, 2 }, // user2
                    { 2, 1, 10, 5, 3 }, // user3
                    { 3, 2, 1, 10, 5 }, // user4
                    { 5, 3, 2, 1, 10 }  // user5
                };

                int idCounter = 1;
                for (int u = 0; u < userIds.Length; u++)
                {
                    for (int b = 0; b < bookIds.Length; b++)
                    {
                        for (int borrow = 0; borrow < borrowMatrix[u, b]; borrow++)
                        {
                            var borrowedBook = new BorrowedBook
                            {
                                Id = $"borrowed{idCounter++}",
                                BookId = bookIds[b],
                                UserId = userIds[u],
                                BorrowDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-idCounter)),
                                DueDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(14 - idCounter)),
                                ReturnDate = (borrow % 3 == 0) ? Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-idCounter + 7)) : null
                            };
                            borrowedBooksList.Add(borrowedBook);
                        }
                    }
                }

                // Only take 70 records if more (should be exactly 70 with above matrix)
                userBorrowedBooks.InsertMany(borrowedBooksList.Take(70).Select(b => b.ToUserBorrowedBook()).ToList());
            }
        }
}
