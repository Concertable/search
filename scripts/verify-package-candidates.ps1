[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'

$expectedPackageIds = @(
    'Concertable.Search.Hosting'
)

$resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packages = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -Filter '*.nupkg' -File |
    Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [StringComparison]::OrdinalIgnoreCase) })

if ($packages.Count -ne $expectedPackageIds.Count) {
    throw "Expected $($expectedPackageIds.Count) package candidates, found $($packages.Count)."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$metadata = foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspec = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [StringComparison]::OrdinalIgnoreCase) })
        if ($nuspec.Count -ne 1) {
            throw "Package '$($package.Name)' contains $($nuspec.Count) nuspec files."
        }

        $stream = $nuspec[0].Open()
        try {
            $document = [System.Xml.XmlDocument]::new()
            $document.Load($stream)
            $metadataNode = $document.DocumentElement.ChildNodes |
                Where-Object { $_.LocalName -eq 'metadata' } |
                Select-Object -First 1
            $idNode = $metadataNode.ChildNodes | Where-Object { $_.LocalName -eq 'id' } | Select-Object -First 1
            $versionNode = $metadataNode.ChildNodes | Where-Object { $_.LocalName -eq 'version' } | Select-Object -First 1
            [pscustomobject]@{ Id = $idNode.InnerText; Version = $versionNode.InnerText }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

$actualPackageIds = @($metadata.Id | Sort-Object)
$expectedSorted = @($expectedPackageIds | Sort-Object)
if (Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actualPackageIds) {
    throw "Package candidate IDs do not match the current Search candidate set: $($actualPackageIds -join ', ')."
}

$versions = @($metadata.Version | Sort-Object -Unique)
if ($versions.Count -ne 1) {
    throw "Search package candidates must use one version; found $($versions -join ', ')."
}

$tempDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$consumerDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $tempDirectory "search-pkg-$([Guid]::NewGuid().ToString('N'))"))
if (-not $consumerDirectory.StartsWith($tempDirectory, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to create package-consumer files outside the temporary directory."
}
[System.IO.Directory]::CreateDirectory($consumerDirectory) | Out-Null

try {
    $escapedPackageDirectory = [System.Security.SecurityElement]::Escape($resolvedPackageDirectory)
    $escapedVersion = [System.Security.SecurityElement]::Escape($versions[0])
    $projectPath = Join-Path $consumerDirectory 'Consumer.csproj'
    $sourcePath = Join-Path $consumerDirectory 'Consumer.cs'
    $configPath = Join-Path $consumerDirectory 'nuget.config'

    [System.IO.File]::WriteAllText($projectPath, @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Concertable.Search.Hosting" Version="$escapedVersion" />
  </ItemGroup>
</Project>
"@)

    [System.IO.File]::WriteAllText($sourcePath, @'
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Concertable.Search.Hosting;

namespace Concertable.Search.PackageConsumer;

public static class Consumer
{
    public static void AddContainerResources(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithServiceDiscovery> auth,
        IResourceBuilder<SqlServerDatabaseResource> searchDb,
        IResourceBuilder<AzureServiceBusResource> serviceBus)
    {
        var migrations = builder.AddSearchMigrations(
            "ghcr.io/concertable/search-migrations",
            "sha256:placeholder",
            searchDb);
        builder.AddSearchWeb("ghcr.io/concertable/search-web", "sha256:placeholder", auth, searchDb)
               .WaitForCompletion(migrations);
        builder.AddSearchWorkers("ghcr.io/concertable/search-workers", "sha256:placeholder", searchDb, serviceBus)
               .WaitForCompletion(migrations);
    }
}
'@)

    [System.IO.File]::WriteAllText($configPath, @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="search-candidates" value="$escapedPackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/Concertable/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="search-candidates">
      <package pattern="Concertable.Search.Hosting" />
    </packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    <packageSource key="github"><package pattern="Concertable.*" /></packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="Concertable" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
"@)

    $packagesPath = Join-Path $consumerDirectory 'packages'
    & dotnet restore $projectPath --configfile $configPath --packages $packagesPath --no-cache
    if ($LASTEXITCODE -ne 0) {
        throw "Clean package-consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build $projectPath --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Clean package-consumer build failed with exit code $LASTEXITCODE."
    }
}
finally {
    for ($attempt = 1; $attempt -le 3 -and [System.IO.Directory]::Exists($consumerDirectory); $attempt++) {
        try {
            [System.IO.Directory]::Delete($consumerDirectory, $true)
        }
        catch [System.IO.DirectoryNotFoundException] {
            if ([System.IO.Directory]::Exists($consumerDirectory)) {
                Start-Sleep -Milliseconds 200
            }
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 200
        }
    }

    if ([System.IO.Directory]::Exists($consumerDirectory)) {
        throw "Could not remove temporary package-consumer directory '$consumerDirectory'."
    }
}
