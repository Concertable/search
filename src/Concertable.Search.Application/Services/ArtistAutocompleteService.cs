using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal sealed class ArtistAutocompleteService : IAutocompleteService
{
    private readonly IArtistAutocompleteRepository repository;

    public ArtistAutocompleteService(IArtistAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
