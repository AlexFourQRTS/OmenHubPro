"""Lighting configuration, shared by GUI / CLI / daemon. Stored as JSON in ~/.config/omenhub/lighting.json.

Presets use the same text format as the Windows build (one .preset file each), so they can be copied between systems.
"""
import json
import os
from dataclasses import dataclass, field

EFFECTS = ["off", "static", "gradient", "breathing", "rainbow", "rainbow-cycle",
           "color-wave", "pulse", "twinkle", "scanner", "fire"]
EFFECT_TITLES = ["Выкл", "Цвет", "Градиент", "Дыхание", "Радуга", "Цикл цвета", "Волна", "Мигание", "Мерцание", "Сканер", "Огонь"]
BAR_ZONES = 4
MAX_SPEED = 10

CONFIG_DIR = os.path.join(os.environ.get("XDG_CONFIG_HOME", os.path.expanduser("~/.config")), "omenhub")
SETTINGS_PATH = os.path.join(CONFIG_DIR, "lighting.json")
PRESET_DIR = os.path.join(CONFIG_DIR, "presets")


def _clamp(value, low, high):
    return max(low, min(high, int(value)))


def _rgb(value, fallback):
    try:
        return [_clamp(channel, 0, 255) for channel in value][:3] if len(value) == 3 else list(fallback)
    except (TypeError, ValueError):
        return list(fallback)


@dataclass
class Lighting:
    effect: int = 1
    primary: list = field(default_factory=lambda: [255, 255, 255])
    secondary: list = field(default_factory=lambda: [0, 80, 255])
    brightness: int = 100
    speed: int = 5
    bar_zones: list = field(default_factory=lambda: [[155, 107, 255], [155, 107, 255], [255, 138, 31], [255, 138, 31]])
    bar_brightness: int = 100
    bar_enabled: bool = True

    def normalized(self):
        self.effect = _clamp(self.effect, 0, len(EFFECTS) - 1)
        self.primary = _rgb(self.primary, (255, 255, 255))
        self.secondary = _rgb(self.secondary, (0, 80, 255))
        self.brightness = _clamp(self.brightness, 0, 100)
        self.speed = _clamp(self.speed, 1, MAX_SPEED)
        zones = [_rgb(z, (255, 255, 255)) for z in (self.bar_zones or [])][:BAR_ZONES]
        self.bar_zones = zones + [[255, 255, 255]] * (BAR_ZONES - len(zones))
        self.bar_brightness = _clamp(self.bar_brightness, 0, 100)
        self.bar_enabled = bool(self.bar_enabled)
        return self

    # --- JSON file -------------------------------------------------------------------------------------------
    @classmethod
    def load(cls, path=SETTINGS_PATH):
        try:
            with open(path) as stream:
                data = json.load(stream)
            return cls(**{k: v for k, v in data.items() if k in cls.__dataclass_fields__}).normalized()
        except (OSError, ValueError, TypeError):
            return cls().normalized()

    def save(self, path=SETTINGS_PATH):
        os.makedirs(os.path.dirname(path), exist_ok=True)
        temporary = path + ".tmp"
        with open(temporary, "w") as stream:
            json.dump(self.__dict__, stream, indent=1)
        os.replace(temporary, path)   # atomic: the daemon never reads a half-written file

    # --- Windows-compatible preset text ------------------------------------------------------------------------
    def keyboard_text(self):
        return ",".join(map(str, self.primary + self.secondary + [self.brightness, self.effect, self.speed]))

    def apply_preset_text(self, text):
        for line in text.splitlines():
            key, separator, value = line.partition("=")
            if not separator:
                key, value = "Keyboard", line
            value = value.strip()
            try:
                if key == "Keyboard":
                    n = [int(part) for part in value.split(",")]
                    if len(n) == 9:
                        self.primary, self.secondary = n[0:3], n[3:6]
                        self.brightness, self.effect, self.speed = n[6], n[7], n[8]
                elif key == "BarZones":
                    self.bar_zones = [[int(c) for c in zone.split(",")] for zone in value.split(";")]
                elif key == "BarBright":
                    self.bar_brightness = int(value)
                elif key == "BarOn":
                    self.bar_enabled = int(value) == 1
            except ValueError:
                continue
        return self.normalized()

    def preset_text(self):
        zones = ";".join(",".join(map(str, z)) for z in self.bar_zones)
        return "Keyboard=%s\nBarZones=%s\nBarBright=%d\nBarOn=%d\n" % (
            self.keyboard_text(), zones, self.bar_brightness, 1 if self.bar_enabled else 0)


def list_presets():
    try:
        return sorted(n[:-7] for n in os.listdir(PRESET_DIR) if n.endswith(".preset"))
    except OSError:
        return []


def _preset_path(name):
    safe = "".join("_" if c in '/\\\0' else c for c in name).strip()
    return os.path.join(PRESET_DIR, safe + ".preset") if safe else None


def save_preset(name, lighting):
    path = _preset_path(name)
    if path:
        os.makedirs(PRESET_DIR, exist_ok=True)
        with open(path, "w") as stream:
            stream.write(lighting.preset_text())
    return path


def load_preset(name, lighting):
    path = _preset_path(name)
    with open(path) as stream:
        return lighting.apply_preset_text(stream.read())


def delete_preset(name):
    path = _preset_path(name)
    if path and os.path.exists(path):
        os.remove(path)
