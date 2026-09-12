[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath,

    [switch] $KeepArtifacts
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryUrl = 'https://github.com/Concertable/search'
$trivyImage = 'aquasec/trivy:0.74.0@sha256:62b1e65e8869bc4b4c6aa4fa2b21595256c7c2f6018a9d9ad61caf87187c1969'
$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) {
    throw 'Could not resolve the Search source revision.'
}

$workingTreeChanges = @(& git -C $repositoryRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the Search working tree.'
}
if ($workingTreeChanges.Count -gt 0) {
    throw 'Refusing to prepare a Search release candidate from a dirty or untracked working tree.'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    if ($KeepArtifacts) {
        throw 'OutputPath is required when KeepArtifacts is specified.'
    }

    $releaseRoot = Join-Path ([System.IO.Path]::GetTempPath()) "concertable-search-release-candidate-$([Guid]::NewGuid().ToString('N'))"
}
elseif ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $releaseRoot = [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    $releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
}

$repositoryPrefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if ($releaseRoot.Equals($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $releaseRoot.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release-candidate output must be outside the Search repository so the image build context remains clean.'
}

if (Test-Path -LiteralPath $releaseRoot) {
    throw "Release-candidate output already exists: '$releaseRoot'."
}

$releaseId = [Guid]::NewGuid().ToString('N')
$markerPath = Join-Path $releaseRoot '.search-release-candidate'
$packageRoot = Join-Path $releaseRoot 'packages'
$imageRoot = Join-Path $releaseRoot 'images'
$evidenceRoot = Join-Path $releaseRoot 'evidence'
$manifestPath = Join-Path $releaseRoot 'release-manifest.json'
$trivyCacheDirectory = Join-Path $repositoryRoot 'artifacts/.trivy-cache'
$webImage = "concertable/search-web:release-candidate-$releaseId"
$workersImage = "concertable/search-workers:release-candidate-$releaseId"
$migrationsImage = "concertable/search-migrations:release-candidate-$releaseId"
$candidateImages = @($webImage, $workersImage, $migrationsImage)
$expectedPackageIds = @('Concertable.Search.Hosting')
$packageProjects = @(
    (Join-Path $repositoryRoot 'src/Concertable.Search.Hosting/Concertable.Search.Hosting.csproj')
)
$packageToken = $env:GITHUB_PACKAGES_TOKEN
Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
$releaseRootCreated = $false
$completed = $false
$releaseVersion = ''

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Value
    )

    [System.IO.File]::WriteAllText($Path, $Value, [System.Text.UTF8Encoding]::new($false))
}

function Assert-ReleaseRoot {
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Refusing release-candidate operation without marker '$markerPath'."
    }
}

function Invoke-WithPackageToken {
    param(
        [Parameter(Mandatory)][scriptblock] $Action,
        [Parameter(Mandatory)][string] $Token
    )

    if ([string]::IsNullOrWhiteSpace($Token)) {
        throw 'GITHUB_PACKAGES_TOKEN is required to restore Search release-candidate dependencies.'
    }

    try {
        $env:GITHUB_PACKAGES_TOKEN = $Token
        & $Action
    }
    finally {
        Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    }
}

function Get-NuGetIdentity {
    param([Parameter(Mandatory)][System.IO.FileInfo] $Package)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Package.FullName)
    try {
        $manifestEntry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        if ($null -eq $manifestEntry) {
            throw "Package '$($Package.Name)' does not contain a NuGet manifest."
        }

        $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
        try {
            [xml] $manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $manifest.package.metadata
        if ([string] $metadata.repository.url -ne $repositoryUrl -or [string] $metadata.projectUrl -ne $repositoryUrl) {
            throw "Package '$($Package.Name)' does not identify the canonical Search repository."
        }
        if ([string] $metadata.repository.commit -ne $revision) {
            throw "Package '$($Package.Name)' does not identify exact Search revision '$revision'."
        }
        if ([string] $metadata.readme -ne 'README.md') {
            throw "Package '$($Package.Name)' does not declare its package README."
        }
        if ([string]::IsNullOrWhiteSpace([string] $metadata.description) -or [string] $metadata.description -eq 'Package Description') {
            throw "Package '$($Package.Name)' does not have a meaningful description."
        }

        return [ordered]@{
            Id = [string] $metadata.id
            Version = [string] $metadata.version
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-ArtifactRecord {
    param([Parameter(Mandatory)][string] $Path)

    $item = Get-Item -LiteralPath $Path
    $releasePrefix = $releaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $item.FullName.StartsWith($releasePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Artifact '$($item.FullName)' is outside the release-candidate root."
    }

    $stream = [System.IO.File]::OpenRead($item.FullName)
    try {
        $hasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            $sha256 = ([System.BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $hasher.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    return [ordered]@{
        path = $item.FullName.Substring($releasePrefix.Length).Replace('\', '/')
        sha256 = $sha256
        bytes = $item.Length
    }
}

function Get-ImageInspection {
    param([Parameter(Mandatory)][string] $Image)

    $json = & docker image inspect $Image
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect release-candidate image '$Image'."
    }

    return ($json | ConvertFrom-Json)[0]
}

function Initialize-TrivyCache {
    if (-not (Test-Path -LiteralPath $trivyCacheDirectory)) {
        New-Item -ItemType Directory -Path $trivyCacheDirectory -Force | Out-Null
    }
}

# Never passes --exit-code. Trivy exits 1 both for "findings" and for a fatal error, so the exit
# code alone cannot tell a real finding from a scan that never ran. The report file can: the fatal
# path never writes one. Returns the exit code and lets Assert-TrivyReport decide.
function Invoke-Trivy {
    param(
        [Parameter(Mandatory)][string[]] $Arguments,
        [Parameter(Mandatory)][string] $ReportName)

    & docker run --rm `
        --volume "${repositoryRoot}:/work:ro" `
        --volume "${imageRoot}:/images:ro" `
        --volume "${evidenceRoot}:/evidence" `
        --volume "${trivyCacheDirectory}:/root/.cache/trivy" `
        $trivyImage `
        @Arguments
    $trivyExit = $LASTEXITCODE

    $reportPath = Join-Path $evidenceRoot $ReportName
    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
        throw ("Trivy could not complete '$ReportName' (exit $trivyExit) and wrote no report. " +
               'This is a scan failure, not a finding -- the gate could not look. ' +
               'Check stderr above for a timeout or a cache problem.')
    }
    if ($trivyExit -ne 0) {
        throw "Trivy exited $trivyExit for '$ReportName'; its report is not trusted."
    }

    return (Get-Content -Raw -LiteralPath $reportPath | ConvertFrom-Json)
}

# Set-StrictMode is on, and a clean scan omits every level rather than emitting an empty one --
# Results is absent just as Secrets and Vulnerabilities are. Existence-check each hop.
function Get-TrivyFindings {
    param([Parameter(Mandatory)] $Report, [Parameter(Mandatory)][string] $Property)

    if (-not ($Report.PSObject.Properties.Name -contains 'Results')) { return @() }
    if ($null -eq $Report.Results) { return @() }

    $found = @()
    foreach ($result in @($Report.Results)) {
        if (-not ($result.PSObject.Properties.Name -contains $Property)) { continue }
        if ($null -eq $result.$Property) { continue }
        $found += @($result.$Property)
    }
    return $found
}

function Remove-CandidateImage {
    param([Parameter(Mandatory)][string] $Image)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $inspectionJson = & docker image inspect $Image 2>$null
        $inspectionExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    if ($inspectionExitCode -ne 0) {
        return
    }

    $inspection = ($inspectionJson | ConvertFrom-Json)[0]
    if ($inspection.Config.Labels.'org.opencontainers.image.source' -ne $repositoryUrl -or
        $inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision -or
        [string]::IsNullOrWhiteSpace($releaseVersion) -or
        $inspection.Config.Labels.'org.opencontainers.image.version' -ne $releaseVersion) {
        throw "Refusing to remove unowned release-candidate image '$Image'."
    }

    & docker image rm --force $Image
    if ($LASTEXITCODE -ne 0) {
        throw "Could not remove release-candidate image '$Image'."
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($packageToken)) {
        throw 'GITHUB_PACKAGES_TOKEN is required for Search release-candidate verification.'
    }

    New-Item -ItemType Directory -Path $releaseRoot | Out-Null
    $releaseRootCreated = $true
    try {
        Write-Utf8NoBom -Path $markerPath -Value 'Concertable.Search release candidate'
    }
    catch {
        Remove-Item -LiteralPath $releaseRoot -Recurse -Force
        $releaseRootCreated = $false
        throw
    }
    New-Item -ItemType Directory -Path $packageRoot, $imageRoot, $evidenceRoot | Out-Null

    foreach ($project in $packageProjects) {
        Invoke-WithPackageToken -Token $packageToken -Action {
            & dotnet restore $project --force-evaluate
            if ($LASTEXITCODE -ne 0) {
                throw "Search package restore failed for '$project' with exit code $LASTEXITCODE."
            }
        }

        & dotnet build $project --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Search package build failed for '$project' with exit code $LASTEXITCODE."
        }

        & dotnet pack $project --configuration $Configuration --no-build --no-restore --output $packageRoot
        if ($LASTEXITCODE -ne 0) {
            throw "Search package creation failed for '$project' with exit code $LASTEXITCODE."
        }
    }

    Invoke-WithPackageToken -Token $packageToken -Action {
        & (Join-Path $PSScriptRoot 'verify-package-candidates.ps1') -PackageDirectory $packageRoot
    }

    $packages = @(Get-ChildItem -LiteralPath $packageRoot -Filter '*.nupkg' -File |
        Where-Object Name -NotLike '*.symbols.nupkg')
    $packageRecords = @($packages | ForEach-Object {
        $identity = Get-NuGetIdentity -Package $_
        [ordered]@{
            id = $identity.Id
            version = $identity.Version
            artifact = Get-ArtifactRecord -Path $_.FullName
        }
    } | Sort-Object { $_.id })
    $actualPackageIds = @($packageRecords | ForEach-Object { $_.id })
    if (($actualPackageIds -join ',') -ne (($expectedPackageIds | Sort-Object) -join ',')) {
        throw "Unexpected Search release-candidate package set '$($actualPackageIds -join ',')'."
    }

    $versions = @($packageRecords | ForEach-Object { $_.version } | Sort-Object -Unique)
    if ($versions.Count -ne 1 -or $versions[0] -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
        throw "Search packages do not share one valid SemVer version: '$($versions -join ',')'."
    }
    if ([version] ($versions[0].Split('-', 2)[0]) -lt [version] '0.1.0') {
        throw "Search release-candidate version '$($versions[0])' is below the independent 0.1.0 baseline."
    }
    $releaseVersion = $versions[0]

    $cleanConsumerPath = Join-Path $evidenceRoot 'clean-consumer.json'
    Write-Utf8NoBom -Path $cleanConsumerPath -Value ([ordered]@{
        status = 'succeeded'
        version = $releaseVersion
        packages = $expectedPackageIds
    } | ConvertTo-Json -Depth 4)

    Invoke-WithPackageToken -Token $packageToken -Action {
        & (Join-Path $PSScriptRoot 'verify-search-images.ps1') `
            -WebImage $webImage `
            -WorkersImage $workersImage `
            -MigrationsImage $migrationsImage `
            -BuildVersion $releaseVersion `
            -KeepImages
    }
    $packageToken = $null

    Initialize-TrivyCache
    # --timeout is a backstop against a pathological walk, not the fix; the persistent cache is.
    $sourceReport = Invoke-Trivy -ReportName 'source-secrets.json' -Arguments @(
        'filesystem', '--scanners', 'secret', '--format', 'json', '--timeout', '30m',
        '--output', '/evidence/source-secrets.json', '--no-progress',
        '--skip-dirs', '/work/.git',
        '--skip-dirs', '/work/.vs',
        '--skip-dirs', '/work/artifacts',
        '--skip-dirs', '/work/**/bin',
        '--skip-dirs', '/work/**/obj',
        '/work'
    )
    $sourceSecrets = Get-TrivyFindings -Report $sourceReport -Property 'Secrets'
    if ($sourceSecrets.Count -gt 0) {
        throw "Source secret scan found $($sourceSecrets.Count) secret(s); see source-secrets.json."
    }

    $imageEvidence = @(
        [ordered]@{ Image = $webImage; Repository = 'ghcr.io/concertable/search-web'; File = 'search-web' },
        [ordered]@{ Image = $workersImage; Repository = 'ghcr.io/concertable/search-workers'; File = 'search-workers' },
        [ordered]@{ Image = $migrationsImage; Repository = 'ghcr.io/concertable/search-migrations'; File = 'search-migrations' }
    )

    foreach ($item in $imageEvidence) {
        $archivePath = Join-Path $imageRoot "$($item.File).tar"
        $vulnerabilityPath = Join-Path $evidenceRoot "$($item.File)-vulnerabilities.json"
        $secretPath = Join-Path $evidenceRoot "$($item.File)-secrets.json"
        $sbomPath = Join-Path $evidenceRoot "$($item.File).cdx.json"

        & docker image save --output $archivePath $item.Image
        if ($LASTEXITCODE -ne 0 -or (Get-Item -LiteralPath $archivePath).Length -eq 0) {
            throw "Could not save release-candidate image '$($item.Image)'."
        }

        $vulnReport = Invoke-Trivy -ReportName "$($item.File)-vulnerabilities.json" -Arguments @(
            'image', '--scanners', 'vuln', '--severity', 'CRITICAL', '--format', 'json', '--timeout', '30m',
            '--output', "/evidence/$($item.File)-vulnerabilities.json", '--no-progress', '--input', "/images/$($item.File).tar"
        )
        $criticals = Get-TrivyFindings -Report $vulnReport -Property 'Vulnerabilities'
        if ($criticals.Count -gt 0) {
            throw "Image '$($item.Image)' has $($criticals.Count) CRITICAL vulnerability(ies)."
        }

        $imageSecretReport = Invoke-Trivy -ReportName "$($item.File)-secrets.json" -Arguments @(
            'image', '--scanners', 'secret', '--format', 'json', '--timeout', '30m',
            '--output', "/evidence/$($item.File)-secrets.json", '--no-progress', '--input', "/images/$($item.File).tar"
        )
        $imageSecrets = Get-TrivyFindings -Report $imageSecretReport -Property 'Secrets'
        if ($imageSecrets.Count -gt 0) {
            throw "Image '$($item.Image)' contains $($imageSecrets.Count) secret(s)."
        }

        Invoke-Trivy -ReportName "$($item.File).cdx.json" -Arguments @(
            'image', '--format', 'cyclonedx', '--timeout', '30m', '--output', "/evidence/$($item.File).cdx.json",
            '--no-progress', '--input', "/images/$($item.File).tar"
        ) | Out-Null

        $sbom = Get-Content -Raw -LiteralPath $sbomPath | ConvertFrom-Json
        if ($sbom.bomFormat -ne 'CycloneDX' -or @($sbom.components).Count -eq 0) {
            throw "Release-candidate SBOM validation failed for '$($item.Image)'."
        }

        $item.archive = Get-ArtifactRecord -Path $archivePath
        $item.sbom = Get-ArtifactRecord -Path $sbomPath
        $item.vulnerabilities = Get-ArtifactRecord -Path $vulnerabilityPath
        $item.secrets = Get-ArtifactRecord -Path $secretPath
    }

    $imageRecords = @($imageEvidence | ForEach-Object {
        $inspection = Get-ImageInspection -Image $_.Image
        [ordered]@{
            repository = $_.Repository
            version = $releaseVersion
            sourceRevision = $revision
            intendedTags = @($releaseVersion, $revision)
            localImageId = [string] $inspection.Id
            archive = $_.archive
            sbom = $_.sbom
            criticalVulnerabilityScan = $_.vulnerabilities
            allSeveritySecretScan = $_.secrets
        }
    })

    $manifest = [ordered]@{
        schemaVersion = 1
        repository = $repositoryUrl
        sourceRevision = $revision
        version = $releaseVersion
        packages = $packageRecords
        images = $imageRecords
        cleanConsumer = Get-ArtifactRecord -Path $cleanConsumerPath
        sourceSecretScan = Get-ArtifactRecord -Path (Join-Path $evidenceRoot 'source-secrets.json')
    }
    Write-Utf8NoBom -Path $manifestPath -Value ($manifest | ConvertTo-Json -Depth 12)

    $verifiedManifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    if ($verifiedManifest.sourceRevision -ne $revision -or
        $verifiedManifest.version -ne $releaseVersion -or
        @($verifiedManifest.packages).Count -ne 1 -or
        @($verifiedManifest.images).Count -ne 3) {
        throw 'Search release-candidate manifest validation failed.'
    }

    $expectedArtifactPaths = @(
        $packageRecords.artifact.path
        $imageRecords.archive.path
        $imageRecords.sbom.path
        $imageRecords.criticalVulnerabilityScan.path
        $imageRecords.allSeveritySecretScan.path
        $manifest.cleanConsumer.path
        $manifest.sourceSecretScan.path
    ) | Sort-Object
    $releasePrefix = $releaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $actualArtifactPaths = @(Get-ChildItem -LiteralPath $releaseRoot -Force -Recurse -File |
        Where-Object FullName -NotIn @($markerPath, $manifestPath) |
        ForEach-Object { $_.FullName.Substring($releasePrefix.Length).Replace('\', '/') } |
        Sort-Object)
    if (($actualArtifactPaths -join "`n") -ne ($expectedArtifactPaths -join "`n")) {
        throw "Release-candidate bundle contains unmanifested files: '$($actualArtifactPaths -join ', ')'."
    }

    $completed = $true
    Write-Host "Verified Search release candidate $releaseVersion for revision ${revision}: 1 package, 3 images, manifest and evidence complete."
    if ($KeepArtifacts) {
        Write-Host "Retained release-candidate artifacts at '$releaseRoot'."
    }
}
finally {
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    $packageToken = $null

    try {
        foreach ($image in $candidateImages) {
            Remove-CandidateImage -Image $image
        }
    }
    finally {
        # The Trivy cache directory is deliberately NOT removed here. Deleting it is the bug: a cache
        # recreated empty every run makes every scan cold, which is what made a scan that never ran
        # look like a secret finding. Do not add a cleanup for it.
        if ((Test-Path -LiteralPath $markerPath -PathType Leaf) -and (-not $KeepArtifacts -or -not $completed)) {
            Assert-ReleaseRoot
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force
            $releaseRootCreated = $false
        }
        elseif ($releaseRootCreated -and -not (Test-Path -LiteralPath $markerPath)) {
            Remove-Item -LiteralPath $releaseRoot -Recurse -Force -ErrorAction SilentlyContinue
            $releaseRootCreated = $false
        }
    }
}
