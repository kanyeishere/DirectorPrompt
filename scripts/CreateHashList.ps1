param(
    [Parameter(Mandatory=$true)]
    [string]$targetDir
)

$hashes = [ordered]@{}

try {
    $targetDir = $targetDir.Replace('\', '/').TrimEnd('/')

    Get-ChildItem -Path $targetDir -File -Recurse -Exclude *.zip,*.pdb,*.ipdb | ForEach-Object {
        $relativePath = $_.FullName.Replace('\', '/').Replace("$targetDir/", "")
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
        $hashes.Add($relativePath, $hash)
    }

    $outputPath = Join-Path $targetDir "hashes.json"
    $hashes | ConvertTo-Json | Out-File -FilePath $outputPath -Encoding utf8
    Write-Output "Hash list generated: $outputPath"
}
catch {
    Write-Error "Failed to generate hash list: $_"
    exit 1
}
