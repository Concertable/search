using Concertable.Search.Application.DTOs;

namespace Concertable.Search.Application.Interfaces;

internal interface IVenueAutocompleteRepository
{
    Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm);
}
