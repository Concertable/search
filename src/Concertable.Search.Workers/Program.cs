using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Search.Infrastructure.Data;
using Concertable.Search.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSearchWorkerHost();

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
