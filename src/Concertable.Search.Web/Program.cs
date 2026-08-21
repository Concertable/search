using Concertable.Search.Api;
using Concertable.Search.Infrastructure.Data;
using Concertable.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Concertable.Search.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddSearchWebHost();

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
