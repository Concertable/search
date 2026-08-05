using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;

public static class SearchTopology
{
    public static AsbTopology AddSearchTopology(this AsbTopology topology) =>
        topology
            .Subscribe<ConcertChangedEvent>(SearchConstants.ServiceName)
            .Subscribe<ArtistChangedEvent>(SearchConstants.ServiceName)
            .Subscribe<VenueChangedEvent>(SearchConstants.ServiceName)
            .Subscribe<ArtistRatingUpdatedEvent>(SearchConstants.ServiceName)
            .Subscribe<VenueRatingUpdatedEvent>(SearchConstants.ServiceName)
            .Subscribe<ConcertRatingUpdatedEvent>(SearchConstants.ServiceName);
}
