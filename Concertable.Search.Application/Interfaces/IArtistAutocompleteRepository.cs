namespace Concertable.Search.Application.Interfaces;

internal interface IArtistAutocompleteRepository
{
    Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm);
}
