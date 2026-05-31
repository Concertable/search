using Concertable.Seed.Identity;

namespace Concertable.Search.IntegrationTests.Fixtures;

public class SeedState
{
    public Guid Customer => SeedCustomers.CustomerId(1);
}
