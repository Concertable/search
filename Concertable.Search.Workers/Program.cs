using Concertable.Artist.Contracts.Events;
using Concertable.Concert.Contracts.Events;
using Concertable.Messaging.Application;
using Concertable.Messaging.AzureServiceBus;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.Venue.Contracts.Events;
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

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<InboxDbContext>().Database.MigrateAsync();

app.Run();
