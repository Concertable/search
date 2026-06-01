using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Handlers;

internal sealed class ConcertRatingProjectionHandler : IIntegrationEventHandler<ConcertRatingUpdatedEvent>
{
    private readonly SearchDbContext context;

    public ConcertRatingProjectionHandler(SearchDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ConcertRatingUpdatedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ConcertRatingProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(ConcertRatingProjectionHandler));

        var projection = await context.Set<ConcertRatingProjection>()
            .FirstOrDefaultAsync(p => p.ConcertId == e.ConcertId, ct);

        if (projection is null)
            context.Set<ConcertRatingProjection>().Add(new ConcertRatingProjection { ConcertId = e.ConcertId, AverageRating = e.AverageRating, ReviewCount = e.ReviewCount });
        else
        {
            projection.AverageRating = e.AverageRating;
            projection.ReviewCount = e.ReviewCount;
        }

        await context.SaveChangesAsync(ct);
    }
}
