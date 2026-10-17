"""omenhub-lighting: command line + daemon for the keyboard / light-bar lighting."""
import argparse
import signal
import sys
import threading

from . import settings
from .engine import LightingEngine, file_watcher


def _color(text):
    text = text.lstrip("#")
    try:
        if len(text) == 6:
            return [int(text[i:i + 2], 16) for i in (0, 2, 4)]
        parts = [int(p) for p in text.split(",")]
        if len(parts) == 3 and all(0 <= p <= 255 for p in parts):
            return parts
    except ValueError:
        pass
    raise argparse.ArgumentTypeError("colour must be RRGGBB or r,g,b")


def _build_parser():
    parser = argparse.ArgumentParser(prog="omenhub-lighting", description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("daemon", help="drive the lighting from the settings file until stopped")
    commands.add_parser("gui", help="open the GTK control window")
    commands.add_parser("status", help="show the current settings and presets")

    keyboard = commands.add_parser("set", help="change keyboard lighting (saved; a running daemon/GUI picks it up)")
    keyboard.add_argument("--effect", choices=settings.EFFECTS)
    keyboard.add_argument("--color", type=_color, help="primary colour")
    keyboard.add_argument("--color2", type=_color, help="second colour")
    keyboard.add_argument("--brightness", type=int, help="0..100")
    keyboard.add_argument("--speed", type=int, help="1..10")

    bar = commands.add_parser("bar", help="change the bottom light bar")
    bar.add_argument("--zones", type=_color, nargs=settings.BAR_ZONES, metavar="COLOR", help="four colours, left to right")
    bar.add_argument("--all", type=_color, help="one colour for every zone")
    bar.add_argument("--brightness", type=int, help="0..100")
    bar.add_argument("--on", action="store_true")
    bar.add_argument("--off", action="store_true")

    preset = commands.add_parser("preset", help="presets (same files as the Windows build)")
    preset.add_argument("action", choices=["list", "apply", "save", "delete"])
    preset.add_argument("name", nargs="?")

    test = commands.add_parser("test", help="run the effect in this process for a few seconds, then exit")
    test.add_argument("--seconds", type=float, default=10)
    return parser


def _run_daemon(seconds=None):
    engine = LightingEngine(file_watcher(), log=lambda text: print(text, flush=True))
    done = threading.Event()
    for signal_number in (signal.SIGINT, signal.SIGTERM):
        signal.signal(signal_number, lambda *_: done.set())
    engine.start()
    done.wait(seconds)
    engine.stop()


def main(argv=None):
    arguments = _build_parser().parse_args(argv)
    command = arguments.command
    if command == "daemon":
        _run_daemon()
    elif command == "test":
        _run_daemon(arguments.seconds)
    elif command == "gui":
        from .gui import run
        run()
    else:
        lighting = settings.Lighting.load()
        if command == "status":
            print("effect=%s color=%s color2=%s brightness=%d speed=%d" % (
                settings.EFFECTS[lighting.effect], lighting.primary, lighting.secondary, lighting.brightness, lighting.speed))
            print("bar: on=%s brightness=%d zones=%s" % (lighting.bar_enabled, lighting.bar_brightness, lighting.bar_zones))
            print("presets:", ", ".join(settings.list_presets()) or "-")
            return 0
        if command == "set":
            if arguments.effect is not None:
                lighting.effect = settings.EFFECTS.index(arguments.effect)
            elif arguments.color and lighting.effect == 0:
                lighting.effect = 1   # picking a colour on a switched-off keyboard turns it on (as in the Windows UI)
            if arguments.color: lighting.primary = arguments.color
            if arguments.color2: lighting.secondary = arguments.color2
            if arguments.brightness is not None: lighting.brightness = arguments.brightness
            if arguments.speed is not None: lighting.speed = arguments.speed
        elif command == "bar":
            if arguments.zones: lighting.bar_zones = arguments.zones
            if arguments.all: lighting.bar_zones = [arguments.all] * settings.BAR_ZONES
            if arguments.brightness is not None: lighting.bar_brightness = arguments.brightness
            if arguments.on: lighting.bar_enabled = True
            if arguments.off: lighting.bar_enabled = False
        elif command == "preset":
            if arguments.action == "list":
                print("\n".join(settings.list_presets()))
                return 0
            if not arguments.name:
                print("preset name required", file=sys.stderr)
                return 2
            if arguments.action == "save":
                settings.save_preset(arguments.name, lighting)
                return 0
            if arguments.action == "delete":
                settings.delete_preset(arguments.name)
                return 0
            try:
                settings.load_preset(arguments.name, lighting)
            except OSError:
                print("no such preset", file=sys.stderr)
                return 1
        lighting.normalized().save()
    return 0


if __name__ == "__main__":
    sys.exit(main())
