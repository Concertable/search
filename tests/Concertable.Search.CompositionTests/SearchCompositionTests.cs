using Concertable.Composition.Testing;
using Concertable.Search.Web;
using Concertable.Search.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Concertable.Search.CompositionTests;

public sealed class SearchCompositionTests
{
    [Fact]
    public void Web_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddSearchWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(Concertable.Search.Web.HostExtensions).Assembly]
        });
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSearchWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Workers_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        builder.AddSearchWorkerHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(Concertable.Search.Workers.HostExtensions).Assembly]
        });
        var invalidBuilder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSearchWorkerHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        using var app = SearchAppHost.CreateBuilder([]).Build();
        var builder = SearchAppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }
}
