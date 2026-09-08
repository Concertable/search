<# Re-scaffold only this owner's InitialCreate migrations. -WhatIf previews; -Check detects model drift. #>
[CmdletBinding(SupportsShouldProcess)]
param([string]$Context, [switch]$Check)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'tools/OwnerOperations.psm1') -Force
$manifest = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'migrations.psd1')
Invoke-OwnerMigrations -Root $PSScriptRoot -Manifest $manifest -Context $Context -Check:$Check -WhatIf:$WhatIfPreference
