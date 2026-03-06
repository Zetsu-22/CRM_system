$killed = @()

# Kill by port 5035
$portProc = Get-NetTCPConnection -LocalPort 5035 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
foreach ($procId in $portProc) {
    if ($procId -and $procId -notin $killed) {
        Write-Host "Killing process on port 5035, PID $procId..."
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        $killed += $procId
    }
}

# Kill by command line
$processes = Get-CimInstance Win32_Process -Filter "name='dotnet.exe'" -ErrorAction SilentlyContinue
foreach ($p in $processes) {
    if ($p.CommandLine -like '*CrmSystem.Api*' -and $p.ProcessId -notin $killed) {
        Write-Host "Killing old backend PID $($p.ProcessId)..."
        Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
        $killed += $p.ProcessId
    }
}

if ($killed.Count -gt 0) {
    Start-Sleep -Seconds 1
}
Write-Host "Done."
