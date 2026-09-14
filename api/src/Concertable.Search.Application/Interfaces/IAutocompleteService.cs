using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IAutocompleteService
{
    Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm);
}
