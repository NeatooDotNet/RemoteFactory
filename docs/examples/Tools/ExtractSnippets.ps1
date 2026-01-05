<#
.SYNOPSIS
    Extracts code snippets from C# files based on region markers for documentation.

.DESCRIPTION
    Scans C# files in the docs/examples directory for #region markers with 'docs-' prefix
    and extracts the code within those regions. Generates a JSON mapping file and optionally
    individual snippet files that can be included in markdown documentation.

.PARAMETER OutputPath
    Directory to output extracted snippets. Default: docs/snippets

.PARAMETER Format
    Output format: 'json' (default), 'files', or 'both'

.EXAMPLE
    .\ExtractSnippets.ps1
    Extracts snippets to default location as JSON

.EXAMPLE
    .\ExtractSnippets.ps1 -OutputPath "../snippets" -Format both
    Extracts snippets to custom location in both JSON and file formats
#>

param(
    [string]$OutputPath = "$PSScriptRoot/../../snippets",
    [ValidateSet('json', 'files', 'both')]
    [string]$Format = 'json'
)

$ErrorActionPreference = 'Stop'

# Directory containing the example source files
$SourceDir = "$PSScriptRoot/.."

# Pattern for finding docs regions
$RegionPattern = '^\s*#region\s+(docs-[\w-]+)\s*$'
$EndRegionPattern = '^\s*#endregion'

function Get-CodeSnippets {
    param([string]$FilePath)

    $content = Get-Content -Path $FilePath -Raw
    $lines = Get-Content -Path $FilePath
    $snippets = @{}

    $currentRegion = $null
    $currentLines = @()
    $inDocsRegion = $false

    foreach ($line in $lines) {
        if ($line -match $RegionPattern) {
            $currentRegion = $Matches[1]
            $inDocsRegion = $true
            $currentLines = @()
            continue
        }

        if ($inDocsRegion -and $line -match $EndRegionPattern) {
            if ($currentRegion -and $currentLines.Count -gt 0) {
                # Remove leading/trailing empty lines and normalize indentation
                $code = NormalizeCode -Lines $currentLines
                $snippets[$currentRegion] = @{
                    Name = $currentRegion
                    SourceFile = (Get-Item $FilePath).Name
                    SourcePath = $FilePath
                    Code = $code
                    LineCount = $currentLines.Count
                }
            }
            $currentRegion = $null
            $inDocsRegion = $false
            continue
        }

        if ($inDocsRegion) {
            $currentLines += $line
        }
    }

    return $snippets
}

function NormalizeCode {
    param([string[]]$Lines)

    if ($Lines.Count -eq 0) { return "" }

    # Remove leading empty lines
    while ($Lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($Lines[0])) {
        $Lines = $Lines[1..($Lines.Count - 1)]
    }

    # Remove trailing empty lines
    while ($Lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($Lines[-1])) {
        $Lines = $Lines[0..($Lines.Count - 2)]
    }

    if ($Lines.Count -eq 0) { return "" }

    # Find minimum indentation (ignoring empty lines)
    $minIndent = [int]::MaxValue
    foreach ($line in $Lines) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            $leadingSpaces = ($line -replace '^(\s*).*$', '$1').Length
            if ($leadingSpaces -lt $minIndent) {
                $minIndent = $leadingSpaces
            }
        }
    }

    # Remove common indentation
    $normalized = @()
    foreach ($line in $Lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            $normalized += ""
        }
        elseif ($line.Length -ge $minIndent) {
            $normalized += $line.Substring($minIndent)
        }
        else {
            $normalized += $line
        }
    }

    return ($normalized -join "`n")
}

function Export-Snippets {
    param(
        [hashtable]$AllSnippets,
        [string]$OutputPath,
        [string]$Format
    )

    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }

    if ($Format -eq 'json' -or $Format -eq 'both') {
        # Create JSON file with all snippets
        $jsonOutput = @{
            GeneratedAt = (Get-Date -Format 'o')
            SnippetCount = $AllSnippets.Count
            Snippets = @{}
        }

        foreach ($key in $AllSnippets.Keys) {
            $snippet = $AllSnippets[$key]
            $jsonOutput.Snippets[$key] = @{
                SourceFile = $snippet.SourceFile
                LineCount = $snippet.LineCount
                Code = $snippet.Code
            }
        }

        $jsonPath = Join-Path $OutputPath "snippets.json"
        $jsonOutput | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
        Write-Host "Generated: $jsonPath"
    }

    if ($Format -eq 'files' -or $Format -eq 'both') {
        # Create individual snippet files
        foreach ($key in $AllSnippets.Keys) {
            $snippet = $AllSnippets[$key]
            $snippetPath = Join-Path $OutputPath "$key.cs"
            $snippet.Code | Set-Content -Path $snippetPath -Encoding UTF8
            Write-Host "Generated: $snippetPath"
        }
    }
}

# Main execution
Write-Host "Extracting documentation snippets from $SourceDir"
Write-Host ""

$allSnippets = @{}
$csFiles = Get-ChildItem -Path $SourceDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj|Generated)\\' }

foreach ($file in $csFiles) {
    $snippets = Get-CodeSnippets -FilePath $file.FullName

    if ($snippets.Count -gt 0) {
        Write-Host "Found $($snippets.Count) snippet(s) in $($file.Name)"

        foreach ($key in $snippets.Keys) {
            if ($allSnippets.ContainsKey($key)) {
                Write-Warning "Duplicate snippet key: $key"
                Write-Warning "  First: $($allSnippets[$key].SourceFile)"
                Write-Warning "  Second: $($snippets[$key].SourceFile)"
            }
            else {
                $allSnippets[$key] = $snippets[$key]
            }
        }
    }
}

Write-Host ""
Write-Host "Total snippets found: $($allSnippets.Count)"
Write-Host ""

# Export snippets
Export-Snippets -AllSnippets $allSnippets -OutputPath $OutputPath -Format $Format

Write-Host ""
Write-Host "Extraction complete!"

# Output summary by category
$categories = @{}
foreach ($key in $allSnippets.Keys) {
    # Extract category from key (e.g., 'docs-quick-start-person-model' -> 'quick-start')
    if ($key -match '^docs-([^-]+(?:-[^-]+)?)-') {
        $category = $Matches[1]
        if (-not $categories.ContainsKey($category)) {
            $categories[$category] = @()
        }
        $categories[$category] += $key
    }
}

Write-Host ""
Write-Host "Snippets by category:"
foreach ($category in $categories.Keys | Sort-Object) {
    Write-Host "  $category`: $($categories[$category].Count) snippets"
}
