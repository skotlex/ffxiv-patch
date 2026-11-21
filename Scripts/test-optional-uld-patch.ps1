$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

$testDir = Join-Path $repoRoot "Tools\OptionalUldPatchTests"
$outDir = Join-Path $testDir "bin"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sources = @(
    (Join-Path $repoRoot "FFXIVPatchGenerator\OptionalUldPatch.cs"),
    (Join-Path $repoRoot "FFXIVPatchGenerator\PartyBonusRoleFontPatch.cs"),
    (Join-Path $repoRoot "FFXIVPatchGenerator\DutyFinderRoleFontPatch.cs"),
    (Join-Path $repoRoot "FFXIVPatchGenerator\InventorySubtitleSpacingPatch.cs"),
    (Join-Path $repoRoot "FFXIVPatchGenerator\HashAndEndian.cs"),
    (Join-Path $testDir "OptionalUldPatchTests.cs")
)

$exePath = Join-Path $outDir "OptionalUldPatchTests.exe"
& $csc /nologo /target:exe "/out:$exePath" $sources
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $exePath
exit $LASTEXITCODE
