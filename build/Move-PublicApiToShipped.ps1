# This Source Code Form is subject to the terms of the MIT License.
# If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
# Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
# All Rights Reserved.

<#
.SYNOPSIS
    Moves what every core library's PublicAPI.Unshipped.txt declares into its PublicAPI.Shipped.txt: what a release
    does to the public API.

.DESCRIPTION
    Microsoft.CodeAnalysis.PublicApiAnalyzers reads both files beside a csproj: what shipped in a release, and what has
    been added or removed since. A release commits to what it ships (docs/ARCHITECTURE.md §7), so before its tag the
    entries of Unshipped move into Shipped, where removing or changing one is an incompatible change, and Unshipped is
    left with its header alone. An entry of Unshipped that begins with *REMOVED* takes that entry out of Shipped
    instead. Shipped is written in ordinal order, so the same entries always make the same file.

    The release workflow refuses a release while any Unshipped file still declares an entry.

.PARAMETER CoreDirectory
    The folder of the core libraries.

.EXAMPLE
    ./build/Move-PublicApiToShipped.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $CoreDirectory = (Join-Path $PSScriptRoot '..\src\core')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$header = '#nullable enable'
$removed = '*REMOVED*'
$encoding = New-Object System.Text.UTF8Encoding($false)

function Read-Entries([string] $path) {
    return @([IO.File]::ReadAllLines($path) | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne $header })
}

function Write-Entries([string] $path, [string[]] $entries) {
    # LF and a final newline, as .gitattributes and the analyzer's own code fix write them.
    $text = (@($header) + $entries) -join "`n"
    [IO.File]::WriteAllText($path, $text + "`n", $encoding)
}

$libraries = @(Get-ChildItem -Path $CoreDirectory -Recurse -Filter PublicAPI.Unshipped.txt)
if ($libraries.Count -eq 0) { throw "No PublicAPI.Unshipped.txt under $CoreDirectory." }

# Every library is worked out before any file is written, so an entry that cannot move leaves every file as it was.
$moves = @()
foreach ($unshippedFile in $libraries) {
    $name = $unshippedFile.Directory.Name
    $shippedPath = Join-Path $unshippedFile.DirectoryName 'PublicAPI.Shipped.txt'
    if (-not (Test-Path $shippedPath)) { throw "$name has no PublicAPI.Shipped.txt." }

    $unshipped = @(Read-Entries $unshippedFile.FullName)
    if ($unshipped.Count -eq 0) { continue }

    $shipped = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($entry in Read-Entries $shippedPath) { [void] $shipped.Add($entry) }

    foreach ($entry in $unshipped) {
        if ($entry.StartsWith($removed, [StringComparison]::Ordinal)) {
            $gone = $entry.Substring($removed.Length)
            if (-not $shipped.Remove($gone)) { throw "$name removes '$gone', which never shipped." }
        }
        elseif (-not $shipped.Add($entry)) {
            throw "$name declares '$entry' in both files."
        }
    }

    $sorted = [string[]] @($shipped)
    [Array]::Sort($sorted, [StringComparer]::Ordinal)
    $moves += [pscustomobject] @{ Name = $name; Shipped = $shippedPath; Unshipped = $unshippedFile.FullName; Entries = $sorted; Moved = $unshipped.Count }
}

$moved = 0
foreach ($move in $moves) {
    if ($PSCmdlet.ShouldProcess($move.Name, "Move $($move.Moved) public API entries to PublicAPI.Shipped.txt")) {
        Write-Entries $move.Shipped $move.Entries
        Write-Entries $move.Unshipped @()
    }

    Write-Host "$($move.Name): $($move.Moved) entries moved, $($move.Entries.Count) shipped in all."
    $moved += $move.Moved
}

Write-Host "$moved public API entries moved to PublicAPI.Shipped.txt."
