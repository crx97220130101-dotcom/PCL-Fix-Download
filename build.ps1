$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.x C# compiler was not found.'
}
$dll = Join-Path $root 'lib\dnlib.dll'
$license = Join-Path $root 'lib\dnlib-LICENSE.txt'
$expectedHash = '97EF5328D1D1703924C48D9CD1D45B7227FE3836E116DCDED623B6A0DBD52C23'
if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'dnlib.dll does not match the bundled dependency hash.'
}
if (-not (Test-Path -LiteralPath $license)) { throw 'dnlib license file is missing.' }
$outDir = Join-Path $root 'bin'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$output = Join-Path $outDir 'PCL-Repair-Standalone.exe'
$sources = @('Standalone.cs', 'OneClick.cs', 'Patch.cs', 'Verify.cs') | ForEach-Object { Join-Path $root "src\$_" }
$arguments = @('/nologo', '/main:Standalone', '/target:exe', '/optimize+', '/r:System.Windows.Forms.dll', "/r:$dll", "/resource:$dll,dnlib.dll", "/resource:$license,dnlib.LICENSE.txt", "/out:$output") + $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE." }
Write-Host "Built: $output"
Write-Host 'The build does not run the patcher or any target executable.'
