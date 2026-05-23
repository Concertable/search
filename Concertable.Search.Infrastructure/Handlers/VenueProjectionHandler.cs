using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal class VenueProjectionHandler : IIntegrationEventHandler<VenueChangedEvent>
{
    private readonly IGeometryProvider geometryProvider;
    private readonly SearchDbContext context;

    public VenueProjectionHandler(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        SearchDbContext context)
    {
        this.geometryProvider = geometryProvider;
        this.context = context;
    }

    public async Task HandleAsync(VenueChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VenueProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VenueProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);

        var venue = await context.Set<VenueReadModel>()
            .FirstOrDefaultAsync(v => v.Id == e.VenueId, ct);

        if (venue is null)
        {
            context.Set<VenueReadModel>().Add(new VenueReadModel
            {
                Id = e.VenueId,
                UserId = e.UserId,
                Name = e.Name,
                Avatar = e.Avatar,
                Location = location,
                Address = new Address(e.County, e.Town)
            });
        }
        else
        {
            venue.UserId = e.UserId;
            venue.Name = e.Name;
            venue.Avatar = e.Avatar;
            venue.Location = location;
            venue.Address = new Address(e.County, e.Town);
        }

        await context.SaveChangesAsync(ct);
    }
}
