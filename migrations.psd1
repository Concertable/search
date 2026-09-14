@{
    Environment = @{
        ConnectionStrings__SearchDb = 'Server=localhost;Database=concertable-search;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'SearchDbContext'; Project = 'api/src/Concertable.Search.Infrastructure'; StartupProject = 'api/src/Concertable.Search.Web'; OutputDir = 'Data/Migrations' }
    )
}
