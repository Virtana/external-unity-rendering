param (
    [Parameter(Mandatory, ValueFromPipeline)]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Leaf)})]
    $ExecutablePath,

    [Switch]
    [bool] $BatchMode,
    
    [Parameter()]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container -IsValid)})]
    [string] $RenderPath,

    [Parameter()]
    [ValidateRange(300, [int]::MaxValue)]
    [int] $RenderHeight = 300,

    [Parameter()]
    [ValidateRange(300, [int]::MaxValue)]
    [int] $RenderWidth = 300,

    [Switch]
    [bool] $Transmit,

    [Switch]
    [bool] $LogJson,

    [Parameter()]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container -IsValid)})]
    [string] $JsonPath,

    [Parameter()]
    [ValidateRange(1, [int]::MaxValue)]
    [int] $ExportCount = -1,

    [Parameter()]
    [ValidatePattern('^[0-9]+(s|m|)$')]
    [string] $ExportDelay,

    [Parameter()]
    [ValidatePattern('^[0-9]+(s|m|)$')]
    [string] $TotalExportTime,

    [Parameter()]
    [ushort] $Port,

    [Parameter()]
    [string] $Interface
)

[System.Diagnostics.Process]$exporter = New-Object System.Diagnostics.Process
$exporter.StartInfo.FileName = Resolve-Path -Path $ExecutablePath | Select-Object -ExpandProperty Path
if ($BatchMode)
{
    $exporter.StartInfo.Arguments = "-batchmode -nographics" 
}
$exporter.StartInfo.Arguments += " -logFile `"$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./exporter.log"))`" export"

if ($RenderPath) {
    $exporter.StartInfo.Arguments += " --renderPath `"$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("$RenderPath"))`""
}

if ($JsonPath) {
    $exporter.StartInfo.Arguments += " --writeToFile `"$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($JsonPath))`""
}
if ($Transmit) {
    $exporter.StartInfo.Arguments += " --transmit"
}
if ($LogJson) {
    $exporter.StartInfo.Arguments += " --logExport"
}
if ($ExportDelay -ne -1) {
    $exporter.StartInfo.Arguments += " --delay $ExportDelay"
}
if ($ExportCount) {
    $exporter.StartInfo.Arguments += " --exportCount $ExportCount"
}
if ($TotalExportTime) {
    $exporter.StartInfo.Arguments += " --totalTime $TotalExportTime"
}
$exporter.StartInfo.Arguments += " -h $RenderHeight"
$exporter.StartInfo.Arguments += " -w $RenderWidth"

if ($Port)
{
    $exporter.StartInfo.Arguments += " --port ${Port}"
}

if ($Interface)
{
    $exporter.StartInfo.Arguments += " --interface `"${Interface}`""
}

Write-Verbose "Launching ${ExecutablePath} as exporter with the arguments: $($exporter.StartInfo.Arguments)"

try {
    if (!$exporter.Start()) {
        Write-Error "Failed to start exporter instance." -ErrorAction Stop
    }
} catch {
    Write-Error "Failed to find exporter executable." -ErrorAction Stop
}

Write-Host "Starting exporter instance..."
if (!($PSBoundParameters['Verbose']) -or ($VerbosePreference -eq 'Continue')) {
    Write-Verbose "VERBOSE is set. Exporter log will be written to the console."
    $exporterReader = Start-Job { 
        Get-Content ".\exporter.log" -Wait 
    }
}

[System.Console]::TreatControlCAsInput = $true

while (!$exporter.WaitForExit(500)) {
    if ([System.Console]::KeyAvailable)
    {
        $key = [System.Console]::ReadKey($true)
        if (($key.modifiers -band [System.ConsoleModifiers]"control") -and ($key.key -eq "C"))
        {
            break;
        }
    }
    if ($exporterReader) 
    {
        $exporterReader `
        | Receive-Job `
        | ForEach-Object {
            $_ `
            | Join-String -OutputPrefix "EXPORTER: " `
            | Write-Verbose
        }
    }
}

if (!$exporter.HasExited) {
    Write-Warning "Detected force exit. Killing instances."
    $exporter.Kill()
    Exit 1
} elseif ($exporter.ExitCode -ne 0) {
    Write-Error "Exporter Failed to complete successfully."
    Exit $exporter.ExitCode
} else {
    if ($exporterReader) {
        Start-Sleep 0.5 # try to get remaining output
        
        Stop-Job $exporterReader

        $exporterReader `
        | Receive-Job `
        | ForEach-Object {
            $_ `
            | Join-String -OutputPrefix "EXPORTER: " `
            | Write-Verbose
        }
        
        Remove-Job $exporterReader
    }
    Write-Host "Exporter Instance has completed successfully."
}