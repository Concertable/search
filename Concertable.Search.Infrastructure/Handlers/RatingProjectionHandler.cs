using Concertable.Artist.Domain;
using Concertable.Concert.Contracts.Events;
using Concertable.Concert.Domain;
using Concertable.Messaging.Domain;
using Concertable.Search.Infrastructure.Data;
using Concertable.Venue.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Handlers;

internal class RatingProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly SearchDbContext context;

    public RatingProjectionHandler(SearchDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(RatingProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(RatingProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        await UpdateArtistRatingAsync(e.ArtistId, e.Stars, ct);
        await UpdateVenueRatingAsync(e.VenueId, e.Stars, ct);
        await UpdateConcertRatingAsync(e.ConcertId, e.Stars, ct);

        await context.SaveChangesAsync(ct);
    }

    private async Task UpdateArtistRatingAsync(int artistId, double stars, CancellationToken ct)
    {
        var projection = await context.Set<ArtistRatingProjection>()
            .FirstOrDefaultAsync(p => p.ArtistId == artistId, ct);

        if (projection is null)
            context.Set<ArtistRatingProjection>().Add(new ArtistRatingProjection { ArtistId = artistId, AverageRating = stars, ReviewCount = 1 });
        else
        {
            projection.AverageRating = CalculateAverage(projection.AverageRating, projection.ReviewCount, stars);
            projection.ReviewCount++;
        }
    }

    private async Task UpdateVenueRatingAsync(int venueId, double stars, CancellationToken ct)
    {
        var projection = await context.Set<VenueRatingProjection>()
            .FirstOrDefaultAsync(p => p.VenueId == venueId, ct);

        if (projection is null)
            context.Set<VenueRatingProjection>().Add(new VenueRatingProjection { VenueId = venueId, AverageRating = stars, ReviewCount = 1 });
        else
        {
            projection.AverageRating = CalculateAverage(projection.AverageRating, projection.ReviewCount, stars);
            projection.ReviewCount++;
        }
    }

    private async Task UpdateConcertRatingAsync(int concertId, double stars, CancellationToken ct)
    {
        var projection = await context.Set<ConcertRatingProjection>()
            .FirstOrDefaultAsync(p => p.ConcertId == concertId, ct);

        if (projection is null)
            context.Set<ConcertRatingProjection>().Add(new ConcertRatingProjection { ConcertId = concertId, AverageRating = stars, ReviewCount = 1 });
        else
        {
            projection.AverageRating = CalculateAverage(projection.AverageRating, projection.ReviewCount, stars);
            projection.ReviewCount++;
        }
    }

    private static double CalculateAverage(double currentAvg, int currentCount, double stars)
        => Math.Round((currentAvg * currentCount + stars) / (currentCount + 1), 1);
}
