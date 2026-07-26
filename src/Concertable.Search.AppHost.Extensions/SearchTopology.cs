using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;

public static class SearchTopology
{
    public static AsbTopology AddSearchTopology(this AsbTopology topology) =>
        topology
            .Subscribe<ConcertChangedEvent>(AppHostConstants.ServiceNames.Search)
            .Subscribe<ArtistChangedEvent>(AppHostConstants.ServiceNames.Search)
            .Subscribe<VenueChangedEvent>(AppHostConstants.ServiceNames.Search)
            .Subscribe<ArtistRatingUpdatedEvent>(AppHostConstants.ServiceNames.Search)
            .Subscribe<VenueRatingUpdatedEvent>(AppHostConstants.ServiceNames.Search)
            .Subscribe<ConcertRatingUpdatedEvent>(AppHostConstants.ServiceNames.Search);
}
