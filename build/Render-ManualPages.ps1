# This Source Code Form is subject to the terms of the MIT License.
# If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
# Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
# All Rights Reserved.

<#
.SYNOPSIS
Renders pages of a PDF to PNG with the PDF API that ships with Windows.

.DESCRIPTION
Some pages of the reference calculator's manual carry their worked examples only as images, so a text extractor
returns nothing for them. This script renders them for reading. It needs Windows PowerShell 5.1 (the Windows Runtime
projection) and nothing else. The manual stays local (docs/*.pdf is gitignored), and so do the images: write them
outside the repository.

A page number is the PDF's own, counted from 1. In the local manual it is the printed page number plus one.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File build/Render-ManualPages.ps1 -PdfPath docs/reference-manual_VI.pdf -Pages 137,138 -OutDir $env:TEMP/manual
#>
param(
    [Parameter(Mandatory)] [string] $PdfPath,
    [Parameter(Mandatory)] [int[]] $Pages,
    [Parameter(Mandatory)] [string] $OutDir,
    [int] $Width = 1400
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Storage.StorageFolder, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime]
$null = [Windows.Data.Pdf.PdfPageRenderOptions, Windows.Data.Pdf, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.IRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]

$extensions = [System.WindowsRuntimeSystemExtensions].GetMethods()
$asTaskOperation = $extensions | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
$asTaskAction = $extensions | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' } | Select-Object -First 1

function Await($operation, [Type] $resultType) {
    $task = $asTaskOperation.MakeGenericMethod($resultType).Invoke($null, @($operation))
    $task.Wait(-1) | Out-Null
    return $task.Result
}

function AwaitAction($action) {
    $task = $asTaskAction.Invoke($null, @($action))
    $task.Wait(-1) | Out-Null
}

# The Windows Runtime file API takes absolute paths only.
$PdfPath = (Resolve-Path -LiteralPath $PdfPath).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path -LiteralPath $OutDir).Path

$file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($PdfPath)) ([Windows.Storage.StorageFile])
$pdf = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
$folder = Await ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync($OutDir)) ([Windows.Storage.StorageFolder])
"pages in document: $($pdf.PageCount)"

foreach ($number in $Pages) {
    if ($number -lt 1 -or $number -gt $pdf.PageCount) { throw "Page $number is outside 1..$($pdf.PageCount)." }
    $page = $pdf.GetPage([uint32]($number - 1))
    $target = Await ($folder.CreateFileAsync("page-$number.png", [Windows.Storage.CreationCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])
    $stream = Await ($target.OpenAsync([Windows.Storage.FileAccessMode]::ReadWrite)) ([Windows.Storage.Streams.IRandomAccessStream])
    $options = New-Object Windows.Data.Pdf.PdfPageRenderOptions
    $options.DestinationWidth = [uint32]$Width
    AwaitAction ($page.RenderToStreamAsync($stream, $options))
    $stream.Dispose()
    $page.Dispose()
    "rendered page $number"
}
