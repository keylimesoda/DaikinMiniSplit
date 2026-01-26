# Project: Daikin BRP072A43 Local Control
**Status:** Stable (v6.1)  
**Target:** Local Network Control (No Cloud)  
**Language:** PowerShell (Windows Forms)

## 1. Hardware Profile
* **Device Type:** Daikin "High Wall" Mini Split Wi-Fi Adapter
* **Model:** BRP072A43 (North American Market)
* **Chipset:** Murata / AMPAK (Legacy 2.4GHz)
* **Network Protocol:** HTTP (Port 80) - Unauthenticated
* **Current IP:** `192.168.183.26`

## 2. API Reference (Local)
The adapter utilizes a legacy REST API. All requests are `GET` requests, even for setting state.

### ✅ Safe Endpoints (Used in App)
| Endpoint | Description | Key Parameters |
| :--- | :--- | :--- |
| `/aircon/get_control_info` | Reads current thermostat state. | `pow`, `mode`, `stemp`, `f_rate`, `f_dir` |
| `/aircon/get_sensor_info` | Reads live sensors. | `htemp` (Indoor), `otemp` (Outdoor), `cmpfreq` (Compressor Hz), `err` (Error Code) |
| `/common/basic_info` | Device hardware details. | `mac`, `ver` (Firmware) |
| `/aircon/set_control_info` | **WRITE** command. | Requires all params: `pow`, `mode`, `stemp`, `f_rate`, `shum`, `f_dir` |

### ⛔ Dangerous Endpoints (DO NOT USE)
| Endpoint | Issue |
| :--- | :--- |
| `/aircon/get_week_power` | **CRITICAL BUG:** Causes the BRP072A43 web server thread to crash/hang, requiring a breaker flip to reset. |
| `/aircon/get_year_power` | Same as above. The chip cannot handle the processing load. |

## 3. Data Logic & Mappings

### Temperature Conversion
The API is strictly **Celsius**. The App handles all F/C translation.
* **Read:** $F = (C \times 1.8) + 32$
* **Write:** $C = (F - 32) / 1.8$ (Rounded to nearest 0.5)

### Mode Mapping (`mode`)
| ID | Mode |
| :--- | :--- |
| `3` | Cool |
| `4` | Heat |
| `1` | Auto |
| `2` | Dry (Dehumidify) |
| `0` | Fan Only |

### Fan Speed (`f_rate`)
* `A`: Auto
* `B`: Quiet (Moon icon)
* `3` - `7`: Fixed speeds (Low to High)

### Swing (`f_dir`)
* `0`: Stopped
* `1`: Vertical
* `2`: Horizontal
* `3`: 3D / Both

## 4. Known Issues & Constraints
1.  **Single Threaded:** The web server is easily overwhelmed. Do not poll faster than every 5–10 seconds.
2.  **Compressor Guard:** If the unit is toggled OFF then ON rapidly, the compressor will wait 3 minutes before starting (safety delay).
3.  **Conflict Mode:** If using a Multi-Split outdoor unit, this indoor unit cannot be in HEAT if another room is in COOL. It will enter standby.

## 5. Current Source Code (v6.1 Stable)
**Features:** Auto-refresh (30s), Diagnostics (Compressor/Sensors), Tooltips, Safe-Mode (Energy APIs removed).

