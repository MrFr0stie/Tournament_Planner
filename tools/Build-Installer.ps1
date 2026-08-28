[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [string]$InnoCompiler
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'DartLeague.csproj'
$installerFile = Join-Path $projectRoot 'installer\Dartboard.iss'
$installerOutput = Join-Path $projectRoot 'installer\Output'

function Get-ReleaseVersion {
    param([string]$RequestedVersion)

    if ($RequestedVersion) {
        return ([Version]$RequestedVersion).ToString(3)
    }

    [xml]$project = Get-Content -LiteralPath $projectFile
    $baseVersion = $project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $baseVersion) {
        throw 'No <Version> was found in DartLeague.csproj.'
    }

    $candidate = [Version]$baseVersion
    while (Test-Path -LiteralPath (Join-Path $installerOutput "Dartboard-Setup-$($candidate.ToString(3)).exe")) {
        $candidate = [Version]::new($candidate.Major, $candidate.Minor, $candidate.Build + 1)
    }

    return $candidate.ToString(3)
}

function Find-InnoCompiler {
    param([string]$RequestedCompiler)

    if ($RequestedCompiler) {
        if (-not (Test-Path -LiteralPath $RequestedCompiler)) {
            throw "Inno Setup compiler was not found: $RequestedCompiler"
        }
        return $RequestedCompiler
    }

    $fromPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    return $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

$releaseVersion = Get-ReleaseVersion -RequestedVersion $Version
$compiler = Find-InnoCompiler -RequestedCompiler $InnoCompiler
if (-not $compiler) {
    throw 'Inno Setup 6 was not found. Install it, then run this script again or pass -InnoCompiler with the full path to ISCC.exe.'
}

Write-Host "Publishing Dartboard $releaseVersion..."
& dotnet publish $projectFile -c Release -p:PublishProfile=WindowsInstaller "-p:Version=$releaseVersion" "-p:AssemblyVersion=$releaseVersion.0" "-p:FileVersion=$releaseVersion.0"
if ($LASTEXITCODE -ne 0) {
    throw 'Publishing the Windows installer build failed.'
}

Write-Host "Creating installer Dartboard-Setup-$releaseVersion.exe..."
& $compiler "/DMyAppVersion=$releaseVersion" $installerFile
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed.'
}

Write-Host "Release installer created: $(Join-Path $installerOutput "Dartboard-Setup-$releaseVersion.exe")"
