using Concertable.Search.Api;
using Concertable.Search.Api.Extensions;
using Concertable.Search.Infrastructure.Data;
using Concertable.Shared.Api.Exceptions;
using Concertable.Shared.Api.Extensions;
using Concertable.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Concertable.Search.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithOrigins(corsOrigins);
    });
});

var services = builder.Services;

services.AddProblemDetails();
services.AddControllers()
    .AddApplicationPart(typeof(Concertable.Shared.Api.Controllers.GenreController).Assembly)
    .AddApplicationJson();
services.AddSearchApi(builder.Configuration);

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.Authority = builder.Configuration["Auth:Authority"] ?? builder.Configuration["services__auth__https__0"];
        opts.Audience = "concertable.search.api";
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = !builder.Environment.IsDevelopment()
        };
    });
services.AddAuthorization();

services.AddExceptionHandler<GlobalExceptionHandler>();

services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

builder.AddDefaultRateLimiting();
builder.AddRateLimitPolicy(RateLimitPolicies.Search, new RateLimitWindow { PermitLimit = 120, WindowSeconds = 60 }, perUser: false);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultRateLimiting();

app.MapDefaultEndpoints();
app.MapControllers();

if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
}

app.Run();

public sealed partial class Program
{ }
