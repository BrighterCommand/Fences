#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.0.0' }

BeforeAll {
    $script:ScriptUnderTest = Join-Path $PSScriptRoot "update-changelog.ps1"

    # Runs the script against a throwaway CHANGELOG.md and returns the lines of the new entry.
    function Invoke-UpdateChangelog {
        param(
            [Parameter(Mandatory = $true)][string] $ReleaseVersion,
            [Parameter(Mandatory = $true)][string] $ReleaseNotesText
        )

        $root = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        $eng = Join-Path $root "eng"
        New-Item -ItemType Directory -Path $eng | Out-Null
        Copy-Item $script:ScriptUnderTest $eng

        $changelog = Join-Path $root "CHANGELOG.md"
        Set-Content -Path $changelog -Value @(
            "# Changelog",
            "",
            "<!-- next-release -->",
            "",
            "## 9.0.0",
            "",
            "* An earlier entry"
        )

        & (Join-Path $eng "update-changelog.ps1") $ReleaseVersion $ReleaseNotesText "https://github.com" | Out-Null

        $raw = [System.IO.File]::ReadAllText($changelog)
        $lines = $raw.Split("`n")
        $start = [Array]::IndexOf($lines, "## $ReleaseVersion")
        $end = [Array]::IndexOf($lines, "## 9.0.0")

        return [PSCustomObject]@{
            Raw   = $raw
            Entry = $lines[($start + 1)..($end - 1)] | Where-Object { $_ -ne "" }
        }
    }
}

Describe "update-changelog.ps1" {

    It "UpdateChangelog_GeneratedNotesWithLeadingComment_CopiesWhatsChangedBullets" {
        # Arrange: the shape GitHub generates for this repository, with CRLF line endings
        $notes = @(
            "<!-- Release notes generated using configuration in .github/release.yml at main -->",
            "",
            "## What's Changed",
            "* Fix one thing by @alice in https://github.com/o/r/pull/1",
            "* Fix another thing by @bob in https://github.com/o/r/pull/2",
            "",
            "## New Contributors",
            "* @carol made their first contribution in https://github.com/o/r/pull/3",
            "",
            "**Full Changelog**: https://github.com/o/r/compare/9.0.0...9.0.1"
        ) -join "`r`n"

        # Act
        $result = Invoke-UpdateChangelog -ReleaseVersion "9.0.1" -ReleaseNotesText $notes

        # Assert
        ($result.Entry -join "|") | Should -BeExactly (@(
                "* Fix one thing by [@alice](https://github.com/alice) in https://github.com/o/r/pull/1",
                "* Fix another thing by [@bob](https://github.com/bob) in https://github.com/o/r/pull/2"
            ) -join "|")
    }

    It "UpdateChangelog_NotesWithCrlfLineEndings_WritesNoCarriageReturns" {
        # Arrange: every line, including each bullet, ends in CRLF
        $notes = @(
            "## What's Changed",
            "",
            "* Fix one thing by @alice in https://github.com/o/r/pull/1",
            "",
            "**Full Changelog**: https://github.com/o/r/compare/9.0.0...9.0.1"
        ) -join "`r`n"

        # Act
        $result = Invoke-UpdateChangelog -ReleaseVersion "9.0.1" -ReleaseNotesText $notes

        # Assert
        $result.Raw | Should -Not -Match "`r"
    }

    It "UpdateChangelog_NotesStartingWithABullet_CopiesTheFirstBullet" {
        # Arrange
        $notes = @(
            "* The first change",
            "* The second change"
        ) -join "`n"

        # Act
        $result = Invoke-UpdateChangelog -ReleaseVersion "9.0.1" -ReleaseNotesText $notes

        # Assert
        ($result.Entry -join "|") | Should -BeExactly "* The first change|* The second change"
    }

    It "UpdateChangelog_HandWrittenNotesWithoutAHeading_CopiesBulletsUntilTheNextHeading" {
        # Arrange: the shape of the hand-written 9.0.0 notes
        $notes = @(
            "The first Fences release.",
            "",
            "* The first change",
            "* The second change",
            "",
            "## The API is unchanged",
            "",
            "* Not part of the entry"
        ) -join "`n"

        # Act
        $result = Invoke-UpdateChangelog -ReleaseVersion "9.0.1" -ReleaseNotesText $notes

        # Assert
        ($result.Entry -join "|") | Should -BeExactly "* The first change|* The second change"
    }

    It "UpdateChangelog_NotesStartingWithWhatsChangedHeading_CopiesBulletsUntilTheNextHeading" {
        # Arrange: the shape of upstream Polly's published notes
        $notes = @(
            "## What's Changed",
            "",
            "* The first change",
            "",
            "## New Contributors",
            "* @carol made their first contribution"
        ) -join "`n"

        # Act
        $result = Invoke-UpdateChangelog -ReleaseVersion "9.0.1" -ReleaseNotesText $notes

        # Assert
        ($result.Entry -join "|") | Should -BeExactly "* The first change"
    }
}
