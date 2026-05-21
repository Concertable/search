namespace Concertable.Search.Application.Interfaces;

internal interface IConcertAutocompleteRepository
{
    Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm);
}
