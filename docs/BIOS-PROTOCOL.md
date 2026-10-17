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
Reads return inconsistent data on this model; per-key lighting is HID (Darfon VID_0D62 PID_54BF), not BIOS 4-zone. Unresolved.

## Other findings
- `nvidia-smi -pl` is unsupported on this laptop (BIOS owns the GPU limit; NVML reports 5-140 W range).
- Fans read 0 rpm in BIOS auto mode when cool (zero-RPM idle) — normal.

## Keyboard lighting findings (OMEN MAX 16, HP Gaming Keyboard II, VID_0D62 PID_54BF)
- BIOS colour table (command 0x20009, type 0x02 get / 0x03 set, 128 bytes): the keyboard is **4-zone RGB**.
  Byte 25..36 = 4 zones x (R,G,B). Bytes 0..1 look like brightness. Writing the full table (read-modify-write) repaints the keyboard. VERIFIED visually.
- Backlight switch: type 0x05, first payload byte 0xE4 = on, 0x64 = off.
- Windows Dynamic Lighting (HID LampArray, usage page 0x59) exposes two arrays:
  * `HID_DEVICE_SYSTEM_VHF` virtual device, 4 lamps in a row, 32 cm wide ("Scene") - opens and paints fine.
  * the keyboard's own LampArray (MI_04) cannot be opened (WinRT async never completes), with or without the OMEN background process.
