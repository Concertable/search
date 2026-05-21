namespace Concertable.Search.Application.Interfaces;

internal interface IAutocompleteService
{
    Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm);
}
