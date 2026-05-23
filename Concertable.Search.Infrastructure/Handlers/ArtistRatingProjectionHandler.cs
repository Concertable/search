using Concertable.Artist.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Handlers;

internal class ArtistRatingProjectionHandler : IIntegrationEventHandler<ArtistRatingUpdatedEvent>
{
    private readonly SearchDbContext context;

    public ArtistRatingProjectionHandler(SearchDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ArtistRatingUpdatedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ArtistRatingProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ArtistRatingProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var projection = await context.Set<ArtistRatingProjection>()
            .FirstOrDefaultAsync(p => p.ArtistId == e.ArtistId, ct);

        if (projection is null)
            context.Set<ArtistRatingProjection>().Add(new ArtistRatingProjection { ArtistId = e.ArtistId, AverageRating = e.AverageRating, ReviewCount = e.ReviewCount });
        else
        {
            projection.AverageRating = e.AverageRating;
            projection.ReviewCount = e.ReviewCount;
        }

        await context.SaveChangesAsync(ct);
    }
}
