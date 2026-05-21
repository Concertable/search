using Concertable.Application.Interfaces.Geometry;
using Concertable.Concert.Contracts.Events;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal class ConcertProjectionHandler : IIntegrationEventHandler<ConcertChangedEvent>
{
    private readonly IGeometryProvider geometryProvider;
    private readonly SearchDbContext context;

    public ConcertProjectionHandler(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        SearchDbContext context)
    {
        this.geometryProvider = geometryProvider;
        this.context = context;
    }

    public async Task HandleAsync(ConcertChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ConcertProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ConcertProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);

        var concert = await context.Set<ConcertSearchModel>()
            .Include(c => c.ConcertGenres)
            .FirstOrDefaultAsync(c => c.Id == e.ConcertId, ct);

        if (concert is null)
        {
            concert = new ConcertSearchModel
            {
                Id = e.ConcertId,
                ArtistId = e.ArtistId,
                VenueId = e.VenueId,
                Name = e.Name,
                Avatar = e.Avatar,
                Price = e.Price,
                TotalTickets = e.TotalTickets,
                AvailableTickets = e.AvailableTickets,
                StartDate = e.Period.Start,
                EndDate = e.Period.End,
                DatePosted = e.DatePosted,
                Location = location
            };
            context.Set<ConcertSearchModel>().Add(concert);

            foreach (var genre in e.Genres)
                concert.ConcertGenres.Add(new ConcertSearchModelGenre { ConcertId = e.ConcertId, Genre = genre });
        }
        else
        {
            concert.ArtistId = e.ArtistId;
            concert.VenueId = e.VenueId;
            concert.Name = e.Name;
            concert.Avatar = e.Avatar;
            concert.Price = e.Price;
            concert.TotalTickets = e.TotalTickets;
            concert.AvailableTickets = e.AvailableTickets;
            concert.StartDate = e.Period.Start;
            concert.EndDate = e.Period.End;
            concert.DatePosted = e.DatePosted;
            concert.Location = location;

            var desired = e.Genres.ToHashSet();
            var current = concert.ConcertGenres.Select(g => g.Genre).ToHashSet();

            foreach (var g in concert.ConcertGenres.Where(g => !desired.Contains(g.Genre)).ToList())
                concert.ConcertGenres.Remove(g);
            foreach (var g in desired.Where(g => !current.Contains(g)))
                concert.ConcertGenres.Add(new ConcertSearchModelGenre { ConcertId = e.ConcertId, Genre = g });
        }

        await context.SaveChangesAsync(ct);
    }
}
