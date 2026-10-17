"""Keeps the lighting alive: the keyboard lamps are sent ~10 times a second (and every 2 s as keep-alive),
the bar lamps only when their colour changes. The keyboard is handed back to its own effects on stop."""
import os
import threading
import time

from . import effects, settings
from .lamparray import KEYBOARD_LAST_LAMP, LampArray, PermissionDenied

FRAME_SECONDS = 0.1
KEEP_ALIVE_SECONDS = 2.0
RETRY_SECONDS = 3.0
FALLBACK_COLUMNS, FALLBACK_COLUMN_MM, FALLBACK_ROW_MM = 20, 17.0, 18.0


class LightingEngine:
    def __init__(self, get_lighting, log=print):
        self._get_lighting = get_lighting
        self._log = log
        self._stop = threading.Event()
        self._thread = None
        self.status = "stopped"

    def start(self):
        self._stop.clear()
        self._thread = threading.Thread(target=self._run, name="lighting", daemon=True)
        self._thread.start()

    def stop(self):
        self._stop.set()
        if self._thread:
            self._thread.join(3)

    def _set_status(self, text):
        if text != self.status:
            self.status = text
            self._log("lighting: " + text)

    def _take_control(self):
        try:
            array = LampArray.open()
        except PermissionDenied as error:
            self._set_status(str(error))
            return None
        if array is None:
            self._set_status("keyboard LampArray not found")
            return None
        array.set_autonomous(False)
        self._set_status("host control on, lamps=%d" % array.lamp_count)
        return array

    @staticmethod
    def _normalized_positions(array):
        raw = array.lamp_positions()
        xs, ys = [], []
        for lamp in range(array.lamp_count):
            known = lamp < len(raw) and raw[lamp] is not None
            xs.append(raw[lamp][0] if known else (lamp % FALLBACK_COLUMNS) * FALLBACK_COLUMN_MM)
            ys.append(raw[lamp][1] if known else (lamp // FALLBACK_COLUMNS) * FALLBACK_ROW_MM)
        max_x, max_y = max(max(xs), 1.0), max(max(ys), 1.0)
        return [x / max_x for x in xs], [y / max_y for y in ys]

    @staticmethod
    def _bar_colors(lighting, lamp_count):
        """One colour per bar lamp: the four zones spread over lamps 103.. (left to right)."""
        factor = lighting.bar_brightness / 100.0 if lighting.bar_enabled else 0.0
        count = lamp_count - (KEYBOARD_LAST_LAMP + 1)
        return [effects.scale(lighting.bar_zones[min(settings.BAR_ZONES - 1, i * settings.BAR_ZONES // count)], factor)
                for i in range(count)]

    def _run(self):
        array = None
        started = time.monotonic()
        last_keys = last_bar = None
        last_send = 0.0
        while not self._stop.is_set():
            if array is None:
                array = self._take_control()
                if array is None:
                    self._stop.wait(RETRY_SECONDS)
                    continue
                x01, y01 = self._normalized_positions(array)
                last_keys = last_bar = None
            lighting = self._get_lighting()
            now = time.monotonic()
            frame = [effects.color_at(lighting, lamp, x01[lamp], y01[lamp], now - started)
                     for lamp in range(KEYBOARD_LAST_LAMP + 1)]
            bar = self._bar_colors(lighting, array.lamp_count)
            ok = True
            keep_alive_due = now - last_send > KEEP_ALIVE_SECONDS
            if frame != last_keys or keep_alive_due:
                ok &= array.paint_lamps(0, KEYBOARD_LAST_LAMP, frame.__getitem__)
                last_keys, last_send = frame, now
            if bar != last_bar or keep_alive_due:
                ok &= array.paint_lamps(KEYBOARD_LAST_LAMP + 1, array.lamp_count - 1,
                                        lambda lamp: bar[lamp - KEYBOARD_LAST_LAMP - 1])
                last_bar = bar
            if not ok:   # device went away (suspend / replug): reopen
                array.close()
                array = None
                self._set_status("write failed, reopening")
                continue
            self._stop.wait(FRAME_SECONDS)
        if array is not None:
            array.set_autonomous(True)
            array.close()
            self._set_status("keyboard handed back to its own effects")


def file_watcher(path=settings.SETTINGS_PATH):
    """Returns a callable that gives the current Lighting and reloads it whenever the file changes."""
    state = {"mtime": None, "lighting": settings.Lighting().normalized()}

    def current():
        try:
            mtime = os.stat(path).st_mtime_ns
        except OSError:
            mtime = None
        if mtime != state["mtime"]:
            state["mtime"] = mtime
            state["lighting"] = settings.Lighting.load(path)
        return state["lighting"]
    return current
