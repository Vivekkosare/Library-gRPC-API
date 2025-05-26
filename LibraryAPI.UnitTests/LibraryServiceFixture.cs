using Castle.Core.Logging;
using LibraryAPI.Protos;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using LibraryAPI.Services;

namespace LibraryAPI.UnitTests;

public class LibraryServiceFixture
{
    public Mock<IMongoDatabase> MockDb { get; }
    public Mock<ILogger<Services.LibraryService>> MockLogger { get; }
    public LibraryServiceFixture()
    {
        MockDb = new Mock<IMongoDatabase>();
        MockLogger = new Mock<ILogger<Services.LibraryService>>();
    }

}