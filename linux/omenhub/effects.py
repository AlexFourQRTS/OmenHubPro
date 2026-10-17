"""Colour of one keyboard lamp for the chosen effect (port of LightingEffects.cs).

x01 / y01 = lamp position on the keyboard, 0..1 from left to right and from top to bottom;
seconds = animation clock. Colours are (r, g, b) tuples of 0..255.
"""
import math

RAINBOW_DEGREES_PER_SECOND = 40


def _byte(value):
    return max(0, min(255, int(value)))


def scale(color, factor):
    return (_byte(color[0] * factor), _byte(color[1] * factor), _byte(color[2] * factor))


def mix(first, second, fraction):
    fraction = max(0.0, min(1.0, fraction))
    return tuple(_byte(first[i] + (second[i] - first[i]) * fraction) for i in range(3))


def from_hue(hue_degrees, brightness):
    sector = (hue_degrees % 360) / 60.0
    index = int(sector)
    rising = sector - index
    falling = 1 - rising
    red, green, blue = [(1, rising, 0), (falling, 1, 0), (0, 1, rising),
                        (0, falling, 1), (rising, 0, 1), (1, 0, falling)][min(index, 5)]
    return scale((red * 255, green * 255, blue * 255), brightness)


def _hash(lamp):
    value = math.sin(lamp * 12.9898) * 43758.5453
    return value - math.floor(value)


def _fire(y01, lamp, time):
    heat = max(0.0, min(1.0, y01 * 0.9 + 0.35 * math.sin(time * 5 + _hash(lamp) * 12) * math.sin(time * 2.3 + lamp)))
    return (255, 60 + 195 * heat * heat, 120 * (heat - 0.8) * 5 if heat > 0.8 else 0)


def color_at(lighting, lamp, x01, y01, seconds):
    time = seconds * (0.25 + lighting.speed * 0.25)
    brightness = lighting.brightness / 100.0
    primary, secondary = lighting.primary, lighting.secondary
    effect = ["off", "static", "gradient", "breathing", "rainbow", "rainbow-cycle",
              "color-wave", "pulse", "twinkle", "scanner", "fire"][lighting.effect]
    level = 1.0
    color = primary
    if effect == "off":
        return (0, 0, 0)
    if effect == "gradient":
        color = mix(primary, secondary, x01)
    elif effect == "breathing":
        level = 0.5 + 0.5 * math.sin(2 * math.pi * time / 3)
    elif effect == "rainbow":
        return from_hue(x01 * 360 + time * RAINBOW_DEGREES_PER_SECOND, brightness)
    elif effect == "rainbow-cycle":
        return from_hue(time * RAINBOW_DEGREES_PER_SECOND, brightness)
    elif effect == "color-wave":
        color = mix(primary, secondary, 0.5 + 0.5 * math.sin(2 * math.pi * (x01 - time * 0.2)))
    elif effect == "pulse":
        color = primary if int(time * 2) % 2 == 0 else secondary
    elif effect == "twinkle":
        color = mix(secondary, primary, max(0.0, math.sin(time * 2 + _hash(lamp) * 6.2832)) ** 6)
    elif effect == "scanner":
        position = 0.5 + 0.5 * math.sin(time * 1.6)
        color = mix(secondary, primary, max(0.0, 1 - abs(x01 - position) * 5))
    elif effect == "fire":
        color = _fire(y01, lamp, time)
    return scale(color, level * brightness)
