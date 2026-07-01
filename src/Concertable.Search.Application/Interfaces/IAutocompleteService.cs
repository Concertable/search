using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IAutocompleteService
{
    Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm);
}
