using Concertable.B2B.Artist.Contracts.Events;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Contracts;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal sealed class ArtistProjectionHandler : IIntegrationEventHandler<ArtistChangedEvent>
{
    private readonly IGeometryProvider geometryProvider;
    private readonly SearchDbContext context;

    public ArtistProjectionHandler(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        SearchDbContext context)
    {
        this.geometryProvider = geometryProvider;
        this.context = context;
    }

    public async Task HandleAsync(ArtistChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ArtistProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(ArtistProjectionHandler));

        var artist = await context.Set<ArtistReadModel>()
            .Include(a => a.ArtistGenres)
            .FirstOrDefaultAsync(a => a.Id == e.ArtistId, ct);

        if (artist is null)
        {
            context.Set<ArtistReadModel>().Add(e.ToReadModel(geometryProvider));
        }
        else
        {
            artist.UserId = e.UserId;
            artist.Name = e.Name;
            artist.Avatar = e.Avatar;
            artist.Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);
            artist.Address = new Address(e.County, e.Town);

            var desired = e.Genres.ToHashSet();
            var current = artist.ArtistGenres.Select(g => g.Genre).ToHashSet();

            foreach (var g in artist.ArtistGenres.Where(g => !desired.Contains(g.Genre)).ToList())
                artist.ArtistGenres.Remove(g);
            foreach (var g in desired.Where(g => !current.Contains(g)))
                artist.ArtistGenres.Add(new ArtistReadModelGenre { ArtistId = e.ArtistId, Genre = g });
        }

        await context.SaveChangesAsync(ct);
    }
}
