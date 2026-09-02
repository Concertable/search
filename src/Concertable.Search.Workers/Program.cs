using Concertable.Search.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSearchWorkerHost();

var app = builder.Build();
app.Run();
