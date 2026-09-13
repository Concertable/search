[CmdletBinding()]
param(
    [string] $WebImage = "concertable/search-web:verification-$PID",
    [string] $WorkersImage = "concertable/search-workers:verification-$PID",
    [string] $MigrationsImage = "concertable/search-migrations:verification-$PID",
    [string] $BuildVersion,
    [switch] $KeepImages
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryUrl = 'https://github.com/Concertable/search'
$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($revision)) {
    throw 'Could not resolve the Search source revision.'
}

$workingTreeChanges = @(& git -C $repositoryRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect the Search working tree.'
}
if ($workingTreeChanges.Count -gt 0) {
    throw 'Refusing to build Search images from a dirty or untracked working tree because OCI revision metadata must identify the exact content.'
}

if ([string]::IsNullOrWhiteSpace($BuildVersion)) {
    $BuildVersion = "0.0.0-local.$($revision.Substring(0, 12))"
}

$packageToken = $env:GITHUB_PACKAGES_TOKEN
Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
if ([string]::IsNullOrWhiteSpace($packageToken)) {
    throw 'GITHUB_PACKAGES_TOKEN is required to restore Search packages during the image builds.'
}

$verificationId = [Guid]::NewGuid().ToString('N')
$builtImages = [System.Collections.Generic.List[string]]::new()

function Invoke-DockerBuildWithPackageToken {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [Parameter(Mandatory)]
        [string] $Token
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'docker'
    $startInfo.UseShellExecute = $false
    $startInfo.Environment['GITHUB_PACKAGES_TOKEN'] = $Token
    if ($startInfo.PSObject.Properties.Name -contains 'ArgumentList') {
        foreach ($argument in $Arguments) {
            $null = $startInfo.ArgumentList.Add($argument)
        }
    }
    else {
        $startInfo.Arguments = ($Arguments | ForEach-Object {
            if ($_ -match '[\s"]') {
                '"' + $_.Replace('"', '\"') + '"'
            }
            else {
                $_
            }
        }) -join ' '
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw 'Could not start Docker.'
    }

    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Docker build failed with exit code $($process.ExitCode)."
    }
}

function Get-ImageInspection {
    param([Parameter(Mandatory)][string] $Image)

    $json = & docker image inspect $Image
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect image '$Image'."
    }

    return ($json | ConvertFrom-Json)[0]
}

function Assert-ImageTagAvailable {
    param([Parameter(Mandatory)][string] $Image)

    $imageIds = @(& docker image ls --quiet --no-trunc --filter "reference=$Image")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not check whether image tag '$Image' is available."
    }

    if ($imageIds.Count -gt 0) {
        throw "Refusing to overwrite existing image tag '$Image'."
    }
}

function Assert-ImageMetadata {
    param(
        [Parameter(Mandatory)]
        [string] $Image,

        [Parameter(Mandatory)]
        [string] $ExpectedAssembly,

        [Parameter(Mandatory)]
        [string] $ExpectedRuntime
    )

    $inspection = Get-ImageInspection -Image $Image
    if ([string]::IsNullOrWhiteSpace([string] $inspection.Config.User) -or $inspection.Config.User -in @('0', 'root')) {
        throw "Image '$Image' does not declare a non-root user."
    }

    $entrypoint = @($inspection.Config.Entrypoint)
    if ($entrypoint.Count -ne 2 -or $entrypoint[0] -ne 'dotnet' -or $entrypoint[1] -ne $ExpectedAssembly) {
        throw "Image '$Image' has unexpected entrypoint '$($entrypoint -join ' ')'."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.source' -ne $repositoryUrl) {
        throw "Image '$Image' does not identify the canonical Search repository."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.revision' -ne $revision) {
        throw "Image '$Image' does not identify source revision '$revision'."
    }

    if ($inspection.Config.Labels.'org.opencontainers.image.version' -ne $BuildVersion) {
        throw "Image '$Image' does not identify build version '$BuildVersion'."
    }

    $configuredEnvironment = @($inspection.Config.Env) -join "`n"
    if ($configuredEnvironment -match 'GITHUB_PACKAGES_TOKEN') {
        throw "Image '$Image' retains the package-token environment variable."
    }

    $history = (& docker history --no-trunc --format '{{.CreatedBy}}' $Image) -join "`n"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect image history for '$Image'."
    }

    if ($history.IndexOf($packageToken, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Image '$Image' history contains the package credential."
    }

    $runtimeOutput = (& docker run --rm --entrypoint dotnet $Image --list-runtimes) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $runtimeOutput -notmatch $ExpectedRuntime) {
        throw "Image '$Image' does not contain its expected .NET 10 runtime."
    }
}

function Remove-VerifiedImages {
    foreach ($image in $builtImages) {
        $inspection = Get-ImageInspection -Image $image
        if ($inspection.Config.Labels.'com.concertable.search.image-verification' -ne $verificationId) {
            throw "Refusing to remove image '$image' because it is not owned by this verification run."
        }

        & docker image rm --force $image
        if ($LASTEXITCODE -ne 0) {
            throw "Could not remove verification image '$image'."
        }
    }
}

try {
    $images = @($WebImage, $WorkersImage, $MigrationsImage)
    if (($images | Sort-Object -Unique).Count -ne $images.Count) {
        throw 'WebImage, WorkersImage, and MigrationsImage must use distinct tags.'
    }

    foreach ($image in $images) {
        Assert-ImageTagAvailable -Image $image
    }

    $commonArguments = @(
        'build',
        '--file', (Join-Path $repositoryRoot 'Dockerfile'),
        '--build-arg', "VCS_REF=$revision",
        '--build-arg', "BUILD_VERSION=$BuildVersion",
        '--label', "com.concertable.search.image-verification=$verificationId",
        '--pull',
        '--secret', 'id=GITHUB_PACKAGES_TOKEN,env=GITHUB_PACKAGES_TOKEN'
    )

    $targets = @(
        @{ Target = 'search-web'; Image = $WebImage },
        @{ Target = 'search-workers'; Image = $WorkersImage },
        @{ Target = 'search-migrations'; Image = $MigrationsImage }
    )

    foreach ($target in $targets) {
        Invoke-DockerBuildWithPackageToken `
            -Token $packageToken `
            -Arguments ($commonArguments + @(
                '--target', $target.Target,
                '--tag', $target.Image,
                $repositoryRoot
            ))
        $builtImages.Add($target.Image)
    }

    Assert-ImageMetadata -Image $WebImage -ExpectedAssembly 'Concertable.Search.Web.dll' -ExpectedRuntime 'Microsoft\.AspNetCore\.App 10\.'
    Assert-ImageMetadata -Image $WorkersImage -ExpectedAssembly 'Concertable.Search.Workers.dll' -ExpectedRuntime 'Microsoft\.AspNetCore\.App 10\.'
    Assert-ImageMetadata -Image $MigrationsImage -ExpectedAssembly 'Concertable.Search.Migrations.dll' -ExpectedRuntime 'Microsoft\.AspNetCore\.App 10\.'
    $packageToken = $null

    Write-Host "Verified Search images for revision ${revision}: $($images -join ', ')."
}
finally {
    Remove-Item Env:GITHUB_PACKAGES_TOKEN -ErrorAction SilentlyContinue
    $packageToken = $null
    if (-not $KeepImages -and $builtImages.Count -gt 0) {
        Remove-VerifiedImages
    }
}
