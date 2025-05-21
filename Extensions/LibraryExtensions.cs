using Google.Protobuf.WellKnownTypes;
using LibraryAPI.Models;
using LibraryAPI.Protos;

namespace LibraryAPI.Extensions
{
    public static class LibraryExtensions
    {
        public static DateTime ToMongoDateTime(this Timestamp timestamp)
        {
            return DateTime.SpecifyKind(timestamp.ToDateTime(), DateTimeKind.Utc);
        }
        public static DateTime ToMongoDateTime(this string dateTime)
        {
            return DateTime.SpecifyKind(DateTime.Parse(dateTime), DateTimeKind.Utc);
        }

        public static UserBorrowedBook ToUserBorrowedBook(this BorrowedBook borrowedBook)
        {
            return new UserBorrowedBook
            {
                Id = borrowedBook.Id,
                BookId = borrowedBook.BookId,
                UserId = borrowedBook.UserId,
                BorrowDate = borrowedBook.BorrowDate.ToMongoDateTime(),
                ReturnDate = borrowedBook.ReturnDate?.ToMongoDateTime(),
                DueDate = borrowedBook.DueDate.ToMongoDateTime()
            };
        }
    }
}