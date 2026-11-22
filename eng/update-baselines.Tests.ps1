#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.0.0' }

BeforeAll {
    $script:ScriptUnderTest = Join-Path $PSScriptRoot "update-baselines.ps1"

    # Copies the script into a throwaway repository root, so it finds the baselines below through $PSScriptRoot/../src.
    function Initialize-RepositoryRoot {
        $root = Join-Path $TestDrive ([Guid]::NewGuid().ToString())
        $eng = Join-Path $root "eng"
        New-Item -ItemType Directory -Path $eng | Out-Null
        Copy-Item $script:ScriptUnderTest $eng
        return $root
    }

    # Writes a project's baselines into src/<Project>/.PublicAPI, the folder the analyzers read them from.
    function Write-PublicApiBaseline {
        param(
            [Parameter(Mandatory = $true)][string] $Root,
            [Parameter(Mandatory = $true)][string] $Project,
            [Parameter(Mandatory = $true)][string[]] $Shipped,
            [Parameter(Mandatory = $true)][string[]] $Unshipped
        )

        $publicApi = Join-Path $Root "src" $Project ".PublicAPI"
        New-Item -ItemType Directory -Path $publicApi | Out-Null
        Set-Content -Path (Join-Path $publicApi "PublicAPI.Shipped.txt") -Value $Shipped
        Set-Content -Path (Join-Path $publicApi "PublicAPI.Unshipped.txt") -Value $Unshipped
        return $publicApi
    }

    function Invoke-BaselineUpdate {
        param(
            [Parameter(Mandatory = $true)][string] $Root
        )

        & (Join-Path $Root "eng" "update-baselines.ps1") | Out-Null
    }
}

Describe "update-baselines.ps1" {

    It "UpdateBaselines_UnshippedEntryInPublicApiFolder_MovesEntryToShipped" {
        # Arrange
        $root = Initialize-RepositoryRoot
        $publicApi = Write-PublicApiBaseline -Root $root -Project "Library" `
            -Shipped @("#nullable enable", "Library.Alpha", "Library.Charlie") `
            -Unshipped @("#nullable enable", "Library.Bravo")

        # Act
        Invoke-BaselineUpdate -Root $root

        # Assert
        $shipped = Get-Content (Join-Path $publicApi "PublicAPI.Shipped.txt")
        $unshipped = Get-Content (Join-Path $publicApi "PublicAPI.Unshipped.txt")
        ($shipped -join "|") | Should -BeExactly "#nullable enable|Library.Alpha|Library.Bravo|Library.Charlie"
        ($unshipped -join "|") | Should -BeExactly "#nullable enable"
    }

    It "UpdateBaselines_NothingUnshipped_LeavesBaselinesUnchanged" {
        # Arrange
        $root = Initialize-RepositoryRoot
        $publicApi = Write-PublicApiBaseline -Root $root -Project "Library" `
            -Shipped @("#nullable enable", "Library.Alpha") `
            -Unshipped @("#nullable enable")
        $shippedPath = Join-Path $publicApi "PublicAPI.Shipped.txt"
        $unshippedPath = Join-Path $publicApi "PublicAPI.Unshipped.txt"
        $shippedBefore = [System.IO.File]::ReadAllBytes($shippedPath)
        $unshippedBefore = [System.IO.File]::ReadAllBytes($unshippedPath)

        # Act
        Invoke-BaselineUpdate -Root $root

        # Assert
        [System.IO.File]::ReadAllBytes($shippedPath) | Should -Be $shippedBefore
        [System.IO.File]::ReadAllBytes($unshippedPath) | Should -Be $unshippedBefore
    }
}
