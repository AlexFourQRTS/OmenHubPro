# OmenHub Pro

Native control panel for HP OMEN laptops (developed on OMEN MAX 16-ak0xxx, Ryzen AI 7 350 + RTX 5070 Ti).
Single self-contained `.exe` (C#, WinForms, .NET Framework 4 that ships with Windows) — no installer, no runtime to download.

## Features
- Separate CPU / GPU fan control via the HP BIOS WMI interface (`root\wmi : hpqBIntM`): Auto, Max, Manual, Temperature curves
- Failsafe: >= 85 C raises the fan floor, >= 95 C forces max; fans return to BIOS control on exit
- Live monitoring: fan RPM, temperatures, GPU power/clock/load, CPU load/frequency
- GPU clock ceiling (nvidia-smi), CPU max frequency / boost (Windows power plan)
- CPU power limit 20-75 W in 5 W steps via RyzenAdj (optional, user-supplied)
- Dark (purple / orange / black) and light (Google-style) themes
- Planned: per-key keyboard lighting (HID), tray + autostart

## Build
```
build.bat
```
Output: `OmenHub.exe` (requests administrator rights — the BIOS WMI interface needs them).

## Linux
Keyboard / light-bar lighting only: see `linux/README.md` (branch `linux`).

## BIOS protocol notes
See `docs/BIOS-PROTOCOL.md`.

## Layout
- `src/` — application source
- `probe/` — small read-only probe tools used to reverse out the BIOS interface
- `docs/` — protocol notes
