namespace LibraryAPI.Models
{
    public class UserBorrowedBook
    {
        public required string Id { get; set; }
        public required string BookId { get; set; }
        public required string UserId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}