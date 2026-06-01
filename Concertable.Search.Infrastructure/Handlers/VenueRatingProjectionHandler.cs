using Concertable.Messaging.Contracts;
using Concertable.Search.Infrastructure.Data;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Handlers;

internal sealed class VenueRatingProjectionHandler : IIntegrationEventHandler<VenueRatingUpdatedEvent>
{
    private readonly SearchDbContext context;

    public VenueRatingProjectionHandler(SearchDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(VenueRatingUpdatedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VenueRatingProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(VenueRatingProjectionHandler));

        var projection = await context.Set<VenueRatingProjection>()
            .FirstOrDefaultAsync(p => p.VenueId == e.VenueId, ct);

        if (projection is null)
            context.Set<VenueRatingProjection>().Add(new VenueRatingProjection { VenueId = e.VenueId, AverageRating = e.AverageRating, ReviewCount = e.ReviewCount });
        else
        {
            projection.AverageRating = e.AverageRating;
            projection.ReviewCount = e.ReviewCount;
        }

        await context.SaveChangesAsync(ct);
    }
}
