using Concertable.Search.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();

var services = builder.Services;

services.AddControllers();
services.AddSearchApi(builder.Configuration);

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.Authority = builder.Configuration["Auth:Authority"] ?? builder.Configuration["services__auth__https__0"];
        opts.Audience = "concertable.api";
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = !builder.Environment.IsDevelopment()
        };
    });
services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

public partial class Program { }
