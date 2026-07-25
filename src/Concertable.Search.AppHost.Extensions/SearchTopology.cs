using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;

public static class SearchTopology
{
    public static AsbTopology AddSearchTopology(this AsbTopology topology) =>
        topology.ForService(AppHostConstants.ServiceNames.Search)
            .Subscribe<ConcertChangedEvent>()
            .Subscribe<ArtistChangedEvent>()
            .Subscribe<VenueChangedEvent>()
            .Subscribe<ArtistRatingUpdatedEvent>()
            .Subscribe<VenueRatingUpdatedEvent>()
            .Subscribe<ConcertRatingUpdatedEvent>()
            .Topology;
}
