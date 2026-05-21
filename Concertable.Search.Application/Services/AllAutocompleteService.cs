using Concertable.Search.Application.Interfaces;

namespace Concertable.Search.Application.Services;

internal class AllAutocompleteService : IAutocompleteService
{
    private readonly IAllAutocompleteRepository repository;

    public AllAutocompleteService(IAllAutocompleteRepository repository)
    {
        this.repository = repository;
    }

    public Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        repository.GetAsync(searchTerm);
}
