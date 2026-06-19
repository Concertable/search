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

// Migrate the projection tables before app.Run() starts the consumer, so events are never
// handled before their tables exist (app-lock-guarded, so Web migrating concurrently is safe).
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await serviceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
    await serviceProvider.GetRequiredService<InboxDbContext>().Database.MigrateAsync();
}

app.Run();
