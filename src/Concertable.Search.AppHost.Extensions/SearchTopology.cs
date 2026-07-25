using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;

public static class SearchTopology
{
    public static AsbTopology AddSearchTopology(this AsbTopology topology) =>
        topology
            .Subscribe<ConcertChangedEvent>("search-concert-changed",        "concertable-search")
            .Subscribe<ArtistChangedEvent>("search-artist-changed",         "concertable-search")
            .Subscribe<VenueChangedEvent>("search-venue-changed",          "concertable-search")
            .Subscribe<ArtistRatingUpdatedEvent>("search-artist-rating-updated",  "concertable-search")
            .Subscribe<VenueRatingUpdatedEvent>("search-venue-rating-updated",   "concertable-search")
            .Subscribe<ConcertRatingUpdatedEvent>("search-concert-rating-updated", "concertable-search");
}
