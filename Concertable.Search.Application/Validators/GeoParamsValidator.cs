using FluentValidation;

namespace Concertable.Search.Application.Validators;

internal class GeoParamsValidator : AbstractValidator<IGeoParams>
{
    public GeoParamsValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.RadiusKm).GreaterThan(0).When(x => x.RadiusKm.HasValue);

        RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude.HasValue)
            .WithMessage("Longitude is required when Latitude is provided.");
        RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude.HasValue)
            .WithMessage("Latitude is required when Longitude is provided.");
    }
}
