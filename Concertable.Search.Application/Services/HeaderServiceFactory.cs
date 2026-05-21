using Concertable.Search.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Application.Services;

internal class HeaderServiceFactory : IHeaderServiceFactory
{
    private readonly IKeyedServiceProvider serviceProvider;

    public HeaderServiceFactory(IKeyedServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IHeaderService Create(HeaderType type)
        => serviceProvider.GetRequiredKeyedService<IHeaderService>(type);
}
