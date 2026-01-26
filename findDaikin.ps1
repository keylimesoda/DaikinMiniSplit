$subnet = "192.168.183"

Write-Host "Scanning $subnet.1 through .254..." -ForegroundColor Cyan

1..254 | ForEach-Object {
    $ip = "$subnet.$_"
    
    # Fast Ping Test (1 count, quiet mode) to see if host is alive
    if (Test-Connection -ComputerName $ip -Count 1 -Quiet) {
        Write-Host -NoNewline "[$ip] Alive. Checking API... "
        
        try {
            # Low timeout to keep scan moving
            $response = Invoke-RestMethod -Uri "http://$ip/aircon/get_control_info" -TimeoutSec 2 -ErrorAction Stop
            
            # If we get here, it's a Daikin
            Write-Host "FOUND DAIKIN!" -ForegroundColor Green
            Write-Host $response
            
            # Optional: Stop after finding it
            # break 
        } catch {
            Write-Host "Not Daikin." -ForegroundColor DarkGray
        }
    }
}

Write-Host "Scan Complete."