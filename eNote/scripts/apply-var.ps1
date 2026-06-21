# Replaces inferable explicit local types with 'var'.
# Skips: method params, switch arms, null/default inits, collection expressions, target-typed new().

param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$typePattern = '(?:var|[A-Z][\w<>,\.\?\[\]]+|int|string|bool|long|double|float|decimal|byte|char)'
$linePattern = "^(?<indent>\s+)(?<type>$typePattern)\s+(?<name>\w+)\s*=\s*(?<init>.+)$"

function Should-SkipVarReplacement {
    param(
        [string]$Line,
        [string]$Type,
        [string]$Init
    )

    if ($Type -eq 'var') { return $true }

    $trimmed = $Line.TrimStart()
    foreach ($prefix in @('foreach ', 'using ', 'catch ', 'fixed ')) {
        if ($trimmed.StartsWith($prefix)) { return $true }
    }

    # Switch pattern arm: AppException appEx =>
    if ($trimmed -match '^\S+\s+\w+\s*=>') { return $true }

    # Method/interface parameter default: Type name = default) or = null)
    if ($Init -match '^(null|default)\)?;?$' -or $Line -match '\)\s*;?\s*$' -and $Line -match '\w+\s*=') {
        if ($Line -match '= (null|default)\)?') { return $true }
    }

    $initTrim = $Init.TrimEnd(';').Trim()
    if ($initTrim -eq 'null' -or $initTrim -eq 'default') { return $true }

    # Collection expressions need an explicit target type
    if ($initTrim.StartsWith('[')) { return $true }

    # Target-typed new() / new(...) needs explicit type on LHS
    if ($initTrim -eq 'new()' -or $initTrim -match '^new\(') { return $true }

    # Switch expressions need a common explicit type
    if ($initTrim -match '\bswitch\b') { return $true }

    # Numeric widening: double x = 0
    if ($Type -in @('double', 'float', 'decimal') -and $initTrim -match '^\d+$') { return $true }

    return $false
}

$changedFiles = 0
$changedLines = 0
$skippedRoot = @('bin', 'obj', 'node_modules')

Get-ChildItem -Path $Root -Filter '*.cs' -Recurse |
    Where-Object {
        $rel = $_.FullName.Substring($Root.Length).TrimStart('\', '/')
        -not ($skippedRoot | Where-Object { $rel -like "$_*\*" -or $rel -like "*\$_\*" })
    } |
    ForEach-Object {
        $lines = [System.IO.File]::ReadAllLines($_.FullName)
        $fileChanged = $false

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -notmatch $linePattern) { continue }

            $indent = $Matches['indent']
            $type = $Matches['type']
            $name = $Matches['name']
            $init = $Matches['init']

            if (Should-SkipVarReplacement -Line $line -Type $type -Init $init) { continue }

            $lines[$i] = "${indent}var $name = $init"
            $fileChanged = $true
            $changedLines++
        }

        if ($fileChanged) {
            [System.IO.File]::WriteAllLines($_.FullName, $lines)
            $changedFiles++
            Write-Host "Updated: $($_.FullName.Substring($Root.Length))"
        }
    }

Write-Host ""
Write-Host "Done. Changed $changedLines lines in $changedFiles files."