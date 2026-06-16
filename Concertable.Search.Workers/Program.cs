using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.ServiceDefaults;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();

var services = builder.Services;

services.AddSearchModule(builder.Configuration);
services.AddSearchProjectionHandlers();

services.AddAzureServiceBusTransport(
    opts =>
    {
        opts.ConnectionString = builder.Configuration.GetConnectionString("asb") ?? "";
        opts.ServiceName = "concertable-search";
    },
    reg => reg
        .SubscribeTo<ArtistChangedEvent>()
        .SubscribeTo<VenueChangedEvent>()
        .SubscribeTo<ConcertChangedEvent>()
        .SubscribeTo<ArtistRatingUpdatedEvent>()
        .SubscribeTo<VenueRatingUpdatedEvent>()
        .SubscribeTo<ConcertRatingUpdatedEvent>());

services.AddInbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("SearchDb")));

var app = builder.Build();

// The projection handlers write SearchDbContext's tables (search.Venues/Artists/Concerts/...).
// Migrate that context here — before app.Run() starts the message pump — so the consumer never
// processes a projection event before those tables exist. Otherwise the first events lose the
// startup race against the Search.Web migration, fail with "Invalid object name 'search.*'", and
// are dropped; the Search-backed find query inner-joins concerts -> venues -> artists, so every
// concert at an un-projected venue silently vanishes from results. MigrateAsync is idempotent and
// guarded by EF's migration app-lock, so Web migrating the same context concurrently is safe.
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await serviceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
    await serviceProvider.GetRequiredService<InboxDbContext>().Database.MigrateAsync();
}

app.Run();
