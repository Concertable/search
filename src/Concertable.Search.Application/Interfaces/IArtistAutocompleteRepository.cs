using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IArtistAutocompleteRepository
{
    Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm);
}
