using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IVenueAutocompleteRepository
{
    Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm);
}
