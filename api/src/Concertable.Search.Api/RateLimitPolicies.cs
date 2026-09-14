namespace Concertable.Search.Api;

public static class RateLimitPolicies
{
    public const string Search = "search";

    public static readonly IReadOnlyList<string> All = [Search];
}
