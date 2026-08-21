using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Kernel;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Search.Infrastructure.Extensions;
using Concertable.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Workers;

public static class HostExtensions
{
    extension(HostApplicationBuilder builder)
    {
        public HostApplicationBuilder AddSearchWorkerHost()
        {
            builder.AddServiceDefaults();
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddSearchModule(builder.Configuration);
            builder.Services.AddSearchProjectionHandlers();
            builder.Services.AddAzureServiceBusTransport(
                opts =>
                {
                    opts.ConnectionString = builder.Configuration.GetConnectionString("asb")
                        ?? (builder.Environment.IsIntegration() ? null!
                            : throw new InvalidOperationException("Connection string 'asb' is required."));
                    opts.ServiceName = builder.Configuration["ServiceBus:ServiceName"]
                        ?? (builder.Environment.IsIntegration() ? "concertable-search"
                            : throw new InvalidOperationException("Configuration 'ServiceBus:ServiceName' is required."));
                },
                reg => reg
                    .SubscribeTo<ArtistChangedEvent>()
                    .SubscribeTo<VenueChangedEvent>()
                    .SubscribeTo<ConcertChangedEvent>()
                    .SubscribeTo<ArtistRatingUpdatedEvent>()
                    .SubscribeTo<VenueRatingUpdatedEvent>()
                    .SubscribeTo<ConcertRatingUpdatedEvent>());
            builder.Services.AddInbox(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("SearchDb")));
            return builder;
        }
    }
}
