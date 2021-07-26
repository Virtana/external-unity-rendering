param (
    [Parameter(Mandatory,
    HelpMessage="Enter the path to the unity project to build.")]
    [ValidateScript({Test-Path -LiteralPath $_ -PathType Container})]
    [string]$ProjectPath,
    
    [Parameter(Mandatory,
    HelpMessage="Enter the path to the folder where to build files to.")]
    [ValidateScript({Test-Path -LiteralPath $_ -PathType Container -IsValid})]
    [string]$BuildPath,

    [switch]
    [bool]$BuildLinux
)

$ProjectPath = Resolve-Path -Path $ProjectPath | Select-Object -ExpandProperty Path

if (!(Test-Path -LiteralPath $BuildPath)){
    New-Item -Path $BuildPath -ItemType Directory -ErrorAction Stop | Out-Null
    Write-Verbose "Created Directory : `"${BuildPath}`""
}

$BuildPath = Resolve-Path -Path $BuildPath `
    | Select-Object -ExpandProperty Path

Write-Verbose "Resolved the output build path to `"${BuildPath}`""

$Executable = New-Object System.IO.DirectoryInfo ".\External Unity Rendering\" `
    | Select-Object -ExpandProperty Name
if (!($BuildLinux))
{
    $Executable += ".exe"
}

Write-Verbose "Resolved executable name to ${Executable}"

if ($ProjectPath | Join-Path -ChildPath "Temp/UnityLockfile" | Test-Path) {
    $title    = 'UnityLockfile detected. Another editor may have this project open. Do you want to continue?'
    $question = 'Are you sure you want to proceed?'
    $choices  = '&Yes', '&No'

    $decision = $Host.UI.PromptForChoice($title, $question, $choices, 1)
    if ($decision -eq 0) {
        Write-Host 'Using a temporary folder to open project.'
    } else {
        Write-Host 'Exiting Script.'
        Exit 0
    }

    $TempPath = Join-Path $Env:Temp ("build-$(New-Guid)")
    New-Item -Type Directory -Path $TempPath | Out-Null
    Write-Verbose "Created ${TempPath}"

    foreach ($ProjectFolder in @("Assets", "Packages", "ProjectSettings")) {
        $ProjectPath `
        | Join-Path -ChildPath $ProjectFolder `
        | Copy-Item -Destination $TempPath -Recurse
        Write-Verbose "Copied $($ProjectPath | Join-Path -ChildPath $ProjectFolder) to $($TempPath | Join-Path -ChildPath $ProjectFolder)" 
    }

    $ProjectPath = $TempPath
}

[System.Diagnostics.Process]$proc = New-Object System.Diagnostics.Process
$proc.StartInfo.FileName = "C:\Programs\Unity\Editor\Unity.exe"
$proc.StartInfo.Arguments = "-quit -batchmode -nographics -projectPath `"${ProjectPath}`""

# Imitate how Unity saves logs normally
if (Test-Path -Path "./build-prev.log")
{
    Remove-Item -Path "./build-prev.log"
}
if (Test-Path -Path "./build.log")
{
    Rename-Item -Path "./build.log" -NewName "build-prev.log"
}

$proc.StartInfo.Arguments += " -logFile `"build.log`""

if ($BuildLinux) {
    $proc.StartInfo.Arguments += " -buildLinux64Player `"$(Join-Path $BuildPath $Executable)`""
}
else 
{
    $proc.StartInfo.Arguments += " -buildWindows64Player `"$(Join-Path $BuildPath $Executable)`""
}

Write-Verbose "Launching Unity with the following arguments: $($proc.StartInfo.Arguments)"

if (!$proc.Start()) {
    Write-Error "Failed to start Unity." -ErrorAction Stop
}

Write-Host "Starting Build..."
if (!($PSBoundParameters['Verbose']) -or ($VerbosePreference -eq 'Continue')) {
    Write-Verbose "VERBOSE is set. Unity build log will be written to the console."
    $fileReader = Start-Job { 
        Get-Content ".\build.log" -Wait 
    }
}

try {
    [System.Console]::TreatControlCAsInput = $true
    while (!$proc.WaitForExit(500)) {
        if ([System.Console]::KeyAvailable)
        {
            $key = [System.Console]::ReadKey($true)
            if (($key.modifiers -band [System.ConsoleModifiers]"control") -and ($key.key -eq "C"))
            {
                break;
            }
        }
        if ($fileReader) 
        {
            $fileReader | Receive-Job | Write-Verbose
        }
    }
}
finally
{
    if ($TempPath)
    {
        Write-Verbose "Clearing ${TempPath}"
        Remove-Item -Path $TempPath -Recurse -Force
    }

    if (!$proc.HasExited) {
        Write-Warning "Detected force exit. Killing Build process."
        $proc.Kill()
        Exit 1
    }
    
    if ($fileReader) {
        Start-Sleep 0.5 # try to get remaining output
        Stop-Job $fileReader
        $fileReader | Receive-Job | Write-Verbose
        Remove-Job $fileReader
    }
}

if ($proc.ExitCode -ne 0) {
    Write-Error "Failed to build successfully. See build log for details."
    Exit $proc.ExitCode
}

if (!($PSBoundParameters['Verbose']) -or ($VerbosePreference -eq 'Continue'))
{
    Write-Host "Unity has exited successfully. See build.log for more details."
}

return "$(Join-Path $BuildPath $Executable)"