namespace LibraryAPI.Extensions
{
    public class BorrowedBookEntity
    {
        public required string Id { get; set; }
        public required string BookId { get; set; }
        public required string UserId { get; set; }
        public required DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public required DateTime DueDate { get; set; }
    }
}