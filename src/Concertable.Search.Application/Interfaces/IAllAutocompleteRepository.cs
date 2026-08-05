using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IAllAutocompleteRepository
{
    Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm);
}
