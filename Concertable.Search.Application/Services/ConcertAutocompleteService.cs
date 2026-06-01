using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal sealed class ConcertAutocompleteService : IAutocompleteService
{
    private readonly IConcertAutocompleteRepository repository;

    public ConcertAutocompleteService(IConcertAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
