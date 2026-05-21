using Concertable.Artist.Contracts.Events;
using Concertable.Concert.Contracts.Events;
using Concertable.DataAccess.Infrastructure;
using Concertable.Search.Application;
using Concertable.Search.Domain.Models;
using Concertable.Search.Application.Validators;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Handlers;
using Concertable.Search.Infrastructure.Repositories;
using Concertable.Search.Application.Services;
using Concertable.Search.Infrastructure.Specifications;
using Concertable.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SearchDbContext>(opt =>
            opt.UseSqlServer(
                configuration.GetConnectionString("SearchDb"),
                sqlOpt => sqlOpt.UseNetTopologySuite()));
        services.AddScoped<ISearchDbContext>(sp => sp.GetRequiredService<SearchDbContext>());
        services.AddSingleton<SearchConfigurationProvider>();
        services.AddSingleton<IGeometrySpecification<ArtistSearchModel>, GeometrySpecification<ArtistSearchModel>>();
        services.AddSingleton<IGeometrySpecification<VenueSearchModel>, GeometrySpecification<VenueSearchModel>>();
        services.AddSingleton<IGeometrySpecification<ConcertSearchModel>, GeometrySpecification<ConcertSearchModel>>();

        services.AddSingleton<ISearchSpecification<ArtistSearchModel>, SearchSpecification<ArtistSearchModel>>();
        services.AddSingleton<ISearchSpecification<VenueSearchModel>, SearchSpecification<VenueSearchModel>>();
        services.AddSingleton<ISearchSpecification<ConcertSearchModel>, SearchSpecification<ConcertSearchModel>>();

        services.AddSingleton<IArtistSearchSpecification, ArtistSearchSpecification>();
        services.AddSingleton<IVenueSearchSpecification, VenueSearchSpecification>();
        services.AddSingleton<IConcertSearchSpecification, ConcertSearchSpecification>();

        services.AddSingleton<ISortSpecification<ArtistHeaderDto>, HeaderSortSpecification<ArtistHeaderDto>>();
        services.AddSingleton<ISortSpecification<VenueHeaderDto>, HeaderSortSpecification<VenueHeaderDto>>();
        services.AddSingleton<ISortSpecification<ConcertHeaderDto>, ConcertSortSpecification>();

        services.AddScoped<IArtistAutocompleteRepository, ArtistAutocompleteRepository>();
        services.AddScoped<IVenueAutocompleteRepository, VenueAutocompleteRepository>();
        services.AddScoped<IConcertAutocompleteRepository, ConcertAutocompleteRepository>();
        services.AddScoped<IAllAutocompleteRepository, AllAutocompleteRepository>();

        services.AddKeyedScoped<IAutocompleteService, ArtistAutocompleteService>(HeaderType.Artist);
        services.AddKeyedScoped<IAutocompleteService, VenueAutocompleteService>(HeaderType.Venue);
        services.AddKeyedScoped<IAutocompleteService, ConcertAutocompleteService>(HeaderType.Concert);
        services.AddScoped<IAutocompleteService, AllAutocompleteService>();

        services.AddScoped<IAutocompleteServiceFactory, AutocompleteServiceFactory>();

        services.AddScoped<IArtistHeaderRepository, ArtistHeaderRepository>();
        services.AddScoped<IVenueHeaderRepository, VenueHeaderRepository>();
        services.AddScoped<IConcertHeaderRepository, ConcertHeaderRepository>();

        services.AddKeyedScoped<IHeaderService, ArtistHeaderService>(HeaderType.Artist);
        services.AddKeyedScoped<IHeaderService, VenueHeaderService>(HeaderType.Venue);
        services.AddKeyedScoped<IHeaderService, ConcertHeaderService>(HeaderType.Concert);
        services.AddScoped<IConcertHeaderModule, ConcertHeaderService>();

        services.AddScoped<IHeaderServiceFactory, HeaderServiceFactory>();

        services.AddScoped<IHeaderModule, SearchModule>();

        services.AddValidatorsFromAssemblyContaining<SearchParamsValidator>();

        return services;
    }

    public static IServiceCollection AddSearchProjectionHandlers(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ConcertChangedEvent>, ConcertProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ReviewSubmittedEvent>, RatingProjectionHandler>();
        return services;
    }
}
