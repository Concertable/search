namespace Concertable.Search.Infrastructure.Data;

internal static class DesignTimeConfiguration
{
    private const string ConnectionStringName = "SearchDb";

    public static string ConnectionString() =>
        Environment.GetEnvironmentVariable($"ConnectionStrings__{ConnectionStringName}")
        ?? throw new InvalidOperationException(
            $"Design-time connection string 'ConnectionStrings__{ConnectionStringName}' is not set. " +
            "Set it via environment or user-secrets — ./initial-migrations.ps1 exports it for local re-scaffolds.");
}
