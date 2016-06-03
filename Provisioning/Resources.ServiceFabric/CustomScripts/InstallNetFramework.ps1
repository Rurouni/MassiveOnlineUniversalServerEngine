#Requires -Version 3.0

Set-StrictMode -Version Latest

$logFile = Join-Path $env:TEMP -ChildPath "InstallNetFx461ScriptLog.txt"

# Check if NetFx461 or later version exists
$netFxKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\" -ErrorAction Ignore

if($netFxKey -and $netFxKey.Release -ge 394254) {
    "$(Get-Date): The machine already has NetFx 4.6.1 or later version installed." | Out-File -FilePath $logFile -Append
    return
}

# Download NetFx461
$setupFileSourceUri = "https://download.microsoft.com/download/3/5/9/35980F81-60F4-4DE3-88FC-8F962B97253B/NDP461-KB3102438-Web.exe"
$setupFileLocalPath = Join-Path $env:TEMP -ChildPath "NDP461-KB3102438-Web.exe"

"$(Get-Date): Start to download NetFx 4.6.1 to $setupFileLocalPath." | Out-File -FilePath $logFile -Append

if(Test-Path $setupFileLocalPath)
{
    Remove-Item -Path $setupFileLocalPath -Force
}

$webClient = New-Object System.Net.WebClient

try {
    $webClient.DownloadFile($setupFileSourceUri, $setupFileLocalPath)
} 
catch {
    "$(Get-Date): It looks the internet network is not available now. Simply wait for 30 seconds and try again." | Out-File -FilePath $logFile -Append
    Start-Sleep -Second 30
    $webClient.DownloadFile($setupFileSourceUri, $setupFileLocalPath)
}

if(!(Test-Path $setupFileLocalPath))
{
    "$(Get-Date): Failed to download NetFx 4.6.1 setup package." | Out-File -FilePath $logFile -Append
    return
}

# Install NetFx461
$setupLogFilePath = Join-Path $env:TEMP -ChildPath "NetFx461SetupLog.txt"
$arguments = "/q /serialdownload /log $setupLogFilePath"

"$(Get-Date): Start to install NetFx 4.6.1." | Out-File -FilePath $logFile -Append
$process = Start-Process -FilePath $setupFileLocalPath -ArgumentList $arguments -Wait -PassThru

"$(Get-Date): Install NetFx finished with exit code : $($process.ExitCode)." | Out-File -FilePath $logFile -Append