using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal sealed class AllAutocompleteService : IAutocompleteService
{
    private readonly IAllAutocompleteRepository repository;

    public AllAutocompleteService(IAllAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
