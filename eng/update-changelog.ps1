#! /usr/bin/env pwsh
param(
    [Parameter(Mandatory = $true)][string] $ReleaseVersion,
    [Parameter(Mandatory = $true)][string] $ReleaseNotesText,
    [Parameter(Mandatory = $true)][string] $GitHubServerUrl
)

$ErrorActionPreference = "Stop"

if ($ReleaseVersion.StartsWith("v")) {
    $ReleaseVersion = $ReleaseVersion.Substring(1)
}

Write-Output "Updating CHANGELOG for v$ReleaseVersion"

$repo = Join-Path $PSScriptRoot ".."
$changelog = Join-Path $repo "CHANGELOG.md"

$lines = Get-Content $changelog
$lines = [System.Collections.Generic.List[string]]$lines

$entry = [System.Collections.Generic.List[string]]@(
    "",
    "## ${ReleaseVersion}",
    ""
)

$releaseNotes = $ReleaseNotesText -split "\r?\n"
$hasBullets = $false
foreach ($line in $releaseNotes) {
    if ($line -eq "") {
        continue
    }
    # Skip anything before the first bullet, such as GitHub's generated comment and the section heading
    if ($line.StartsWith("##")) {
        if ($hasBullets) {
            break
        }
        continue
    }
    if (!$line.StartsWith("* ")) {
        continue
    }

    # Update the user's login to link to their GitHub profile
    $line = $line -Replace "\@(([a-zA-Z0-9\-]+))", ('[@$1](' + $GitHubServerUrl + '/$1)')

    $entry.Add($line)
    $hasBullets = $true
}

$index = $lines.IndexOf("<!-- next-release -->")
$lines.InsertRange($index + 1, $entry)

$lines | Set-Content $changelog
