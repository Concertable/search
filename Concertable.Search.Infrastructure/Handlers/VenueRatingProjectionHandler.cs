using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Concertable.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Handlers;

internal class VenueRatingProjectionHandler : IIntegrationEventHandler<VenueRatingUpdatedEvent>
{
    private readonly SearchDbContext context;

    public VenueRatingProjectionHandler(SearchDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(VenueRatingUpdatedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VenueRatingProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VenueRatingProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

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
