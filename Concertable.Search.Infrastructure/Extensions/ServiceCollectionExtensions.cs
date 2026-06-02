using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.DataAccess.Infrastructure;
using Concertable.Search.Domain.Models;
using Concertable.Search.Application.Validators;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Handlers;
using Concertable.Search.Infrastructure.Repositories;
using Concertable.Search.Application.Services;
using Concertable.Search.Infrastructure.Specifications;
using Concertable.B2B.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Messaging.Contracts;
using Concertable.Search.Application.DTOs;
using Concertable.Kernel.Extensions;

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

        services.AddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        services.AddSingleton(TimeProvider.System);
        services.AddGeometry();

        services.AddSingleton<IGeometrySpecification<ArtistReadModel>, GeometrySpecification<ArtistReadModel>>();
        services.AddSingleton<IGeometrySpecification<VenueReadModel>, GeometrySpecification<VenueReadModel>>();
        services.AddSingleton<IGeometrySpecification<ConcertReadModel>, GeometrySpecification<ConcertReadModel>>();

        services.AddSingleton<ISearchSpecification<ArtistReadModel>, SearchSpecification<ArtistReadModel>>();
        services.AddSingleton<ISearchSpecification<VenueReadModel>, SearchSpecification<VenueReadModel>>();
        services.AddSingleton<ISearchSpecification<ConcertReadModel>, SearchSpecification<ConcertReadModel>>();

        services.AddSingleton<IArtistSearchSpecification, ArtistSearchSpecification>();
        services.AddSingleton<IVenueSearchSpecification, VenueSearchSpecification>();
        services.AddSingleton<IConcertSearchSpecification, ConcertSearchSpecification>();

        services.AddSingleton<ISortSpecification<ArtistHeader>, HeaderSortSpecification<ArtistHeader>>();
        services.AddSingleton<ISortSpecification<VenueHeader>, HeaderSortSpecification<VenueHeader>>();
        services.AddSingleton<ISortSpecification<ConcertHeader>, ConcertSortSpecification>();

        services.AddScoped<IArtistAutocompleteRepository, ArtistAutocompleteRepository>();
        services.AddScoped<IVenueAutocompleteRepository, VenueAutocompleteRepository>();
        services.AddScoped<IConcertAutocompleteRepository, ConcertAutocompleteRepository>();
        services.AddScoped<IAllAutocompleteRepository, AllAutocompleteRepository>();

        services.AddKeyedScoped<IAutocompleteService, ArtistAutocompleteService>(HeaderType.Artist);
        services.AddKeyedScoped<IAutocompleteService, VenueAutocompleteService>(HeaderType.Venue);
        services.AddKeyedScoped<IAutocompleteService, ConcertAutocompleteService>(HeaderType.Concert);
        services.AddKeyedScoped<IAutocompleteService, AllAutocompleteService>(null);

        services.AddScoped<IAutocompleteServiceFactory, AutocompleteServiceFactory>();

        services.AddScoped<IArtistHeaderRepository, ArtistHeaderRepository>();
        services.AddScoped<IVenueHeaderRepository, VenueHeaderRepository>();
        services.AddScoped<IConcertHeaderRepository, ConcertHeaderRepository>();

        services.AddKeyedScoped<IHeaderService, ArtistHeaderService>(HeaderType.Artist);
        services.AddKeyedScoped<IHeaderService, VenueHeaderService>(HeaderType.Venue);
        services.AddKeyedScoped<IHeaderService, ConcertHeaderService>(HeaderType.Concert);
        services.AddScoped<IConcertHeaderService, ConcertHeaderService>();

        services.AddScoped<IHeaderServiceFactory, HeaderServiceFactory>();

        services.AddScoped<IHeaderDispatcher, HeaderDispatcher>();

        services.AddValidatorsFromAssemblyContaining<SearchParamsValidator>();

        return services;
    }

    public static IServiceCollection AddSearchProjectionHandlers(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ConcertChangedEvent>, ConcertProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ArtistRatingUpdatedEvent>, ArtistRatingProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueRatingUpdatedEvent>, VenueRatingProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ConcertRatingUpdatedEvent>, ConcertRatingProjectionHandler>();
        return services;
    }
}
