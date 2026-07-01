using System.Text.Json.Serialization;

namespace Concertable.Search.Application;

[JsonConverter(typeof(JsonStringEnumConverter<HeaderType>))]
public enum HeaderType
{
    [JsonStringEnumMemberName(HeaderTypeNames.Artist)]
    Artist,
    [JsonStringEnumMemberName(HeaderTypeNames.Venue)]
    Venue,
    [JsonStringEnumMemberName(HeaderTypeNames.Concert)]
    Concert
}

public static class HeaderTypeNames
{
    public const string Artist = "artist";
    public const string Venue = "venue";
    public const string Concert = "concert";
}
