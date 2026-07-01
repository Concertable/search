using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IConcertAutocompleteRepository
{
    Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm);
}
