#add kill on CTRL-C
param (
    [Parameter(Mandatory, ValueFromPipeline)]
    [ValidateScript({ 
        foreach ($property in @("PhysicsPath","RendererPath")) {
            if (!([bool](Get-Member -InputObject $_ -MemberType Properties -Name $property))) {
                return $false
            }
        }
        return $true
    })]
    $ExecutablePath,

    [Switch]
    [bool] $Transmit,

    [Switch]
    [bool] $LogJson,

    [Switch]
    [bool] $DontPrettyPrint,

    [Parameter()]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container -IsValid)})]
    [string] $JsonPath,
    
    [Parameter(Mandatory)]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container)})]
    [string] $RenderPath,

    [Parameter()]
    [ValidateRange(300, [int]::MaxValue)]
    [int] $RenderHeight = 300,

    [Parameter()]
    [ValidateRange(300, [int]::MaxValue)]
    [int] $RenderWidth = 300,

    [Parameter()]
    [ValidatePattern('^[0-9]+(s|m|)$')]
    [string] $ExportDelay,

    [Parameter()]
    [ValidateRange(1, [int]::MaxValue)]
    [int] $ExportCount = -1,

    [Parameter()]
    [ValidatePattern('^[0-9]+(s|m|)$')]
    [string] $TotalExportTime
)


# .\build.ps1 -ProjectPath "D:\Virtana\External Unity Rendering\External Unity Rendering" -TempPath .\temp\ -BuildPath .\build\
# .\run.ps1 -ExportDelay 100 -ExportCount 20 -RenderHeight 1080 -RenderWidth 1920 -RenderPath .\renders\ -Transmit 

$RenderPath = Resolve-Path -Path $RenderPath | Select-Object -ExpandProperty Path

if ($Transmit) {
    [System.Diagnostics.Process]$renderer = New-Object System.Diagnostics.Process
    $renderer.StartInfo.FileName = $ExecutablePath.RendererPath
    $renderer.StartInfo.Arguments = "-batchmode -profiler-enable -logFile .\renderer_log.txt"
    if (!$renderer.Start()) {
        Write-Error "Failed to start Renderer." -ErrorAction Stop
    }
    Write-Output "Starting Renderer instance..."
}

[System.Diagnostics.Process]$physics = New-Object System.Diagnostics.Process
$physics.StartInfo.FileName = $ExecutablePath.PhysicsPath
$physics.StartInfo.Arguments = "-batchmode -quit -logFile `"./physics_log.txt`" -r `"$RenderPath`""

if ($JsonPath) {
    $physics.StartInfo.Arguments += " --writeToFile `"{0}`"" -f ($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($JsonPath))
}
if ($Transmit) {
    $physics.StartInfo.Arguments += " --transmit"
}
if ($LogJson) {
    $physics.StartInfo.Arguments += " --logExport"
    if (!$DontPrettyPrint) {
        $physics.StartInfo.Arguments += " --prettyPrint"
    }
}
if ($ExportDelay -ne -1) {
    $physics.StartInfo.Arguments += " --delay $ExportDelay"
}
if ($ExportCount) {
    $physics.StartInfo.Arguments += " --exportCount $ExportCount"
}
if ($TotalExportTime) {
    $physics.StartInfo.Arguments += " --totalTime $TotalExportTime"
}
$physics.StartInfo.Arguments += " -h $RenderHeight"
$physics.StartInfo.Arguments += " -w $RenderWidth"

try {    
    if (!$physics.Start()) {
        Write-Error "Failed to start Physics." -ErrorAction Stop
    }
} catch {
    Write-Error "Failed to find Physics executable." -ErrorAction Stop
}

Write-Output "Starting Physics instance..."

while (!$physics.HasExited) {
    $physics.WaitForExit()
}

if ($physics.ExitCode -ne 0) {
    Write-Error "Physics Failed to complete successfully."
    if ($Transmit) {
        $renderer.Kill()
    }
    Exit $physics.ExitCode
}

if ($Transmit) {
    while (!$renderer.HasExited) {
        $renderer.WaitForExit()
    }
}