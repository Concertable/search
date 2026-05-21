using Concertable.Search.Api.Controllers;
using Concertable.Search.Api.ModelBinders;
using Concertable.Search.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSearchModule(configuration);
        services.Configure<MvcOptions>(opts =>
            opts.ModelBinderProviders.Insert(0, new CommaDelimitedIntArrayBinderProvider()));
        services.AddControllers()
            .AddApplicationPart(typeof(AutocompleteController).Assembly)
            .ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        return services;
    }
}
