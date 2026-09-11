using Concertable.Search.Hosting;
using Concertable.Search.Web;
using Concertable.Search.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.Search.StartupTests;

public sealed class AppModelStartupContractTests
{
    [Fact]
    public async Task WebHost_StartsOnTheConfigurationTheAppModelSupplies()
    {
        var appModel = AppHost.CreateBuilder([]);
        var arguments = await AppModelConfiguration.ArgumentsForAsync(
            appModel, SearchConstants.WebResource, AppModelConfiguration.Secrets);

        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddSearchWebHost();
        using var app = builder.Build();

        app.Services.GetService<IStartupValidator>()?.Validate();
    }

    [Fact]
    public async Task WorkerHost_StartsOnTheConfigurationTheAppModelSupplies()
    {
        var appModel = AppHost.CreateBuilder([]);
        var arguments = await AppModelConfiguration.ArgumentsForAsync(
            appModel, SearchConstants.WorkersResource, AppModelConfiguration.Secrets);

        var builder = Host.CreateApplicationBuilder(arguments);
        builder.AddSearchWorkerHost();
        using var app = builder.Build();

        app.Services.GetService<IStartupValidator>()?.Validate();
    }
}
