namespace Concertable.Search.Application.Interfaces;

internal interface IVenueAutocompleteRepository
{
    Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm);
}
