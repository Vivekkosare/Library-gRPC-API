using Grpc.Core;
using MongoDB.Driver;
using Moq;

namespace LibraryAPI.UnitTests;

public static class TestHelpers
{
    public static IAsyncCursor<T> MockCursor<T>(List<T> data)
    {
        var mockCursor = new Mock<IAsyncCursor<T>>();
        var moveNextCount = 0;
        mockCursor.Setup(c => c.Current).Returns(() => moveNextCount == 1 ? data : new List<T>());
        mockCursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(() => ++moveNextCount == 1);
        mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => ++moveNextCount == 1);
        return mockCursor.Object;
    }

    public static ServerCallContext CreateTestServerCallContext() => new TestServerCallContext();

    public class TestServerCallContext : ServerCallContext
    {
        public static ServerCallContext Create() => new TestServerCallContext();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null!;
        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "localhost";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; } = null;
        protected override AuthContext AuthContextCore => null!;
    }
}
