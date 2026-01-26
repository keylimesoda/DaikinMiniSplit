Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$DaikinIP = "192.168.183.26"
# ---------------------

# Initialize Form
$form = New-Object System.Windows.Forms.Form
$form.Text = "Daikin Manager (Stable)"
$form.Size = New-Object System.Drawing.Size(340, 620)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

# --- AUTO-REFRESH TIMER (30 seconds) ---
$refreshTimer = New-Object System.Windows.Forms.Timer
$refreshTimer.Interval = 30000

# Fonts
$fontBold = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontNorm = New-Object System.Drawing.Font("Segoe UI", 9)
$fontMono = New-Object System.Drawing.Font("Consolas", 9)
$fontSmall = New-Object System.Drawing.Font("Segoe UI", 8)

# --- TOOLTIPS ---
$toolTip = New-Object System.Windows.Forms.ToolTip
$toolTip.AutoPopDelay = 10000; $toolTip.InitialDelay = 500; $toolTip.ReshowDelay = 500; $toolTip.ShowAlways = $true
function Add-Tip($ctrl, $t, $d) { $toolTip.SetToolTip($ctrl, "API Param: $t`n$d") }

# --- TABS ---
$tabControl = New-Object System.Windows.Forms.TabControl; $tabControl.Dock = "Fill"
$tabCtrl = New-Object System.Windows.Forms.TabPage; $tabCtrl.Text = "  Controls  "; $tabCtrl.Padding = New-Object System.Windows.Forms.Padding(10)
$tabDiag = New-Object System.Windows.Forms.TabPage; $tabDiag.Text = "  Diagnostics  "; $tabDiag.Padding = New-Object System.Windows.Forms.Padding(10)

$tabControl.Controls.Add($tabCtrl)
$tabControl.Controls.Add($tabDiag)
$form.Controls.Add($tabControl)

# =========================
# TAB 1: CONTROLS
# =========================

$lblMainStatus = New-Object System.Windows.Forms.Label; $lblMainStatus.Location = New-Object System.Drawing.Point(15, 15); $lblMainStatus.Size = New-Object System.Drawing.Size(280, 20)
$lblMainStatus.Text = "Status: Connecting..."; $lblMainStatus.Font = $fontBold
$tabCtrl.Controls.Add($lblMainStatus)

$btnPower = New-Object System.Windows.Forms.Button; $btnPower.Location = New-Object System.Drawing.Point(15, 45); $btnPower.Size = New-Object System.Drawing.Size(280, 45)
$btnPower.Text = "POWER: OFF"; $btnPower.Font = $fontBold; $btnPower.BackColor = [System.Drawing.Color]::LightGray
Add-Tip $btnPower "pow" "0/1 Toggle"
$tabCtrl.Controls.Add($btnPower)

$grpSet = New-Object System.Windows.Forms.GroupBox; $grpSet.Location = New-Object System.Drawing.Point(15, 100); $grpSet.Size = New-Object System.Drawing.Size(280, 320); $grpSet.Text = "Thermostat"
$tabCtrl.Controls.Add($grpSet)

# Mode
$lblMode = New-Object System.Windows.Forms.Label; $lblMode.Location = New-Object System.Drawing.Point(15, 30); $lblMode.Text = "Mode:"; $lblMode.AutoSize = $true
Add-Tip $lblMode "mode" "GET/SET: /aircon/get_control_info, /aircon/set_control_info"
$grpSet.Controls.Add($lblMode)
$cmbMode = New-Object System.Windows.Forms.ComboBox; $cmbMode.Location = New-Object System.Drawing.Point(90, 27); $cmbMode.Size = New-Object System.Drawing.Size(170, 23); $cmbMode.DropDownStyle = "DropDownList"
[void]$cmbMode.Items.AddRange(@("Cool", "Heat", "Auto", "Dry", "Fan"))
Add-Tip $cmbMode "mode" "3=Cool, 4=Heat, 1=Auto, 0=Fan"
$grpSet.Controls.Add($cmbMode)

# Update dial color when mode changes
$cmbMode.Add_SelectedIndexChanged({ $dialPanel.Invalidate() })

# Temperature Dial
$Global:DialTemp = 72
$Global:DialMin = 60
$Global:DialMax = 90
$Global:DialDragging = $false

$dialPanel = New-Object System.Windows.Forms.Panel
$dialPanel.Location = New-Object System.Drawing.Point(65, 55)
$dialPanel.Size = New-Object System.Drawing.Size(150, 150)
$dialPanel.BackColor = [System.Drawing.Color]::Transparent
Add-Tip $dialPanel "stemp" "Drag to set temperature (GET/SET: /aircon/set_control_info)"
$grpSet.Controls.Add($dialPanel)

$lblDialTemp = New-Object System.Windows.Forms.Label
$lblDialTemp.Location = New-Object System.Drawing.Point(0, 60); $lblDialTemp.Size = New-Object System.Drawing.Size(150, 30)
$lblDialTemp.Text = "72°F"; $lblDialTemp.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$lblDialTemp.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
$dialPanel.Controls.Add($lblDialTemp)

$lblActualMini = New-Object System.Windows.Forms.Label
$lblActualMini.Location = New-Object System.Drawing.Point(0, 88); $lblActualMini.Size = New-Object System.Drawing.Size(150, 18)
$lblActualMini.Text = "(Actual: --°)"; $lblActualMini.ForeColor = [System.Drawing.Color]::DimGray
$lblActualMini.Font = $fontSmall; $lblActualMini.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
Add-Tip $lblActualMini "htemp" "GET: /aircon/get_sensor_info (Indoor Temp)"
$dialPanel.Controls.Add($lblActualMini)

$dialPanel.Add_Paint({
    param($sender, $e)
    $g = $e.Graphics
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    $cx = 75; $cy = 75; $r = 65
    
    # Draw outer arc (track)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::LightGray, 8)
    $g.DrawArc($pen, ($cx-$r), ($cy-$r), ($r*2), ($r*2), 135, 270)
    $pen.Dispose()
    
    # Draw colored arc (progress)
    $range = $Global:DialMax - $Global:DialMin
    $pct = ($Global:DialTemp - $Global:DialMin) / $range
    $sweepAngle = 270 * $pct
    $color = if ($cmbMode.Text -eq "Heat") { [System.Drawing.Color]::OrangeRed } elseif ($cmbMode.Text -eq "Cool") { [System.Drawing.Color]::DodgerBlue } else { [System.Drawing.Color]::MediumSeaGreen }
    $pen = New-Object System.Drawing.Pen($color, 8)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen, ($cx-$r), ($cy-$r), ($r*2), ($r*2), 135, $sweepAngle)
    $pen.Dispose()
    
    # Draw tick marks
    for ($i = 0; $i -le 6; $i++) {
        $angle = (135 + ($i * 45)) * [Math]::PI / 180
        $x1 = $cx + ($r + 5) * [Math]::Cos($angle); $y1 = $cy + ($r + 5) * [Math]::Sin($angle)
        $x2 = $cx + ($r + 12) * [Math]::Cos($angle); $y2 = $cy + ($r + 12) * [Math]::Sin($angle)
        $tickPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Gray, 2)
        $g.DrawLine($tickPen, $x1, $y1, $x2, $y2)
        $tickPen.Dispose()
    }
    
    # Draw knob at current position
    $knobAngle = (135 + (270 * $pct)) * [Math]::PI / 180
    $kx = $cx + $r * [Math]::Cos($knobAngle); $ky = $cy + $r * [Math]::Sin($knobAngle)
    $knobBrush = New-Object System.Drawing.SolidBrush($color)
    $g.FillEllipse($knobBrush, ($kx-10), ($ky-10), 20, 20)
    $knobBrush.Dispose()
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2)
    $g.DrawEllipse($borderPen, ($kx-10), ($ky-10), 20, 20)
    $borderPen.Dispose()
})

$dialPanel.Add_MouseDown({
    param($sender, $e)
    $Global:DialDragging = $true
    Update-DialFromMouse $e.X $e.Y
})

$dialPanel.Add_MouseMove({
    param($sender, $e)
    if ($Global:DialDragging) { Update-DialFromMouse $e.X $e.Y }
})

$dialPanel.Add_MouseUp({
    $Global:DialDragging = $false
})

function Update-DialFromMouse($x, $y) {
    $cx = 75; $cy = 75
    $angle = [Math]::Atan2(($y - $cy), ($x - $cx)) * 180 / [Math]::PI
    # Convert angle to 0-270 range starting from bottom-left (135°)
    $angle = $angle - 135
    if ($angle -lt 0) { $angle += 360 }
    if ($angle -gt 270) { return }  # Outside dial range
    
    $pct = $angle / 270
    $temp = [Math]::Round($Global:DialMin + ($pct * ($Global:DialMax - $Global:DialMin)))
    $temp = [Math]::Max($Global:DialMin, [Math]::Min($Global:DialMax, $temp))
    $Global:DialTemp = $temp
    $lblDialTemp.Text = "$temp°F"
    $dialPanel.Invalidate()
}

function Set-DialValue($temp) {
    $Global:DialTemp = [Math]::Max($Global:DialMin, [Math]::Min($Global:DialMax, $temp))
    $lblDialTemp.Text = "$($Global:DialTemp)°F"
    $dialPanel.Invalidate()
}

# Fan
$lblFan = New-Object System.Windows.Forms.Label; $lblFan.Location = New-Object System.Drawing.Point(15, 210); $lblFan.Text = "Fan:"; $lblFan.AutoSize = $true
Add-Tip $lblFan "f_rate" "GET/SET: /aircon/get_control_info, /aircon/set_control_info"
$grpSet.Controls.Add($lblFan)
$cmbFan = New-Object System.Windows.Forms.ComboBox; $cmbFan.Location = New-Object System.Drawing.Point(90, 207); $cmbFan.Size = New-Object System.Drawing.Size(170, 23); $cmbFan.DropDownStyle = "DropDownList"
[void]$cmbFan.Items.AddRange(@("Auto", "Quiet", "Level 3", "Level 4", "Level 5", "Level 6", "Level 7"))
Add-Tip $cmbFan "f_rate" "A=Auto, B=Quiet, 3-7=Levels"
$grpSet.Controls.Add($cmbFan)

# Swing
$lblSwing = New-Object System.Windows.Forms.Label; $lblSwing.Location = New-Object System.Drawing.Point(15, 245); $lblSwing.Text = "Swing:"; $lblSwing.AutoSize = $true
Add-Tip $lblSwing "f_dir" "GET/SET: /aircon/get_control_info, /aircon/set_control_info"
$grpSet.Controls.Add($lblSwing)
$cmbSwing = New-Object System.Windows.Forms.ComboBox; $cmbSwing.Location = New-Object System.Drawing.Point(90, 242); $cmbSwing.Size = New-Object System.Drawing.Size(170, 23); $cmbSwing.DropDownStyle = "DropDownList"
[void]$cmbSwing.Items.AddRange(@("Stopped", "Vertical", "Horizontal", "3D / Both"))
Add-Tip $cmbSwing "f_dir" "0=Stop, 1=Vert, 2=Horiz, 3=3D"
$grpSet.Controls.Add($cmbSwing)

$btnApply = New-Object System.Windows.Forms.Button; $btnApply.Location = New-Object System.Drawing.Point(15, 275); $btnApply.Size = New-Object System.Drawing.Size(245, 35)
$btnApply.Text = "APPLY SETTINGS"; $btnApply.Font = $fontBold; $btnApply.BackColor = [System.Drawing.Color]::LightBlue
$grpSet.Controls.Add($btnApply)

# =========================
# TAB 2: DIAGNOSTICS
# =========================

$grpSensors = New-Object System.Windows.Forms.GroupBox; $grpSensors.Location = New-Object System.Drawing.Point(15, 15); $grpSensors.Size = New-Object System.Drawing.Size(280, 120); $grpSensors.Text = "Outdoor & System"
$tabDiag.Controls.Add($grpSensors)

$lblOutVal = New-Object System.Windows.Forms.Label; $lblOutVal.Location = New-Object System.Drawing.Point(15, 25); $lblOutVal.Size = New-Object System.Drawing.Size(250, 20); $lblOutVal.Text = "Outside Temp: --"
$grpSensors.Controls.Add($lblOutVal)
Add-Tip $lblOutVal "otemp" "Outdoor Sensor"

$lblComp = New-Object System.Windows.Forms.Label; $lblComp.Location = New-Object System.Drawing.Point(15, 50); $lblComp.Size = New-Object System.Drawing.Size(250, 20); $lblComp.Text = "Compressor Load: --"
$grpSensors.Controls.Add($lblComp)
Add-Tip $lblComp "cmpfreq" "Inverter Frequency (Hz)"

$lblErr = New-Object System.Windows.Forms.Label; $lblErr.Location = New-Object System.Drawing.Point(15, 75); $lblErr.Size = New-Object System.Drawing.Size(250, 20); $lblErr.Text = "Error Code: --"
$grpSensors.Controls.Add($lblErr)
Add-Tip $lblErr "err" "System Error Code"

$grpInfo = New-Object System.Windows.Forms.GroupBox; $grpInfo.Location = New-Object System.Drawing.Point(15, 150); $grpInfo.Size = New-Object System.Drawing.Size(280, 100); $grpInfo.Text = "Hardware Info"
$tabDiag.Controls.Add($grpInfo)

$lblMac = New-Object System.Windows.Forms.Label; $lblMac.Location = New-Object System.Drawing.Point(15, 25); $lblMac.Size = New-Object System.Drawing.Size(250, 20); $lblMac.Text = "MAC: --"; $lblMac.Font = $fontMono
$grpInfo.Controls.Add($lblMac)
Add-Tip $lblMac "mac" "From /common/basic_info"

$lblFirm = New-Object System.Windows.Forms.Label; $lblFirm.Location = New-Object System.Drawing.Point(15, 50); $lblFirm.Size = New-Object System.Drawing.Size(250, 20); $lblFirm.Text = "Firmware: --"; $lblFirm.Font = $fontMono
$grpInfo.Controls.Add($lblFirm)
Add-Tip $lblFirm "ver" "From /common/basic_info"

$btnRefresh = New-Object System.Windows.Forms.Button; $btnRefresh.Location = New-Object System.Drawing.Point(15, 330); $btnRefresh.Size = New-Object System.Drawing.Size(280, 40)
$btnRefresh.Text = "REFRESH DATA"; $btnRefresh.Font = $fontBold
$tabDiag.Controls.Add($btnRefresh)

# --- HELPERS ---
function Convert-CtoF($c) { if ($c -eq "-" -or $c -eq $null -or $c -eq "") { return $null } return [Math]::Round(([double]$c * 1.8) + 32, 1) }
function Convert-FtoC($f) { $c = ($f - 32) / 1.8; return [Math]::Round($c * 2) / 2 }
function Format-TempDisplay($f, $c) { if ($f -eq $null) { return "--" } return "$f°F ($c°C)" }

# --- LOGIC ---

$Global:CurrentPower = "0"

function Refresh-All {
    $lblMainStatus.Text = "Status: Polling..."
    
    # 1. INFO (/common/basic_info) - Lightweight
    try {
        $r = Invoke-RestMethod -Uri "http://$DaikinIP/common/basic_info" -TimeoutSec 2
        $d = @{}; $r -split ',' | ForEach-Object { $k,$v = $_ -split '='; $d[$k] = $v }
        $lblMac.Text = "MAC: " + $d["mac"]; $lblFirm.Text = "Ver: " + $d["ver"].Replace("_",".")
    } catch { $lblMac.Text = "MAC: Offline" }

    # 2. CONTROLS (/aircon/get_control_info) - Critical
    try {
        $r = Invoke-RestMethod -Uri "http://$DaikinIP/aircon/get_control_info" -TimeoutSec 2
        $d = @{}; $r -split ',' | ForEach-Object { $k,$v = $_ -split '='; $d[$k] = $v }
        
        $Global:CurrentPower = $d["pow"]
        if ($d["pow"] -eq "1") { $btnPower.Text = "POWER: ON"; $btnPower.BackColor = [System.Drawing.Color]::LightGreen } else { $btnPower.Text = "POWER: OFF"; $btnPower.BackColor = [System.Drawing.Color]::LightCoral }
        
        $m=$d["mode"]; switch($m){3{$t="Cool"}4{$t="Heat"}1{$t="Auto"}2{$t="Dry"}0{$t="Fan"}Default{$t="Cool"}}; $cmbMode.SelectedItem = $t
        
        # Reverse hysteresis compensation for display
        $stempC = [double]$d["stemp"]
        if ($m -eq "4") { $stempC = $stempC - 1.0 }      # Heat: we sent +1, so subtract
        elseif ($m -eq "3") { $stempC = $stempC + 1.0 }  # Cool: we sent -1, so add
        Set-DialValue ([Math]::Round((Convert-CtoF $stempC)))
        
        $f=$d["f_rate"]; if($f -eq "A"){$cmbFan.SelectedItem="Auto"}elseif($f -eq "B"){$cmbFan.SelectedItem="Quiet"}else{$cmbFan.SelectedItem="Level $f"}
        $s=$d["f_dir"]; switch($s){"0"{$st="Stopped"}"1"{$st="Vertical"}"2"{$st="Horizontal"}"3"{$st="3D / Both"}Default{$st="Vertical"}}; $cmbSwing.SelectedItem=$st
        
        $lblMainStatus.Text = "Status: Connected"
    } catch { $lblMainStatus.Text = "Status: Error / Offline" }

    # 3. SENSORS (/aircon/get_sensor_info) - Lightweight
    try {
        $r = Invoke-RestMethod -Uri "http://$DaikinIP/aircon/get_sensor_info" -TimeoutSec 2
        $d = @{}; $r -split ',' | ForEach-Object { $k,$v = $_ -split '='; $d[$k] = $v }
        
        $inC = $d["htemp"]; $outC = $d["otemp"]
        $inF = Convert-CtoF $inC; $outF = Convert-CtoF $outC
        if ($inF -eq $null) { $lblActualMini.Text = "(Actual: --)" } else { $lblActualMini.Text = "(Actual: $inF°F)" }
        $lblOutVal.Text = "Outside: $(Format-TempDisplay $outF $outC)"
        
        $freq = $d["cmpfreq"]; if ($freq -eq "0") { $lblComp.Text="Compressor: Idle (0%)";$lblComp.ForeColor=[System.Drawing.Color]::Gray } else { $lblComp.Text="Compressor: ACTIVE ($freq Hz)";$lblComp.ForeColor=[System.Drawing.Color]::DarkGreen }
        if ($d["err"] -eq "0") { $lblErr.Text="System Health: OK";$lblErr.ForeColor=[System.Drawing.Color]::DarkGreen } else { $lblErr.Text="ERROR CODE: "+$d["err"];$lblErr.ForeColor=[System.Drawing.Color]::Red }
    } catch { $lblActualMini.Text = "(Actual: --)" }
}

function Set-Daikin {
    $p = $Global:CurrentPower; switch ($cmbMode.Text) { "Cool" {$m=3} "Heat" {$m=4} "Auto" {$m=1} "Dry" {$m=2} "Fan" {$m=0} }
    $t = Convert-FtoC $Global:DialTemp
    
    # Hysteresis compensation: Daikin stops ~1°C before reaching setpoint
    # Heat mode: stops when room is ~1°C BELOW setpoint → send 1°C higher
    # Cool mode: stops when room is ~1°C ABOVE setpoint → send 1°C lower
    if ($m -eq 4) { $t = $t + 1.0 }      # Heat: raise setpoint
    elseif ($m -eq 3) { $t = $t - 1.0 }  # Cool: lower setpoint
    
    if ($cmbFan.Text -eq "Auto") { $f="A" } elseif ($cmbFan.Text -eq "Quiet") { $f="B" } else { $f = $cmbFan.Text.Split(" ")[1] }
    switch ($cmbSwing.Text) { "Stopped" {$s=0} "Vertical" {$s=1} "Horizontal" {$s=2} "3D / Both" {$s=3} }
    
    $uri = "http://$DaikinIP/aircon/set_control_info?pow=$p&mode=$m&stemp=$t&f_rate=$f&shum=0&f_dir=$s"
    try { Invoke-RestMethod -Uri $uri -TimeoutSec 3 | Out-Null; Start-Sleep -Milliseconds 200; Refresh-All } catch { [System.Windows.Forms.MessageBox]::Show("Failed to send command.") }
}

# --- EVENTS ---
$form.Add_Load({ 
    Refresh-All
    $refreshTimer.Start()
})

$refreshTimer.Add_Tick({ Refresh-All })

$btnApply.Add_Click({ Set-Daikin })

$btnRefresh.Add_Click({ 
    $refreshTimer.Stop()
    Refresh-All
    $refreshTimer.Start()
})

$btnPower.Add_Click({ 
    if ($Global:CurrentPower -eq "1") { $Global:CurrentPower = "0" } else { $Global:CurrentPower = "1" }
    Set-Daikin 
})

$form.Add_FormClosed({ $refreshTimer.Dispose() })

[void]$form.ShowDialog()