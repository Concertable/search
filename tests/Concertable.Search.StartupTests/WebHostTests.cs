using Concertable.Search.Web;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.Search.StartupTests;

public sealed class WebHostTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddSearchWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(Concertable.Search.Web.HostExtensions).Assembly]
        });
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.False(jwtOptions.RequireHttpsMetadata);
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSearchWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void ProductionEnvironment_RequiresHttpsMetadata()
    {
        var arguments = CompositionTestArguments.Create();
        arguments[0] = "--environment=Production";
        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddSearchWebHost();
        using var app = builder.Build();
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(jwtOptions.RequireHttpsMetadata);
    }
}
