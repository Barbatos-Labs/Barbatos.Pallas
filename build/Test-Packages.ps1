# This Source Code Form is subject to the terms of the MIT License.
# If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
# Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
# All Rights Reserved.

<#
.SYNOPSIS
    Installs the packed packages the way a user would and calculates with them.

.DESCRIPTION
    A package that packs cleanly can still be unusable: an assembly it should carry left out, a type that does not
    load, a dependency that does not resolve. None of that shows in dotnet build or dotnet test, which run on the
    projects of the repository and never on the package. So two programs outside the repository - one on
    Barbatos.Pallas.Engine alone, one on Barbatos.Pallas.DependencyInjection - restore the packages from the folder
    they were packed into, name a public type of every assembly the package carries, and calculate on every runtime
    the packages target.

    The folder must hold exactly the two published packages: anything else is a package that should not exist.
    NuGet restores into a cache of its own under the work folder, because the global cache would already hold a
    package of the same version packed earlier, and that one would be tested instead.

.PARAMETER PackageDirectory
    The folder dotnet pack wrote the packages to.

.EXAMPLE
    ./build/Test-Packages.ps1 -PackageDirectory artifacts/packages
#>
[CmdletBinding()]
param(
    [string] $PackageDirectory = (Join-Path $PSScriptRoot '..\artifacts\packages')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$published = @('Barbatos.Pallas.Engine', 'Barbatos.Pallas.DependencyInjection')
$frameworks = @('net8.0', 'net9.0', 'net10.0')

$PackageDirectory = (Resolve-Path $PackageDirectory).Path
$packages = @(Get-ChildItem -Path $PackageDirectory -Filter *.nupkg)
$ids = @($packages | ForEach-Object { $_.Name -replace '\.\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?\.nupkg$', '' } | Sort-Object)
if (($ids -join ',') -ne (($published | Sort-Object) -join ',')) {
    throw "Expected exactly the packages $($published -join ', ') in $PackageDirectory, found: $($ids -join ', ')."
}

$engine = $packages | Where-Object { $_.Name -like 'Barbatos.Pallas.Engine.*' }
$version = $engine.Name -replace '^Barbatos\.Pallas\.Engine\.', '' -replace '\.nupkg$', ''

# The consumer takes the same Microsoft.Extensions.DependencyInjection the repository pins.
$pins = Get-Content (Join-Path $PSScriptRoot '..\Directory.Packages.props') -Raw
$di = [regex]::Match($pins, '<PackageVersion Include="Microsoft\.Extensions\.DependencyInjection" Version="([^"]+)"').Groups[1].Value
if (-not $di) { throw 'Microsoft.Extensions.DependencyInjection has no pin in Directory.Packages.props.' }

# Outside the repository, so none of its Directory.Build.props, central package versions or global.json applies.
$work = Join-Path ([IO.Path]::GetTempPath()) ('pallas-packages-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$env:NUGET_PACKAGES = Join-Path $work 'nuget-cache'

Set-Content -Path (Join-Path $work 'nuget.config') -Encoding UTF8 -Value @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="packed" value="$PackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@

function New-Consumer([string] $name, [string] $references, [string] $program) {
    $directory = Join-Path $work $name
    New-Item -ItemType Directory -Path $directory | Out-Null
    Set-Content -Path (Join-Path $directory "$name.csproj") -Encoding UTF8 -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>$($frameworks -join ';')</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@
    # The C# is plain ASCII and names the calculator's symbols by code point (Symbols below), so neither the
    # encoding PowerShell writes nor the one it reads this script in can change what a test says.
    Set-Content -Path (Join-Path $directory 'Program.cs') -Encoding UTF8 -Value @"
$program

// "/" is the fraction bar, "sqrt" the square root and "*" the multiplication sign.
static string Symbols(string text) => text
    .Replace("/", char.ConvertFromUtf32(0x231F))
    .Replace("sqrt", char.ConvertFromUtf32(0x221A))
    .Replace("*", char.ConvertFromUtf32(0x00D7));

static void Expect(CalculatorSession session, string input, string expected)
{
    string actual = session.Calculate(Symbols(input)).Display.Text;
    Console.WriteLine(input + " = " + actual);
    if (actual != Symbols(expected))
    {
        throw new InvalidOperationException(input + " was " + actual + ", expected " + Symbols(expected) + ".");
    }
}

static void Carried(params Type[] types)
{
    foreach (Type type in types)
    {
        Console.WriteLine("carries " + type.Assembly.GetName().Name + " " + type.Assembly.GetName().Version);
    }
}
"@
    return $directory
}

$engineConsumer = New-Consumer 'EngineConsumer' @"
    <PackageReference Include="Barbatos.Pallas.Engine" Version="$version" />
"@ @'
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

// Engine alone: no dependency, and every assembly it carries can be named.
Carried(typeof(Barbatos.Pallas.Numerics.Trigonometry), typeof(ExpressionParser),
    typeof(Barbatos.Pallas.LinearAlgebra.ExactLinearAlgebra), typeof(Barbatos.Pallas.Statistics.ExactSample),
    typeof(Barbatos.Pallas.Solvers.PolynomialRoots));

CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.Calculate);
Expect(session, "2/3+1/1/2", "13/6");
Expect(session, "sqrt(2)*3", "3sqrt(2)");
Expect(session, "sin(30)", "1/2");
'@

$diConsumer = New-Consumer 'DependencyInjectionConsumer' @"
    <PackageReference Include="Barbatos.Pallas.DependencyInjection" Version="$version" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$di" />
"@ @'
using Barbatos.Pallas.DependencyInjection;
using Barbatos.Pallas.Engine;
using Microsoft.Extensions.DependencyInjection;

// DependencyInjection brings Engine as a dependency and carries the reference data and the spreadsheet.
Carried(typeof(Barbatos.Pallas.Data.ConstantSets), typeof(Barbatos.Pallas.Spreadsheet.SpreadsheetGrid));

ServiceCollection services = new();
services.AddPallas();
using ServiceProvider provider = services.BuildServiceProvider();
CalculatorSession session = provider.GetRequiredService<CalculatorSession>();
Expect(session, "2/3+1/1/2", "13/6");
Expect(session, "@c*2", "599584916");
'@

try {
    foreach ($consumer in @($engineConsumer, $diConsumer)) {
        $name = Split-Path $consumer -Leaf
        Write-Host "== $name ($version)"
        & dotnet build $consumer --configuration Release --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "$name does not build against the packages." }

        foreach ($framework in $frameworks) {
            Write-Host "-- $framework"
            & dotnet run --project $consumer --configuration Release --framework $framework --no-build
            if ($LASTEXITCODE -ne 0) { throw "$name failed on $framework." }
        }
    }

    Write-Host "The packages install and calculate on $($frameworks -join ', ')."
}
finally {
    Remove-Item -Path $work -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue
}
