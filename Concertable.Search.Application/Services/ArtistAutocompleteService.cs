using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal class ArtistAutocompleteService : IAutocompleteService
{
    private readonly IArtistAutocompleteRepository repository;

    public ArtistAutocompleteService(IArtistAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
