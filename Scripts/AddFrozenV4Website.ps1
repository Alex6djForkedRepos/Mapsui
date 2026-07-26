[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$WebsitePath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[0-9a-fA-F]{64}$")]
    [string]$ExpectedSha256,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [int]::MaxValue)]
    [int]$ExpectedFileCount
)

$ErrorActionPreference = "Stop"

$resolvedArchivePath = (Resolve-Path -LiteralPath $ArchivePath).Path
$resolvedWebsitePath = (Resolve-Path -LiteralPath $WebsitePath).Path
$v4Path = [System.IO.Path]::GetFullPath((Join-Path $resolvedWebsitePath "v4"))

if ([System.IO.Path]::GetDirectoryName($v4Path) -ne $resolvedWebsitePath) {
    throw "The v4 destination is outside the website directory."
}

if (Test-Path -LiteralPath $v4Path) {
    throw "The v4 destination already exists: $v4Path"
}

$actualSha256 = (Get-FileHash -LiteralPath $resolvedArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $ExpectedSha256.ToLowerInvariant()) {
    throw "Frozen v4 archive checksum mismatch. Expected $ExpectedSha256 but found $actualSha256."
}

$archiveEntries = @(tar -tzf $resolvedArchivePath)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the frozen v4 archive."
}

$unsafeEntries = @($archiveEntries | Where-Object {
    $_ -notlike "v4/*" -or
    $_ -match '(^|/)\.\.(/|$)' -or
    $_ -match '^[/\\]'
})
if ($unsafeEntries.Count -ne 0) {
    throw "The frozen v4 archive contains entries outside the v4 directory."
}

tar -xzf $resolvedArchivePath -C $resolvedWebsitePath
if ($LASTEXITCODE -ne 0) {
    throw "Could not extract the frozen v4 archive."
}

$actualFileCount = (Get-ChildItem -LiteralPath $v4Path -Recurse -File | Measure-Object).Count
if ($actualFileCount -ne $ExpectedFileCount) {
    throw "Frozen v4 file count mismatch. Expected $ExpectedFileCount but found $actualFileCount."
}

foreach ($requiredFile in @("index.html", "samples/index.html", "api/index.html")) {
    if (-not (Test-Path -LiteralPath (Join-Path $v4Path $requiredFile) -PathType Leaf)) {
        throw "Frozen v4 website is missing $requiredFile."
    }
}

Write-Output "Added the frozen v4 website ($actualFileCount files) to $v4Path"
