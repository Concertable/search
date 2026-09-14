using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Contracts;
using Concertable.Search.Infrastructure.Data;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Handlers;

internal sealed class VenueProjectionHandler : IIntegrationEventHandler<VenueChangedEvent>
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
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VenueProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(VenueProjectionHandler));

        var venue = await context.Set<VenueReadModel>()
            .FirstOrDefaultAsync(v => v.Id == e.VenueId, ct);

        if (venue is null)
        {
            context.Set<VenueReadModel>().Add(e.ToReadModel(geometryProvider));
        }
        else
        {
            venue.UserId = e.UserId;
            venue.Name = e.Name;
            venue.Avatar = e.Avatar;
            venue.Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude);
            venue.Address = new Address(e.County, e.Town);
        }

        await context.SaveChangesAsync(ct);
    }
}
