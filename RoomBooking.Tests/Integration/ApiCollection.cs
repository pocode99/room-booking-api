using Xunit;

namespace RoomBooking.Tests.Integration;

// All integration test classes join this collection, so they share ONE running application
// and ONE database, and never run in parallel with each other.
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
}