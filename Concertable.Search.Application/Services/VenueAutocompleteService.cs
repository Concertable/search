using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal class VenueAutocompleteService : IAutocompleteService
{
    private readonly IVenueAutocompleteRepository repository;

    public VenueAutocompleteService(IVenueAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
