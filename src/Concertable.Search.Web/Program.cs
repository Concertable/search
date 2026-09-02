using Concertable.Search.Api;
using Concertable.ServiceDefaults;
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

app.Run();

public sealed partial class Program
{ }
