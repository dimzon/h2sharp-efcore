<#
.SYNOPSIS
    Measures the coverage of the provider and holds the threshold.

.DESCRIPTION
    The collector of coverlet writes one report per test project, and every report covers
    the provider alone. This script merges the reports: a line of the provider is covered
    when at least one of the projects ran it, and the merged number is the number the
    threshold is held against.

    The coverage is measured in Debug: in Release the inlining of the compiler smears the
    lines, and the number stops meaning anything.

.PARAMETER ResultsDirectory
    The directory the collector wrote the reports into.

.PARAMETER Threshold
    The least line rate the provider may have. The run fails when the merged report is
    below it. The threshold is set by the first measurement of the merged report and only
    goes up: it is 0.50, and the current ModelTests measurement is 0.5027.

.PARAMETER Report
    Where the merged report is written. The report is not written when the path is empty.

.EXAMPLE
    pwsh tools/CheckCoverage.ps1 -ResultsDirectory TestResults
#>
[CmdletBinding()]
param(
    [string] $ResultsDirectory = "TestResults",
    [double] $Threshold = 0.58,
    [string] $Report = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ResultsDirectory)) {
    Write-Error "The directory of the reports does not exist: $ResultsDirectory"
}

$files = Get-ChildItem -Path $ResultsDirectory -Filter "coverage.cobertura.xml" -Recurse -File

if ($files.Count -eq 0) {
    Write-Error "There is no report of the coverage in $ResultsDirectory"
}

# Every report holds the same classes of the provider, and every class holds its lines with
# the number of the hits. The lines of a class are merged by their number: a line is covered
# when at least one of the projects ran it.
$classes = @{}

foreach ($file in $files) {
    $coverage = [xml](Get-Content $file.FullName)

    foreach ($class in $coverage.coverage.packages.package.classes.class) {
        $name = $class.name

        if (-not $classes.ContainsKey($name)) {
            $classes[$name] = @{ Valid = [System.Collections.Generic.HashSet[int]]::new(); Covered = [System.Collections.Generic.HashSet[int]]::new() }
        }

        foreach ($line in $class.lines.line) {
            $number = [int] $line.number

            [void] $classes[$name].Valid.Add($number)

            if ([int] $line.hits -gt 0) {
                [void] $classes[$name].Covered.Add($number)
            }
        }
    }
}

$valid = 0
$covered = 0
$branchesValid = 0
$branchesCovered = 0

foreach ($class in $classes.Values) {
    $valid += $class.Valid.Count
    $covered += $class.Covered.Count
}

if ($valid -eq 0) {
    Write-Error "The reports hold no line of the provider"
}

$rate = $covered / $valid

Write-Host ("Coverage of the provider: {0} of {1} lines ({2:P2})" -f $covered, $valid, $rate)
Write-Host ("Threshold: {0:P2}" -f $Threshold)

if ($Report -ne "") {
    $merged = @"
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="$rate" branch-rate="0" lines-covered="$covered" lines-valid="$valid" branches-covered="$branchesCovered" branches-valid="$branchesValid" />
"@

    Set-Content -Path $Report -Value $merged
    Write-Host "The merged report is written to $Report"
}

if ($rate -lt $Threshold) {
    Write-Error ("The coverage of the provider is below the threshold: {0:P2} < {1:P2}" -f $rate, $Threshold)
    exit 1
}

Write-Host "The coverage of the provider is above the threshold."
