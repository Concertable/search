using System.Net;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.Contracts.Enums;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.Search.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]
public sealed class PostgresProviderTests(ApiFixture fixture) : IAsyncLifetime
{
    private sealed record PaginationResponse<T>(IEnumerable<T> Data, int TotalCount);

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ResetAsync_ClearsBothOwnedSchemasTwice_AndPreservesDatabaseMetadata()
    {
        await InsertResetSentinelsAsync(900001, Guid.NewGuid());
        await fixture.ResetAsync();
        await AssertResetStateAsync(900001);

        await InsertResetSentinelsAsync(900002, Guid.NewGuid());
        await fixture.ResetAsync();
        await AssertResetStateAsync(900002);
    }

    [Fact]
    public async Task Search_PreservesCaseInsensitiveAccentSensitiveUnicodeSemantics()
    {
        await InsertArtistAsync(900010, "Ångström");
        await InsertArtistAsync(900011, "Angstrom");
        await InsertArtistAsync(900012, "東京");
        await InsertArtistAsync(900013, "case-order-alpha");
        await InsertArtistAsync(900014, "Case-Order-bravo");
        await InsertArtistAsync(900015, "CASE-ORDER-charlie");

        var client = fixture.CreateClient();
        var accented = await SearchArtistsAsync(client, "ÅNGSTRÖM");
        Assert.Contains(accented, artist => artist.Name == "Ångström");
        Assert.DoesNotContain(accented, artist => artist.Name == "Angstrom");

        var unaccented = await SearchArtistsAsync(client, "Angstrom");
        Assert.Contains(unaccented, artist => artist.Name == "Angstrom");
        Assert.DoesNotContain(unaccented, artist => artist.Name == "Ångström");

        var unicode = await SearchArtistsAsync(client, "東京");
        Assert.Contains(unicode, artist => artist.Name == "東京");

        var ordered = await SearchArtistsAsync(client, "case-order-", "name_asc");
        Assert.Equal(
        [
            "case-order-alpha",
            "Case-Order-bravo",
            "CASE-ORDER-charlie",
        ],
        ordered.Select(artist => artist.Name));
    }

    [Fact]
    public async Task RadiusQuery_UsesPostgisTranslationAndSpatialIndex()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
            var geometry = scope.ServiceProvider.GetRequiredService<IGeometrySpecification<ConcertReadModel>>();
            var radiusSql = context.Set<ConcertReadModel>()
                .Where(geometry.ToExpression(new ConcertParams
                {
                    Latitude = 51.5074,
                    Longitude = -0.1278,
                    RadiusKm = 1,
                }))
                .ToQueryString();
            var recentSql = context.Set<ConcertReadModel>()
                .OrderByDatePostedDescending()
                .ToQueryString();

            Assert.Contains("ST_DWithin", radiusSql);
            Assert.Contains("IS NULL", recentSql);
            Assert.Contains("DESC", recentSql);
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var populate = connection.CreateCommand())
        {
            populate.CommandText = """
                INSERT INTO search."Concerts"
                    ("Id", "ArtistId", "VenueId", "Name", "Avatar", "Price", "TotalTickets",
                     "AvailableTickets", "StartDate", "EndDate", "DatePosted", "Location")
                SELECT
                    100000 + value,
                    1,
                    1,
                    'spatial-' || value,
                    NULL,
                    10,
                    100,
                    100,
                    now(),
                    now() + interval '1 day',
                    now(),
                    ST_SetSRID(ST_MakePoint(-179 + (value % 358), -80 + (value % 160)), 4326)::geography
                FROM generate_series(1, 4096) AS value;
                ANALYZE search."Concerts";
                """;
            await populate.ExecuteNonQueryAsync();
        }

        await using var explain = connection.CreateCommand();
        explain.CommandText = """
            EXPLAIN (FORMAT JSON)
            SELECT "Id"
            FROM search."Concerts"
            WHERE ST_DWithin(
                "Location",
                ST_SetSRID(ST_MakePoint(-0.1278, 51.5074), 4326)::geography,
                1000)
            """;
        var plan = Assert.IsType<string>(await explain.ExecuteScalarAsync());
        Assert.Contains("IX_Concerts_Location", plan);
    }

    [Fact]
    public async Task ArtistProjection_ReplayingTheSameEnvelope_PreservesTheSuppliedIdentifiersOnce()
    {
        const int artistId = 900020;
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var changed = new ArtistChangedEvent(
            artistId,
            userId,
            "Replay Artist",
            "About",
            "avatar",
            "banner",
            "London",
            "London",
            51.5074,
            -0.1278,
            "replay@example.com",
            [Genre.Rock],
            tenantId);
        var envelope = MessageEnvelope.Create<ArtistChangedEvent>(DateTimeOffset.UtcNow);
        var handlers = fixture.Services
            .GetRequiredService<IScoped<IEnumerable<IIntegrationEventHandler<ArtistChangedEvent>>>>();

        await handlers.RunAsync(async registered =>
        {
            foreach (var handler in registered)
                await handler.HandleAsync(changed, envelope);
        });
        await handlers.RunAsync(async registered =>
        {
            foreach (var handler in registered)
                await handler.HandleAsync(changed, envelope);
        });

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a."UserId", COUNT(i."MessageId")
            FROM search."Artists" AS a
            LEFT JOIN messaging."Inbox" AS i
                ON i."MessageId" = @message_id
               AND i."ConsumerName" = 'ArtistProjectionHandler'
            WHERE a."Id" = @artist_id
            GROUP BY a."UserId"
            """;
        command.Parameters.AddWithValue("message_id", envelope.MessageId);
        command.Parameters.AddWithValue("artist_id", artistId);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(userId, reader.GetGuid(0));
        Assert.Equal(1, reader.GetInt64(1));
        Assert.False(await reader.ReadAsync());
    }

    private async Task InsertResetSentinelsAsync(int artistId, Guid messageId)
    {
        await InsertArtistAsync(artistId, $"reset-{artistId}");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO messaging."Inbox" ("MessageId", "ConsumerName", "MessageType", "ReceivedAt")
            VALUES (@message_id, 'ResetProof', 'reset-proof', now())
            """;
        command.Parameters.AddWithValue("message_id", messageId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task AssertResetStateAsync(int artistId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        Assert.Equal(
            0L,
            await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM search.\"Artists\" WHERE \"Id\" = @artist_id",
                new NpgsqlParameter("artist_id", artistId)));
        Assert.Equal(
            0L,
            await ScalarAsync(connection, "SELECT COUNT(*) FROM messaging.\"Inbox\" WHERE \"ConsumerName\" = 'ResetProof'"));
        Assert.Equal(
            2L,
            await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_name LIKE '__EFMigrationsHistory%'"));
        Assert.True(await ScalarAsync(connection, "SELECT COUNT(*) FROM public.spatial_ref_sys") > 0);
        Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM pg_extension WHERE extname = 'postgis'"));
    }

    private async Task InsertArtistAsync(int artistId, string name)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO search."Artists" ("Id", "UserId", "Name", "Avatar", "Location", "County", "Town")
            VALUES (
                @artist_id,
                @user_id,
                @name,
                'avatar',
                ST_SetSRID(ST_MakePoint(-0.1278, 51.5074), 4326)::geography,
                'London',
                'London')
            """;
        command.Parameters.AddWithValue("artist_id", artistId);
        command.Parameters.AddWithValue("user_id", Guid.NewGuid());
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<ArtistHeader>> SearchArtistsAsync(
        HttpClient client,
        string searchTerm,
        string? sort = null)
    {
        var uri = $"/api/Header?headerType=Artist&pageSize=100&searchTerm={Uri.EscapeDataString(searchTerm)}";
        if (sort is not null)
            uri += $"&sort={sort}";

        var response = await client.GetAsync(uri);
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ArtistHeader>>();
        Assert.NotNull(result);
        return result.Data.ToArray();
    }

    private static async Task<long> ScalarAsync(
        NpgsqlConnection connection,
        string sql,
        NpgsqlParameter? parameter = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter is not null)
            command.Parameters.Add(parameter);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
