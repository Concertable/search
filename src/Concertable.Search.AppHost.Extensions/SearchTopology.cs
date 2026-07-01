public static class SearchTopology
{
    public static AsbTopology AddSearchTopology(this AsbTopology topology) =>
        topology
            .Subscribe("event-concertchangedevent",       "search-concert-changed",        "concertable-search")
            .Subscribe("event-artistchangedevent",        "search-artist-changed",         "concertable-search")
            .Subscribe("event-venuechangedevent",         "search-venue-changed",          "concertable-search")
            .Subscribe("event-artistratingupdatedevent",  "search-artist-rating-updated",  "concertable-search")
            .Subscribe("event-venueratingupdatedevent",   "search-venue-rating-updated",   "concertable-search")
            .Subscribe("event-concertratingupdatedevent", "search-concert-rating-updated", "concertable-search");
}
