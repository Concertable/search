[CmdletBinding()]
param(
    [string] $InfraPath,
    [string] $AspireCommand
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RequiredProperty([hashtable] $Value, [string] $Name) {
    if (-not $Value.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace([string] $Value[$Name])) {
        throw "The Infra dependency pin is missing '$Name'."
    }

    return [string] $Value[$Name]
}

function Invoke-Git([string] $RepositoryPath, [string[]] $Arguments) {
    $output = & git -C $RepositoryPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed for '$RepositoryPath': $($output -join [Environment]::NewLine)"
    }

    return @($output)
}

function Assert-GitExitCode([string] $RepositoryPath, [string[]] $Arguments, [int[]] $AllowedExitCodes) {
    $output = & git -C $RepositoryPath @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($AllowedExitCodes -notcontains $exitCode) {
        throw "git $($Arguments -join ' ') failed for '$RepositoryPath': $($output -join [Environment]::NewLine)"
    }

    return $exitCode
}

function Get-AbsolutePath([string] $Path, [string] $Description) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Description is required."
    }

    try {
        return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
    }
    catch {
        throw "$Description '$Path' does not exist."
    }
}

function Assert-InfraSource([string] $RequestedInfraPath, [hashtable] $Dependency) {
    $infraRoot = Get-AbsolutePath $RequestedInfraPath '-InfraPath'
    if (-not (Test-Path -LiteralPath $infraRoot -PathType Container)) {
        throw "-InfraPath '$infraRoot' is not a directory."
    }

    $repositoryRoot = Get-AbsolutePath ((Invoke-Git $infraRoot @('rev-parse', '--show-toplevel') | Select-Object -Last 1).Trim()) 'Infra Git root'
    if (-not [string]::Equals($infraRoot, $repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "-InfraPath must be the Infra repository root, not '$infraRoot'."
    }

    $expectedRemote = Get-RequiredProperty $Dependency 'InfraRepository'
    $actualRemote = (Invoke-Git $infraRoot @('remote', 'get-url', 'origin') | Select-Object -Last 1).Trim()
    if ($actualRemote -ne $expectedRemote) {
        throw "-InfraPath origin must be '$expectedRemote'."
    }

    $revision = Get-RequiredProperty $Dependency 'Revision'
    Invoke-Git $infraRoot @('cat-file', '-e', "$revision^{commit}") | Out-Null

    $labRelativePath = Get-RequiredProperty $Dependency 'LabRelativePath'
    $labScript = Join-Path $infraRoot (Join-Path $labRelativePath 'verify.ps1')
    if (-not (Test-Path -LiteralPath $labScript -PathType Leaf)) {
        throw "The pinned Infra lab script is missing: '$labScript'."
    }

    $sourceDifference = Assert-GitExitCode $infraRoot @('diff', '--quiet', $revision, '--', $labRelativePath) @(0, 1)
    if ($sourceDifference -ne 0) {
        throw "Infra lab '$labRelativePath' differs from pinned revision '$revision'."
    }

    $worktreeStatus = Invoke-Git $infraRoot @('status', '--porcelain=v1', '--untracked-files=all', '--', $labRelativePath)
    if (@($worktreeStatus | Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) }).Count -ne 0) {
        throw "Infra lab '$labRelativePath' has local modifications."
    }

    return [pscustomobject]@{
        Root = $infraRoot
        LabScript = $labScript
        ReceiptPath = Join-Path $infraRoot (Get-RequiredProperty $Dependency 'ReceiptRelativePath')
    }
}

function Resolve-AspireCommand([string] $ExplicitCommand) {
    if (-not [string]::IsNullOrWhiteSpace($ExplicitCommand)) {
        return Get-AbsolutePath $ExplicitCommand '-AspireCommand'
    }

    $command = Get-Command aspire -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw "The native Aspire CLI was not found on PATH. Install it or supply -AspireCommand."
    }

    return $command.Source
}

function Assert-NoExistingAppHost([string] $ResolvedAspireCommand, [string] $AppHostPath) {
    $output = & $ResolvedAspireCommand 'ps' '--format' 'Json' '--non-interactive' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Aspire could not report running AppHosts: $($output -join [Environment]::NewLine)"
    }

    try {
        $applications = @($output | ConvertFrom-Json)
    }
    catch {
        throw 'Aspire returned an unrecognised AppHost status response.'
    }

    foreach ($application in $applications) {
        # Aspire CLI 13.6.1 serializes AppHostDisplayInfo with camel-case names.
        # appHostPath is required by that schema; reject an unknown shape rather
        # than treating a running AppHost as absent.
        $pathProperty = $application.PSObject.Properties['appHostPath']
        if ($null -eq $pathProperty) {
            throw 'Aspire returned an AppHost with no path; refusing to start an unknown workload.'
        }

        $reportedPath = Get-AbsolutePath ([string] $pathProperty.Value) 'Aspire AppHost path'
        if ([string]::Equals((Split-Path -Leaf $reportedPath), (Split-Path -Leaf $AppHostPath), [StringComparison]::OrdinalIgnoreCase)) {
            throw "A Search AppHost is already running at '$reportedPath'."
        }
    }
}

function Assert-NoLabCollision([hashtable] $Dependency) {
    $docker = Get-Command docker -CommandType Application, Function -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $docker) {
        throw 'Docker is required to preflight the Infra lab.'
    }

    foreach ($resource in @(
            @{ Kind = 'container'; Name = Get-RequiredProperty $Dependency 'EmulatorContainer' },
            @{ Kind = 'network'; Name = Get-RequiredProperty $Dependency 'EmulatorNetwork' })) {
        $output = & docker $resource.Kind 'inspect' $resource.Name 2>&1
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0) {
            throw "The Infra lab $($resource.Kind) '$($resource.Name)' already exists."
        }
        $diagnostic = $output -join [Environment]::NewLine
        $missingResource = $diagnostic -match '(?i)no such (object|container|network)'
        if ($resource.Kind -eq 'network') {
            $missingResource = $missingResource -or $diagnostic -match "(?i)network $([regex]::Escape($resource.Name)) not found(?:\r?\n|$)"
        }
        if ($exitCode -ne 1 -or -not $missingResource) {
            throw "Could not determine whether the Infra lab $($resource.Kind) '$($resource.Name)' exists: $($output -join [Environment]::NewLine)"
        }
    }
}

function Invoke-LabWithoutAzureCredentials([string] $LabScript) {
    $credentialNames = @(Get-ChildItem Env: |
        Where-Object { $_.Name -match '^(ARM|AZURE)_' } |
        ForEach-Object Name)
    $originalCredentials = @{}
    foreach ($name in $credentialNames) {
        $originalCredentials[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        Remove-Item -LiteralPath "Env:$name"
    }

    try {
        & pwsh '-NoProfile' '-File' $LabScript
        if ($LASTEXITCODE -ne 0) {
            throw "Infra lab exited with code $LASTEXITCODE."
        }
    }
    finally {
        foreach ($name in @(Get-ChildItem Env: |
                Where-Object { $_.Name -match '^(ARM|AZURE)_' } |
                ForEach-Object Name)) {
            Remove-Item -LiteralPath "Env:$name"
        }
        foreach ($name in $originalCredentials.Keys) {
            [Environment]::SetEnvironmentVariable($name, $originalCredentials[$name], 'Process')
        }
    }
}

function Assert-FreshPassedReceipt([string] $ReceiptPath, [DateTimeOffset] $InvocationStartedAt) {
    if (-not (Test-Path -LiteralPath $ReceiptPath -PathType Leaf)) {
        throw "Infra lab did not produce a verification receipt at '$ReceiptPath'."
    }

    if ((Get-Item -LiteralPath $ReceiptPath).LastWriteTimeUtc -lt $InvocationStartedAt.UtcDateTime) {
        throw "Infra lab verification receipt at '$ReceiptPath' predates this invocation."
    }

    try {
        $receipt = Get-Content -LiteralPath $ReceiptPath -Raw | ConvertFrom-Json
        $startedAt = [DateTimeOffset] $receipt.started_at_utc
        $finishedAt = [DateTimeOffset] $receipt.finished_at_utc
    }
    catch {
        throw "Infra lab verification receipt at '$ReceiptPath' is invalid."
    }

    if ($receipt.result -ne 'passed') {
        throw "Infra lab verification receipt result is '$($receipt.result)', not 'passed'."
    }
    if ($startedAt -lt $InvocationStartedAt -or $finishedAt -lt $InvocationStartedAt) {
        throw "Infra lab verification receipt at '$ReceiptPath' predates this invocation."
    }
}

function Invoke-StartLocal([string] $RequestedInfraPath, [string] $ExplicitAspireCommand, [hashtable] $Dependency) {
    $infra = Assert-InfraSource $RequestedInfraPath $Dependency
    $aspire = Resolve-AspireCommand $ExplicitAspireCommand
    $appHost = Get-AbsolutePath (Join-Path $PSScriptRoot '..\local\AppHost\Concertable.Search.AppHost.csproj') 'Search AppHost'

    Assert-NoExistingAppHost $aspire $appHost
    Assert-NoLabCollision $Dependency

    $startedAt = [DateTimeOffset]::UtcNow
    Invoke-LabWithoutAzureCredentials $infra.LabScript
    Assert-FreshPassedReceipt $infra.ReceiptPath $startedAt

    Assert-NoLabCollision $Dependency
    Assert-NoExistingAppHost $aspire $appHost
    & $aspire 'run' '--apphost' $appHost
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $exception = [InvalidOperationException]::new("Aspire run exited with code $exitCode.")
        $exception.Data['AspireExitCode'] = $exitCode
        throw $exception
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    try {
        if ([string]::IsNullOrWhiteSpace($InfraPath)) {
            throw '-InfraPath is required.'
        }

        $dependency = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'start-local.infra.psd1')
        Invoke-StartLocal $InfraPath $AspireCommand $dependency
        exit 0
    }
    catch {
        $aspireExitCode = $_.Exception.Data['AspireExitCode']
        if ($aspireExitCode -is [int]) {
            exit $aspireExitCode
        }

        Write-Error $_
        exit 1
    }
}
