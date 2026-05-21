namespace Concertable.Search.Application.Interfaces;

internal interface IAllAutocompleteRepository
{
    Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm);
}
