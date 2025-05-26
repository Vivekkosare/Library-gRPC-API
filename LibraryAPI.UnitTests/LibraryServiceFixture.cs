using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace LibraryAPI.UnitTests;

public class LibraryServiceFixture
{
    public Mock<IMongoDatabase> MockDb { get; }
    public Mock<ILogger<Library_gRPC_API.Services.UserLibraryService>> MockLogger { get; }
    public LibraryServiceFixture()
    {
        MockDb = new Mock<IMongoDatabase>();
        MockLogger = new Mock<ILogger<Library_gRPC_API.Services.UserLibraryService>>();
    }

}