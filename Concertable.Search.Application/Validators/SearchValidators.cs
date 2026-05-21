using Concertable.Application.Validators.Parameters;
using FluentValidation;

namespace Concertable.Search.Application.Validators;

internal class SearchParamsValidator : AbstractValidator<SearchParams>
{
    private static readonly string[] ValidSortValues = ["name_asc", "name_desc", "date_asc", "date_desc"];

    public SearchParamsValidator()
    {
        Include(new PageParamsValidator());
        Include(new GeoParamsValidator());
        RuleFor(x => x.HeaderType).NotNull();
        RuleFor(x => x.Sort)
            .Must(s => ValidSortValues.Contains(s!.ToLower()))
            .When(x => x.Sort is not null)
            .WithMessage($"Sort must be one of: {string.Join(", ", ValidSortValues)}.");
    }
}

internal class ConcertParamsValidator : AbstractValidator<ConcertParams>
{
    public ConcertParamsValidator()
    {
        Include(new GeoParamsValidator());
        RuleFor(x => x.Take).GreaterThan(0).When(x => x.Take != 0);
    }
}
