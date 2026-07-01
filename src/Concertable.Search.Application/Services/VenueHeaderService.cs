using Concertable.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Application.Interfaces;
using Concertable.Search.Application.Params;

namespace Concertable.Search.Application.Services;

internal sealed class VenueHeaderService : IHeaderService
{
    private readonly IVenueHeaderRepository venueHeaderRepository;

    public VenueHeaderService(IVenueHeaderRepository venueHeaderRepository)
    {
        this.venueHeaderRepository = venueHeaderRepository;
    }

    public async Task<IPagination<IHeader>> SearchAsync(SearchParams searchParams)
    {
        var result = await venueHeaderRepository.SearchAsync(searchParams);
        return new Pagination<VenueHeader>(result.Data, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<IHeader>> GetByAmountAsync(int amount) =>
        await venueHeaderRepository.GetByAmountAsync(amount);
}
