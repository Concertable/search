namespace Concertable.Search.Infrastructure;

internal static class Schema
{
    public const string Name = "search";

    public static class Tables
    {
        public const string Artists = "Artists";
        public const string ArtistGenres = "ArtistGenres";
        public const string Concerts = "Concerts";
        public const string ConcertGenres = "ConcertGenres";
        public const string Venues = "Venues";
        public const string ArtistRatingProjections = "ArtistRatingProjections";
        public const string ConcertRatingProjections = "ConcertRatingProjections";
        public const string VenueRatingProjections = "VenueRatingProjections";
    }
}
