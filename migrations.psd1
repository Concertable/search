@{
    Environment = @{
        ConnectionStrings__SearchDb = 'Host=localhost;Database=concertable-search;Username=postgres;Password=postgres'
    }
    Migrations = @(
        @{ Context = 'SearchDbContext'; Project = 'api/src/Concertable.Search.Infrastructure'; StartupProject = 'api/src/Concertable.Search.Web'; OutputDir = 'Data/Migrations' }
    )
}
