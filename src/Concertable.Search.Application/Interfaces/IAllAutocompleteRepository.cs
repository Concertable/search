using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IAllAutocompleteRepository
{
    Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm);
}
