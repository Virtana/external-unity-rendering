param (
    [Parameter(Mandatory, ValueFromPipeline)]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Leaf)})]
    $ExecutablePath,
    
    [Parameter()]
    [ValidateScript({(Test-Path -LiteralPath $_ -PathType Container -IsValid)})]
    [string] $RenderPath,

    [Parameter()]
    [ushort] $Port,

    [Parameter()]
    [string] $Interface
)

if ($RenderPath) {
    if (!(Test-Path -LiteralPath $RenderPath -PathType Container)) {
        New-Item -Path $RenderPath -ItemType Directory
    }
    $RenderPath = Resolve-Path -Path $RenderPath | Select-Object -ExpandProperty Path
}

[System.Diagnostics.Process]$renderer = New-Object System.Diagnostics.Process
$renderer.StartInfo.FileName = $ExecutablePath
$renderer.StartInfo.Arguments = "-batchmode -logFile `"$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./renderer.log"))`" renderer"

if ($Port)
{
    $renderer.StartInfo.Arguments += " --port ${Port}"
}

if ($Interface)
{
    $renderer.StartInfo.Arguments += " --interface `"${Interface}`""
}

if ($RenderPath) {
    $renderer.StartInfo.Arguments += " --renderPath `"$RenderPath`""
}

Write-Verbose "Launching ${ExecutablePath} as Renderer with the arguments: $($renderer.StartInfo.Arguments)"

if (!$renderer.Start()) {
    Write-Error "Failed to start Renderer isntance." -ErrorAction Stop
}
Write-Host "Starting Renderer instance..."
if (!($PSBoundParameters['Verbose']) -or ($VerbosePreference -eq 'Continue')) {
    Write-Verbose "VERBOSE is set. Renderer log will be written to the console."
    $rendererReader = Start-Job { 
        Get-Content ".\renderer.log" -Wait 
    }
}

while (!$renderer.WaitForExit(500)) {
    if ([System.Console]::KeyAvailable)
    {
        $key = [System.Console]::ReadKey($true)
        if (($key.modifiers -band [System.ConsoleModifiers]"control") -and ($key.key -eq "C"))
        {
            break;
        }
    }
    if ($rendererReader) 
    {
        $rendererReader `
        | Receive-Job `
        | ForEach-Object {
            $_ `
            | Join-String -OutputPrefix "RENDERER: " `
            | Write-Verbose
        }
    }
}
if (!$renderer.HasExited)
{
    Write-Warning "Detected force exit. Killing Renderer."
    $renderer.Kill()
}

if ($rendererReader) {
    Start-Sleep 0.5 # try to get remaining output
    
    Stop-Job $rendererReader

    $rendererReader `
    | Receive-Job `
    | ForEach-Object {
        $_ `
        | Join-String -OutputPrefix "RENDERER: " `
        | Write-Verbose
    }
    
    Remove-Job $rendererReader
}

Write-Host "Renderer has completed successfully."
