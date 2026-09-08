@{
    Environment = @{
        ConnectionStrings__SearchDb = 'Server=localhost;Database=concertable-search;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'SearchDbContext'; Project = 'src/Concertable.Search.Infrastructure'; StartupProject = 'src/Concertable.Search.Web'; OutputDir = 'Data/Migrations' }
    )
}
