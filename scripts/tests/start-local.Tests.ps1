[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '..\start-local.ps1')

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Throws([scriptblock] $Action, [string] $Message) {
    try {
        & $Action
    }
    catch {
        return
    }

    throw $Message
}

function global:docker {
    if ($env:START_LOCAL_TEST_DOCKER_MODE -eq 'unavailable') {
        Write-Output 'error during connect: the Docker daemon is unavailable'
        $global:LASTEXITCODE = 1
        return
    }
    if ($env:START_LOCAL_TEST_DOCKER_MODE -eq "leftover-$($args[0])" -and
        (Test-Path -LiteralPath $env:START_LOCAL_TEST_LOG) -and
        ((Get-Content -LiteralPath $env:START_LOCAL_TEST_LOG) -contains 'lab')) {
        Write-Output '[{}]'
        $global:LASTEXITCODE = 0
        return
    }
    if ($args[0] -eq 'network') {
        Write-Output "Error response from daemon: network $($args[2]) not found"
    }
    else {
        Write-Output 'Error: No such object'
    }
    $global:LASTEXITCODE = 1
}

function New-Fixture([string] $Root) {
    $infra = Join-Path $Root 'infra'
    $lab = Join-Path $infra 'labs\azure-local'
    $runtime = Join-Path $lab '.runtime'
    New-Item -ItemType Directory -Force -Path $runtime | Out-Null

    @"
`$ErrorActionPreference = 'Stop'
Add-Content -LiteralPath `$env:START_LOCAL_TEST_LOG -Value 'lab'
if (`$null -ne `$env:ARM_TEST_CREDENTIAL -or `$null -ne `$env:AZURE_TEST_CREDENTIAL) { throw 'Azure credentials reached the lab.' }
if (`$env:START_LOCAL_TEST_LAB_MODE -eq 'failure') { exit 17 }
if (`$env:START_LOCAL_TEST_LAB_MODE -eq 'stale') { exit 0 }
@{ result = 'passed'; started_at_utc = [DateTimeOffset]::UtcNow.ToString('o'); finished_at_utc = [DateTimeOffset]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path `$PSScriptRoot '.runtime/verification.json')
"@ | Set-Content -LiteralPath (Join-Path $lab 'verify.ps1') -Encoding utf8
    '.runtime/' | Set-Content -LiteralPath (Join-Path $lab '.gitignore') -Encoding utf8

    Push-Location $infra
    try {
        & git init --quiet
        & git config user.email 'tests@concertable.invalid'
        & git config user.name 'Search script tests'
        & git add 'labs/azure-local/verify.ps1' 'labs/azure-local/.gitignore'
        & git commit --quiet -m 'Add test lab'
        & git remote add origin 'https://github.com/Concertable/infra.git'
        $revision = (& git rev-parse HEAD).Trim()
    }
    finally {
        Pop-Location
    }

    $aspire = Join-Path $Root 'aspire.ps1'
    @"
Add-Content -LiteralPath `$env:START_LOCAL_TEST_LOG -Value (`$args -join ' ')
if (`$args[0] -eq 'ps') {
    if ([string]::IsNullOrWhiteSpace(`$env:START_LOCAL_TEST_APPHOST_STATUS)) { '[]' }
    else { `$env:START_LOCAL_TEST_APPHOST_STATUS }
    exit 0
}
if (`$args[0] -eq 'run') {
    if (`$env:START_LOCAL_TEST_ASPIRE_RUN_EXIT) { exit [int]`$env:START_LOCAL_TEST_ASPIRE_RUN_EXIT }
    exit 0
}
throw "Unexpected Aspire invocation: `$(`$args -join ' ')"
"@ | Set-Content -LiteralPath $aspire -Encoding utf8

    return [pscustomobject]@{
        Infra = $infra
        Aspire = $aspire
        Dependency = @{
            InfraRepository = 'https://github.com/Concertable/infra.git'
            Revision = $revision
            LabRelativePath = 'labs/azure-local'
            ReceiptRelativePath = 'labs/azure-local/.runtime/verification.json'
            EmulatorContainer = 'concertable-azure-local'
            EmulatorNetwork = 'concertable-azure-local'
        }
    }
}

function New-DependencyPin([pscustomobject] $Fixture, [string] $Path) {
    @"
@{
    InfraRepository = 'https://github.com/Concertable/infra.git'
    Revision = '$($Fixture.Dependency.Revision)'
    LabRelativePath = 'labs/azure-local'
    ReceiptRelativePath = 'labs/azure-local/.runtime/verification.json'
    EmulatorContainer = 'concertable-azure-local'
    EmulatorNetwork = 'concertable-azure-local'
}
"@ | Set-Content -LiteralPath $Path -Encoding utf8
}

function Invoke-Case([string] $Name, [scriptblock] $Test) {
    & $Test
    Write-Host "PASS $Name"
}

$temporaryParent = [IO.Path]::TrimEndingDirectorySeparator((Resolve-Path -LiteralPath ([IO.Path]::GetTempPath())).Path)
$rootName = "search-start-local-" + [guid]::NewGuid().ToString('N')
$root = Join-Path $temporaryParent $rootName
New-Item -ItemType Directory -Path $root | Out-Null
$previousArm = $env:ARM_TEST_CREDENTIAL
$previousAzure = $env:AZURE_TEST_CREDENTIAL
$previousMode = $env:START_LOCAL_TEST_LAB_MODE
$previousLog = $env:START_LOCAL_TEST_LOG
$previousAppHostStatus = $env:START_LOCAL_TEST_APPHOST_STATUS
$previousDockerMode = $env:START_LOCAL_TEST_DOCKER_MODE

try {
    $env:START_LOCAL_TEST_DOCKER_MODE = ''
    Invoke-Case 'runs the lab before Aspire and restores credentials' {
        $fixture = New-Fixture (Join-Path $root 'success')
        $log = Join-Path $root 'success.log'
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'success'
        $env:START_LOCAL_TEST_APPHOST_STATUS = ''
        $env:ARM_TEST_CREDENTIAL = 'preserve-me'
        $env:AZURE_TEST_CREDENTIAL = 'preserve-me-too'

        Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency

        Assert-True ($env:ARM_TEST_CREDENTIAL -eq 'preserve-me') 'ARM credential was not restored.'
        Assert-True ($env:AZURE_TEST_CREDENTIAL -eq 'preserve-me-too') 'AZURE credential was not restored.'
        $lines = [string[]] (Get-Content -LiteralPath $log)
        $labIndex = [Array]::IndexOf($lines, 'lab')
        $runIndex = [Array]::FindIndex($lines, [Predicate[string]] { param($line) $line -like 'run *' })
        Assert-True ($labIndex -ge 0 -and $runIndex -gt $labIndex) 'Aspire started before the lab.'
    }

    Invoke-Case 'stops before the lab when Docker is unavailable' {
        $fixture = New-Fixture (Join-Path $root 'docker-unavailable')
        $log = Join-Path $root 'docker-unavailable.log'
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'success'
        $env:START_LOCAL_TEST_APPHOST_STATUS = ''
        $env:START_LOCAL_TEST_DOCKER_MODE = 'unavailable'
        try {
            Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'An unavailable Docker daemon must stop orchestration.'
            Assert-True (-not ((Get-Content -LiteralPath $log) -match '^lab$|^run ')) 'The lab or Aspire ran with Docker unavailable.'
        }
        finally {
            $env:START_LOCAL_TEST_DOCKER_MODE = ''
        }
    }

    foreach ($kind in @('container', 'network')) {
        Invoke-Case "does not start Aspire when the lab leaves its $kind" {
            $fixture = New-Fixture (Join-Path $root "leftover-$kind")
            $log = Join-Path $root "leftover-$kind.log"
            $env:START_LOCAL_TEST_LOG = $log
            $env:START_LOCAL_TEST_LAB_MODE = 'success'
            $env:START_LOCAL_TEST_APPHOST_STATUS = ''
            $env:START_LOCAL_TEST_DOCKER_MODE = "leftover-$kind"
            try {
                Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } "A remaining lab $kind must stop orchestration."
                $lines = @(Get-Content -LiteralPath $log)
                Assert-True ($lines -contains 'lab') 'The lab never ran, so its cleanup was not checked.'
                Assert-True (-not ($lines -match '^run ')) "Aspire ran while the lab $kind remained."
            }
            finally {
                $env:START_LOCAL_TEST_DOCKER_MODE = ''
            }
        }
    }

    Invoke-Case 'does not start Aspire when the lab fails' {
        $fixture = New-Fixture (Join-Path $root 'failure')
        $log = Join-Path $root 'failure.log'
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'failure'
        $env:START_LOCAL_TEST_APPHOST_STATUS = ''

        Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'A failed lab must stop orchestration.'
        Assert-True (-not (Test-Path -LiteralPath $log) -or -not ((Get-Content -LiteralPath $log) -match '^run ')) 'Aspire ran after a failed lab.'
    }

    Invoke-Case 'does not start Aspire from a stale receipt' {
        $fixture = New-Fixture (Join-Path $root 'stale')
        $log = Join-Path $root 'stale.log'
        $receiptPath = Join-Path $fixture.Infra 'labs\azure-local\.runtime\verification.json'
        @{ result = 'passed'; started_at_utc = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString('o'); finished_at_utc = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString('o') } |
            ConvertTo-Json | Set-Content -LiteralPath $receiptPath
        (Get-Item -LiteralPath $receiptPath).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-5)
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'stale'
        $env:START_LOCAL_TEST_APPHOST_STATUS = ''

        Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'A stale receipt must stop orchestration.'
        Assert-True (-not (Test-Path -LiteralPath $log) -or -not ((Get-Content -LiteralPath $log) -match '^run ')) 'Aspire ran after a stale receipt.'
    }

    Invoke-Case 'rejects a modified lab before it runs' {
        $fixture = New-Fixture (Join-Path $root 'dirty')
        $log = Join-Path $root 'dirty.log'
        Add-Content -LiteralPath (Join-Path $fixture.Infra 'labs\azure-local\verify.ps1') -Value '# dirty'
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'success'
        $env:START_LOCAL_TEST_APPHOST_STATUS = ''

        Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'A modified lab must stop orchestration.'
        Assert-True (-not (Test-Path -LiteralPath $log)) 'The modified lab or Aspire was invoked.'
    }

    Invoke-Case 'does not run the lab for an indeterminate AppHost inventory' {
        $fixture = New-Fixture (Join-Path $root 'unknown-apphost')
        $log = Join-Path $root 'unknown-apphost.log'
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'success'
        $env:START_LOCAL_TEST_APPHOST_STATUS = '[{"name":"unknown"}]'

        Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'An indeterminate AppHost inventory must stop orchestration.'
        Assert-True (-not ((Get-Content -LiteralPath $log) -match '^lab$|^run ')) 'The lab or Aspire ran after an indeterminate AppHost inventory.'
    }

    Invoke-Case 'does not run the lab when another Search worktree is running' {
        $fixture = New-Fixture (Join-Path $root 'running-search')
        $log = Join-Path $root 'running-search.log'
        $otherAppHost = Join-Path $root 'other-search\Concertable.Search.AppHost.csproj'
        New-Item -ItemType Directory -Force -Path (Split-Path $otherAppHost -Parent) | Out-Null
        New-Item -ItemType File -Path $otherAppHost | Out-Null
        $env:START_LOCAL_TEST_LOG = $log
        $env:START_LOCAL_TEST_LAB_MODE = 'success'
        $env:START_LOCAL_TEST_APPHOST_STATUS = (@(@{ appHostPath = $otherAppHost }) | ConvertTo-Json -Compress)

        Assert-Throws { Invoke-StartLocal $fixture.Infra $fixture.Aspire $fixture.Dependency } 'A running Search AppHost must stop orchestration.'
        Assert-True (-not ((Get-Content -LiteralPath $log) -match '^lab$|^run ')) 'The lab or Aspire ran while another Search AppHost was running.'
    }

    Invoke-Case 'propagates a failed Aspire launch from the script entrypoint' {
        $fixture = New-Fixture (Join-Path $root 'launch-failure')
        $log = Join-Path $root 'launch-failure.log'
        $fixtureSearchRoot = Join-Path $root 'launch-failure-search'
        $fixtureScripts = Join-Path $fixtureSearchRoot 'scripts'
        $fixtureAppHost = Join-Path $fixtureSearchRoot 'local\AppHost\Concertable.Search.AppHost.csproj'
        $dockerDirectory = Join-Path $root 'docker-bin'
        New-Item -ItemType Directory -Force -Path $fixtureScripts | Out-Null
        New-Item -ItemType Directory -Force -Path (Split-Path $fixtureAppHost -Parent) | Out-Null
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\start-local.ps1') -Destination (Join-Path $fixtureScripts 'start-local.ps1')
        New-Item -ItemType File -Path $fixtureAppHost | Out-Null
        New-Item -ItemType Directory -Force -Path $dockerDirectory | Out-Null
        @'
@echo Error: No such object
@exit /b 1
'@ | Set-Content -LiteralPath (Join-Path $dockerDirectory 'docker.cmd') -Encoding ascii
        New-DependencyPin $fixture (Join-Path $fixtureScripts 'start-local.infra.psd1')

        $previousPath = $env:PATH
        try {
            $env:PATH = "$dockerDirectory;$previousPath"
            $env:START_LOCAL_TEST_LOG = $log
            $env:START_LOCAL_TEST_LAB_MODE = 'success'
            $env:START_LOCAL_TEST_APPHOST_STATUS = ''
            $env:START_LOCAL_TEST_ASPIRE_RUN_EXIT = '23'

            & pwsh '-NoProfile' '-File' (Join-Path $fixtureScripts 'start-local.ps1') '-InfraPath' $fixture.Infra '-AspireCommand' $fixture.Aspire
            Assert-True ($LASTEXITCODE -eq 23) 'The script entrypoint did not propagate the Aspire launch exit code.'
            Assert-True (@(Get-Content -LiteralPath $log | Where-Object { $_ -match '^run ' }).Count -gt 0) 'The fake Aspire launch was not invoked.'
        }
        finally {
            $env:PATH = $previousPath
            Remove-Item Env:START_LOCAL_TEST_ASPIRE_RUN_EXIT -ErrorAction SilentlyContinue
        }
    }
}
finally {
    $env:ARM_TEST_CREDENTIAL = $previousArm
    $env:AZURE_TEST_CREDENTIAL = $previousAzure
    $env:START_LOCAL_TEST_LAB_MODE = $previousMode
    $env:START_LOCAL_TEST_LOG = $previousLog
    $env:START_LOCAL_TEST_APPHOST_STATUS = $previousAppHostStatus
    $env:START_LOCAL_TEST_DOCKER_MODE = $previousDockerMode
    if (Test-Path -LiteralPath $root) {
        $resolvedRoot = (Resolve-Path -LiteralPath $root).Path
        $expectedRoot = Join-Path $temporaryParent $rootName
        if (-not [string]::Equals($resolvedRoot, $expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove unexpected test directory '$resolvedRoot'."
        }

        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force -ErrorAction Stop
    }
}
