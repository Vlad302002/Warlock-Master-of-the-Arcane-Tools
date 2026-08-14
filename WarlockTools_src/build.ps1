# Build WarlockTools_GUI.exe next to Squeezer / XRconvert / TXMLConvert
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
# WarlockTools_src → exe в папке modding.tools (рядом с GUI)
$tools = Resolve-Path (Join-Path $here "..")
$exeName = "WarlockTools_GUI.exe"

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "csc.exe not found (need .NET Framework 4.x)"
}

$fw = Split-Path $csc
$sources = @(
    (Join-Path $here "Program.cs"),
    (Join-Path $here "UiLang.cs"),
    (Join-Path $here "ToolRunner.cs"),
    (Join-Path $here "MainForm.cs"),
    (Join-Path $here "WarlockMd.cs")
)
$outExe = Join-Path $tools $exeName

Write-Host "Compiling with: $csc"
& $csc /nologo /utf8output /codepage:65001 /target:winexe /optimize+ /platform:anycpu `
    /out:"$outExe" `
    /reference:"$fw\System.dll" `
    /reference:"$fw\System.Core.dll" `
    /reference:"$fw\System.Drawing.dll" `
    /reference:"$fw\System.Windows.Forms.dll" `
    $sources

if ($LASTEXITCODE -ne 0) { throw "csc failed with exit $LASTEXITCODE" }

Write-Host "OK: $outExe"
Get-Item $outExe | Format-List FullName, Length, LastWriteTime
