# HP BIOS WMI protocol (verified on OMEN MAX 16-ak0xxx, BIOS F.07)

Namespace `root\wmi`, class `hpqBIntM`, methods `hpqBIOSInt0/4/128/1024/4096` (suffix = output size).
Input object `hpqBDataIn`: `Sign = "SECU"`, `Command`, `CommandType`, `Size`, `hpqBData`.

- `Size` must be 0 or >= 4: pad payloads with zeros to 4 bytes. `hpqBIOSInt0` fails — use `Int4` for writes.
- Output: `rwReturnCode` (0 = ok, 5 = invalid command/params) and `Data`.

## Command 0x20008
| Type | Meaning | Notes |
|------|---------|-------|
| 0x10 | fan count | 2 |
| 0x2D | **real fan RPM / 100** | call with 128-byte output; `[cpu, gpu]` |
| 0x2C | fan type | 128-byte output |
| 0x26 / 0x27 | FanMax get / set | `27 01` = max (~4400 rpm); `27 00` = hand control back to BIOS auto |
| 0x2E | set manual fan level `[cpu, gpu]` in 100 rpm units | persists until FanMax toggled off |
| 0x1A | fan/perf mode `{FF, mode}` | modes 0x30/0x31 accepted; effect not fully mapped |
| 0x23 | temperature byte | |
| 0x21 | GPU power flags `[customTGP, PPAB, dstate]` | read only so far |
| 0x2F | fan table | |

## Command 0x20009 (keyboard)
Controls the bottom light bar (4 zones) - see the corrected findings below.

## Other findings
- `nvidia-smi -pl` is unsupported on this laptop (BIOS owns the GPU limit; NVML reports 5-140 W range).
- Fans read 0 rpm in BIOS auto mode when cool (zero-RPM idle) — normal.

## Keyboard lighting findings (OMEN MAX 16, HP Gaming Keyboard II, VID_0D62 PID_54BF) - CORRECTED
- The BIOS colour table (command 0x20009, type 0x02 get / 0x03 set, 128 bytes; bytes 25..36 = 4 zones x R,G,B) drives the
  **bottom light bar**, not the keyboard. VERIFIED visually (variants 1, 3, 5 of the lab only changed the bar).
- The same bar is also exposed to Windows as the virtual LampArray `HID_DEVICE_SYSTEM_VHF` (4 lamps, 32 cm).
- The **keyboard** is a per-key HID LampArray (usage page 0x59, interface MI_04): 120 lamps, 34.2 x 12.5 cm.
  Windows' own LampArray API hangs when opening it, but the device answers raw HID feature reports
  (report 1 = attributes, 4 = multi-lamp update, 5 = range update, 6 = autonomous-mode control; all 51-byte feature reports).
  See `src/Hardware/Lighting/DirectLampArray.cs`.

### Lamp index map of the HID LampArray (verified by the user with the range finder)
- Lamps **0..102** = keyboard (6x20 grid; some cells have no key and stay dark).
- Lamps **103..119** = bottom light bar (17 lamps). Keyboard and bar are independent: paint each range separately.
