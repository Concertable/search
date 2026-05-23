using Concertable.Artist.Contracts.Events;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal class ArtistProjectionHandler : IIntegrationEventHandler<ArtistChangedEvent>
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
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ArtistProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ArtistProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);

        var artist = await context.Set<ArtistReadModel>()
            .Include(a => a.ArtistGenres)
            .FirstOrDefaultAsync(a => a.Id == e.ArtistId, ct);

        if (artist is null)
        {
            artist = new ArtistReadModel
            {
                Id = e.ArtistId,
                UserId = e.UserId,
                Name = e.Name,
                Avatar = e.Avatar,
                Location = location,
                Address = new Address(e.County, e.Town)
            };
            context.Set<ArtistReadModel>().Add(artist);

            foreach (var genre in e.Genres)
                artist.ArtistGenres.Add(new ArtistReadModelGenre { ArtistId = e.ArtistId, Genre = genre });
        }
        else
        {
            artist.UserId = e.UserId;
            artist.Name = e.Name;
            artist.Avatar = e.Avatar;
            artist.Location = location;
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
