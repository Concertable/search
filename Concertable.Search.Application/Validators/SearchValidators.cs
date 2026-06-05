using Concertable.Kernel.Validators;
using Concertable.Search.Application.Params;
using FluentValidation;

namespace Concertable.Search.Application.Validators;

internal sealed class SearchParamsValidator : AbstractValidator<SearchParams>
{
    public SearchParamsValidator()
    {
        Include(new PageParamsValidator());
        Include(new GeoParamsValidator());
        RuleFor(x => x.HeaderType).NotNull();
    }
}

internal sealed class ConcertParamsValidator : AbstractValidator<ConcertParams>
{
    public ConcertParamsValidator()
    {
        Include(new GeoParamsValidator());
        RuleFor(x => x.Take).GreaterThan(0).When(x => x.Take != 0);
    }
}
