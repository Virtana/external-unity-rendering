param (
    [Parameter(Mandatory,
    HelpMessage="Enter the path to the unity project to build.")]
    [ValidateScript({Test-Path -LiteralPath $_ -PathType Container})]
    [string]$ProjectPath,
    
    [Parameter(Mandatory,
    HelpMessage="Enter the path to the temp path.")]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container)})]
    [string]$TempPath,
    
    [Parameter(Mandatory,
    HelpMessage="Enter the path to the folder where to build files to.")]
    [ValidateScript({Test-Path -LiteralPath $_ -PathType Container})]
    [string]$BuildPath,

    [Parameter(HelpMessage="Enter comma separated options for the build options (see ).")]
    [ValidateScript({
        $validOptions = @("none","development","autorunplayer","showbuiltplayer",
            "buildadditionalstreamedscenes","acceptexternalmodificationstoplayer",
            "connectwithprofiler","allowdebugging","symlinklibraries","uncompressedassetbundle",
            "connecttohost","enableheadlessmode","buildscriptsonly","patchpackage",
            "forceenableassertions","compresswithlz4","compresswithlz4hc","strictmode",
            "includetestassemblies","nouniqueidentifier","waitforplayerconnection","enablecodecoverage",
            "enabledeepprofilingsupport","detailedbuildreport","shaderlivelinksupport")

        $_.Split(",") | ForEach-Object {
            $option = $_.Trim().ToLower()
            if (!$validOptions.Contains($option)) {
                return $false;
            }
        }

        return $true;
    })]
    [string]$BuildOptions,

    [switch]
    [bool]$BuildWindows
)

$ProjectPath = Resolve-Path -Path $ProjectPath | Select-Object -ExpandProperty Path
$TempPath = Resolve-Path -Path $TempPath | Select-Object -ExpandProperty Path
$BuildPath = Resolve-Path -Path $BuildPath | Select-Object -ExpandProperty Path

Get-ChildItem -Path $TempPath | ForEach-Object {
    Remove-Item -Path $_ -Recurse -Force -Confirm:$false
}
Get-ChildItem -Path $BuildPath | ForEach-Object {
    Remove-Item -Path $_ -Recurse -Force -Confirm:$false
}

Copy-Item -Path ("{0}\*" -f $ProjectPath) -Destination $TempPath -Recurse

[System.Diagnostics.Process]$proc = New-Object System.Diagnostics.Process
$proc.StartInfo.FileName = "C:\Programs\Unity\Editor\Unity.exe"
$proc.StartInfo.Arguments = "-quit -batchmode -nographics -projectPath `"$TempPath`" -logFile `"./physics_build_log.txt`" -executeMethod BuildScript.Build
--config Physics --build `"$BuildPath`""
if ($BuildOptions)
{
    $proc.StartInfo.Arguments += " --options `"$BuildOptions`""
}
if ($BuildWindows) {
    $proc.StartInfo.Arguments += " --buildTarget `"StandaloneWindows64`""
}

if (!$proc.Start()) {
    Write-Error "Failed to start Unity." -ErrorAction Stop
}

Write-Host "Starting Physics Build..."
while (!$proc.HasExited) {
    Write-Host "Waiting for Unity to exit..."
    $proc.WaitForExit()
}

if ($proc.ExitCode -ne 0) {
    Write-Error "Failed to build physics. See build log for details."
    Exit $proc.ExitCode
}

$proc.StartInfo.FileName = "C:\Programs\Unity\Editor\Unity.exe"
$proc.StartInfo.Arguments = "-quit -batchmode -nographics -projectPath `"$TempPath`" -logFile `"./renderer_build_log.txt`" -executeMethod BuildScript.Build
--config Renderer --build `"$BuildPath`""
if ($BuildOptions)
{
    $proc.StartInfo.Arguments += " --options `"$BuildOptions`""
}
if ($BuildWindows) {
    $proc.StartInfo.Arguments += " --buildTarget `"StandaloneWindows64`""
}

if (!$proc.Start()) {
    Write-Error "Failed to start Unity." -ErrorAction Stop
}

Write-Host "Starting Renderer Build..."
while (!$proc.HasExited) {
    Write-Host "Waiting for Unity to exit..."
    $proc.WaitForExit()
}

if ($proc.ExitCode -ne 0) {
    Write-Error "Failed to build renderer. See build log for details."
    Exit $proc.ExitCode
}

return [PSCustomObject]@{
    PhysicsPath = Join-Path -Path $BuildPath -ChildPath 'Physics\Physics.exe'
    RendererPath = Join-Path -Path $BuildPath -ChildPath 'Renderer\Renderer.exe'
}