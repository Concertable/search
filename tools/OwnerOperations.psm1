# Canonical source: Concertable/platform-dotnet tools/OwnerOperations.psm1.
# Monorepo source: api/Concertable.Shared/tools/OwnerOperations.psm1.
# Vendored copies travel with each owner. scripts/sync-owner-tooling.ps1 -Check verifies byte parity.
Set-StrictMode -Version Latest

function Resolve-OwnerPath {
    param([string]$Root, [string]$RelativePath)
    $rootPath = [IO.Path]::GetFullPath($Root)
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $RelativePath))
    $prefix = $rootPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $comparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (-not $path.StartsWith($prefix, $comparison)) {
        throw "Path must stay inside owner root: $RelativePath"
    }
    $cursor = $path
    while ($cursor.Length -ge $rootPath.Length) {
        if ((Test-Path -LiteralPath $cursor) -and
            ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Owner operations cannot traverse a link: $cursor"
        }
        $cursor = Split-Path $cursor -Parent
    }
    return $path
}

function Get-NormalizedMigrationFiles {
    param([string]$Directory)
    $files = @{}
    if (Test-Path -LiteralPath $Directory) {
        foreach ($file in Get-ChildItem -LiteralPath $Directory -File) {
            $key = $file.Name -replace '^\d{14}_', ''
            $files[$key] = (Get-Content -LiteralPath $file.FullName -Raw) -replace '\d{14}(?=_InitialCreate)', 'TIMESTAMP'
        }
    }
    return $files
}

function Test-MigrationUnchanged {
    param([string]$OldDirectory, [string]$NewDirectory)
    $old = Get-NormalizedMigrationFiles $OldDirectory
    $new = Get-NormalizedMigrationFiles $NewDirectory
    if ($old.Count -eq 0 -or $old.Count -ne $new.Count) { return $false }
    foreach ($key in $old.Keys) {
        if (-not $new.ContainsKey($key) -or $old[$key] -cne $new[$key]) { return $false }
    }
    return $true
}

function Invoke-OwnerMigrations {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][hashtable]$Manifest,
        [string]$Context,
        [switch]$Check
    )
    $ErrorActionPreference = 'Stop'
    $migrations = @($Manifest.Migrations | Where-Object { -not $Context -or $_.Context -eq $Context })
    if ($migrations.Count -eq 0) { throw "Unknown migration context: $Context" }
    # Resolve every path before changing any migration directory.
    $operations = @(
        foreach ($migration in $migrations) {
            $project = Resolve-OwnerPath $Root $migration.Project
            $startup = Resolve-OwnerPath $Root $migration.StartupProject
            $directory = Resolve-OwnerPath $project $migration.OutputDir
            if (-not (Test-Path -LiteralPath $project -PathType Container) -or
                -not (Test-Path -LiteralPath $startup -PathType Container)) {
                throw "Migration project or startup project is missing: $($migration.Context)"
            }
            [pscustomobject]@{ Migration = $migration; Project = $project; Startup = $startup; Directory = $directory }
        }
    )
    $savedEnvironment = @{}
    try {
        foreach ($name in $Manifest.Environment.Keys) {
            $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $Manifest.Environment[$name], 'Process')
        }
        foreach ($operation in $operations) {
            $migration = $operation.Migration
            $arguments = @('--context', $migration.Context, '--project', $operation.Project, '--startup-project', $operation.Startup)
            if ($Check) {
                if ($PSCmdlet.ShouldProcess($migration.Context, 'Check pending EF model changes')) {
                    & dotnet ef migrations has-pending-model-changes @arguments
                    if ($LASTEXITCODE -ne 0) { throw "Model check failed for $($migration.Context) (exit $LASTEXITCODE)." }
                }
                continue
            }
            if (-not $PSCmdlet.ShouldProcess($operation.Directory, "Re-scaffold $($migration.Context) InitialCreate")) { continue }
            $directory = $operation.Directory
            # Keep C# backups outside the project so the EF build cannot compile duplicate snapshots.
            # A direct child of the owner root shares the checkout volume even when OS temp does not.
            $backup = Resolve-OwnerPath $Root (".migration-backup-" + [guid]::NewGuid().ToString('N'))
            if ([IO.Path]::GetPathRoot($backup) -ne [IO.Path]::GetPathRoot($directory)) { throw 'Migration backup must share the project volume.' }
            $hasBackup = Test-Path -LiteralPath $directory
            if ($hasBackup) { Move-Item -LiteralPath $directory -Destination $backup }
            try {
                & dotnet ef migrations add InitialCreate @arguments --output-dir $migration.OutputDir
                if ($LASTEXITCODE -ne 0) { throw "Scaffolding failed for $($migration.Context) (exit $LASTEXITCODE)." }
                if (@(Get-ChildItem -LiteralPath $directory -Filter '*ModelSnapshot.cs' -File).Count -ne 1) {
                    throw "Scaffolding did not produce a model snapshot for $($migration.Context)."
                }
                if ($hasBackup -and (Test-MigrationUnchanged $backup $directory)) {
                    Remove-Item -LiteralPath $directory -Recurse -Force
                    Move-Item -LiteralPath $backup -Destination $directory
                    Write-Host "$($migration.Context) unchanged - kept existing migration ID."
                } elseif ($hasBackup) {
                    Remove-Item -LiteralPath $backup -Recurse -Force
                }
            } catch {
                if (Test-Path -LiteralPath $directory) { Remove-Item -LiteralPath $directory -Recurse -Force }
                if ($hasBackup -and (Test-Path -LiteralPath $backup)) { Move-Item -LiteralPath $backup -Destination $directory }
                throw
            }
        }
    } finally {
        foreach ($name in $savedEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
        }
    }
}

function Initialize-OwnerDevelopment {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$AppHostProject,
        [string[]]$SettingsProjects = @(),
        [Parameter(Mandatory)][string[]]$SecretKeys
    )
    $ErrorActionPreference = 'Stop'
    $project = Resolve-OwnerPath $Root $AppHostProject
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "AppHost project missing: $project" }
    [xml]$projectXml = Get-Content -LiteralPath $project -Raw
    if (-not $projectXml.SelectSingleNode('//UserSecretsId')) { throw "AppHost must declare UserSecretsId: $project" }
    foreach ($settingsProject in $SettingsProjects) {
        $target = Resolve-OwnerPath $Root "$settingsProject/appsettings.Development.json"
        $example = "$target.example"
        if (-not (Test-Path -LiteralPath $example)) { throw "Development template missing: $example" }
        if (-not (Test-Path -LiteralPath $target) -and $PSCmdlet.ShouldProcess($target, 'Create from .example')) {
            Copy-Item -LiteralPath $example -Destination $target
        }
    }
    # WhatIf must work on a clean clone without an SDK, restore, or user-secrets access.
    if ($WhatIfPreference) {
        foreach ($key in $SecretKeys) { $null = $PSCmdlet.ShouldProcess("$project :: $key", 'Set local default if missing') }
        return
    }
    $existing = @(& dotnet user-secrets list --project $project 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Cannot read AppHost user-secrets (exit $LASTEXITCODE)." }
    foreach ($key in $SecretKeys) {
        if ($existing | Where-Object { "$_" -match ('^' + [regex]::Escape($key) + '\s*=') }) { continue }
        if ($PSCmdlet.ShouldProcess("$project :: $key", 'Set local default if missing')) {
            & dotnet user-secrets set $key 'local-dev-shared-service-secret' --project $project | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Cannot set AppHost user-secret $key (exit $LASTEXITCODE)." }
        }
    }
}

Export-ModuleMember -Function Invoke-OwnerMigrations, Initialize-OwnerDevelopment
