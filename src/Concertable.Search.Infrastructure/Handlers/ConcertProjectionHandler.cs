using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Contracts;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal sealed class ConcertProjectionHandler : IIntegrationEventHandler<ConcertChangedEvent>
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
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ConcertProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(ConcertProjectionHandler));

        var concert = await context.Set<ConcertReadModel>()
            .Include(c => c.ConcertGenres)
            .FirstOrDefaultAsync(c => c.Id == e.ConcertId, ct);

        if (concert is null)
        {
            context.Set<ConcertReadModel>().Add(e.ToReadModel(geometryProvider));
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
            concert.Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);

            var desired = e.Genres.ToHashSet();
            var current = concert.ConcertGenres.Select(g => g.Genre).ToHashSet();

            foreach (var g in concert.ConcertGenres.Where(g => !desired.Contains(g.Genre)).ToList())
                concert.ConcertGenres.Remove(g);
            foreach (var g in desired.Where(g => !current.Contains(g)))
                concert.ConcertGenres.Add(new ConcertReadModelGenre { ConcertId = e.ConcertId, Genre = g });
        }

        await context.SaveChangesAsync(ct);
    }
}
