"""GTK 3 control window: keyboard effect / colours / brightness / speed, light bar zones, presets."""
import gi

gi.require_version("Gtk", "3.0")
gi.require_version("Gdk", "3.0")
from gi.repository import Gdk, GLib, Gtk  # noqa: E402

from . import settings  # noqa: E402
from .engine import LightingEngine  # noqa: E402


def _to_rgba(color):
    return Gdk.RGBA(color[0] / 255.0, color[1] / 255.0, color[2] / 255.0, 1)


def _from_rgba(rgba):
    return [round(rgba.red * 255), round(rgba.green * 255), round(rgba.blue * 255)]


class Window(Gtk.Window):
    def __init__(self):
        super().__init__(title="OmenHub Pro — подсветка")
        self.set_border_width(14)
        self.set_default_size(560, 0)
        self.lighting = settings.Lighting.load()
        self._loading = False
        self._save_source = None
        self.engine = LightingEngine(lambda: self.lighting, log=lambda text: GLib.idle_add(self._show_status, text))

        grid = Gtk.Grid(column_spacing=12, row_spacing=10)
        self.add(grid)
        row = 0

        def label(text, bold=False):
            widget = Gtk.Label(label=("<b>%s</b>" % text) if bold else text, use_markup=bold, xalign=0)
            return widget

        grid.attach(label("Клавиатура", True), 0, row, 3, 1); row += 1
        self.effect = Gtk.ComboBoxText()
        for title in settings.EFFECT_TITLES:
            self.effect.append_text(title)
        grid.attach(label("Эффект"), 0, row, 1, 1); grid.attach(self.effect, 1, row, 2, 1); row += 1
        self.primary = Gtk.ColorButton(); self.secondary = Gtk.ColorButton()
        grid.attach(label("Основной цвет"), 0, row, 1, 1); grid.attach(self.primary, 1, row, 2, 1); row += 1
        grid.attach(label("Второй цвет"), 0, row, 1, 1); grid.attach(self.secondary, 1, row, 2, 1); row += 1
        self.brightness = self._scale(0, 100, 5); self.speed = self._scale(1, settings.MAX_SPEED, 1)
        grid.attach(label("Яркость"), 0, row, 1, 1); grid.attach(self.brightness, 1, row, 2, 1); row += 1
        grid.attach(label("Скорость"), 0, row, 1, 1); grid.attach(self.speed, 1, row, 2, 1); row += 1

        grid.attach(label("Нижняя полоса", True), 0, row, 3, 1); row += 1
        self.bar_on = Gtk.Switch(halign=Gtk.Align.START)
        grid.attach(label("Включена"), 0, row, 1, 1); grid.attach(self.bar_on, 1, row, 2, 1); row += 1
        zone_box = Gtk.Box(spacing=6)
        self.zones = [Gtk.ColorButton() for _ in range(settings.BAR_ZONES)]
        for button in self.zones:
            zone_box.pack_start(button, True, True, 0)
        grid.attach(label("Зоны (слева направо)"), 0, row, 1, 1); grid.attach(zone_box, 1, row, 2, 1); row += 1
        self.bar_brightness = self._scale(0, 100, 5)
        grid.attach(label("Яркость полосы"), 0, row, 1, 1); grid.attach(self.bar_brightness, 1, row, 2, 1); row += 1

        grid.attach(label("Пресеты", True), 0, row, 3, 1); row += 1
        self.presets = Gtk.ComboBoxText()
        grid.attach(self.presets, 0, row, 1, 1)
        buttons = Gtk.Box(spacing=6)
        for title, handler in (("Применить", self._apply_preset), ("Сохранить как…", self._save_preset), ("Удалить", self._delete_preset)):
            button = Gtk.Button(label=title); button.connect("clicked", handler); buttons.pack_start(button, True, True, 0)
        grid.attach(buttons, 1, row, 2, 1); row += 1

        self.status = Gtk.Label(label="", xalign=0)
        self.status.get_style_context().add_class("dim-label")
        grid.attach(self.status, 0, row, 3, 1)

        for widget, signal in ((self.effect, "changed"), (self.primary, "color-set"), (self.secondary, "color-set"),
                               (self.brightness, "value-changed"), (self.speed, "value-changed"),
                               (self.bar_brightness, "value-changed"), *[(z, "color-set") for z in self.zones]):
            widget.connect(signal, self._edited)
        self.bar_on.connect("notify::active", self._edited)
        self.connect("destroy", self._quit)
        self._load_widgets()
        self.engine.start()

    @staticmethod
    def _scale(low, high, step):
        scale = Gtk.Scale.new_with_range(Gtk.Orientation.HORIZONTAL, low, high, step)
        scale.set_digits(0); scale.set_hexpand(True)
        return scale

    def _show_status(self, text):
        self.status.set_text(text)
        return False

    def _load_widgets(self):
        self._loading = True
        light = self.lighting
        self.effect.set_active(light.effect)
        self.primary.set_rgba(_to_rgba(light.primary)); self.secondary.set_rgba(_to_rgba(light.secondary))
        self.brightness.set_value(light.brightness); self.speed.set_value(light.speed)
        self.bar_on.set_active(light.bar_enabled); self.bar_brightness.set_value(light.bar_brightness)
        for button, color in zip(self.zones, light.bar_zones):
            button.set_rgba(_to_rgba(color))
        self._refresh_presets()
        self._loading = False

    def _refresh_presets(self):
        self.presets.remove_all()
        for name in settings.list_presets():
            self.presets.append_text(name)

    def _edited(self, *_):
        if self._loading:
            return
        light = self.lighting
        # build a fresh object: the engine thread always sees a consistent snapshot
        updated = settings.Lighting(
            effect=self.effect.get_active(), primary=_from_rgba(self.primary.get_rgba()),
            secondary=_from_rgba(self.secondary.get_rgba()), brightness=int(self.brightness.get_value()),
            speed=int(self.speed.get_value()), bar_zones=[_from_rgba(b.get_rgba()) for b in self.zones],
            bar_brightness=int(self.bar_brightness.get_value()), bar_enabled=self.bar_on.get_active())
        if light.effect == 0 and updated.effect == 0 and updated.primary != light.primary:
            updated.effect = 1   # picking a colour on a switched-off keyboard turns it on
            self._loading = True; self.effect.set_active(1); self._loading = False
        self.lighting = updated.normalized()
        if self._save_source is None:   # debounce disk writes while a slider is dragged
            self._save_source = GLib.timeout_add(500, self._flush)

    def _flush(self):
        self._save_source = None
        self.lighting.save()
        return False

    def _apply_preset(self, *_):
        name = self.presets.get_active_text()
        if name:
            try:
                self.lighting = settings.load_preset(name, settings.Lighting.load())
            except OSError:
                return
            self.lighting.save()
            self._load_widgets()

    def _save_preset(self, *_):
        dialog = Gtk.MessageDialog(transient_for=self, modal=True, message_type=Gtk.MessageType.QUESTION,
                                   buttons=Gtk.ButtonsType.OK_CANCEL, text="Название пресета:")
        entry = Gtk.Entry(activates_default=True)
        dialog.get_content_area().pack_start(entry, False, False, 6)
        dialog.set_default_response(Gtk.ResponseType.OK)
        dialog.show_all()
        if dialog.run() == Gtk.ResponseType.OK and entry.get_text().strip():
            settings.save_preset(entry.get_text().strip(), self.lighting)
            self._refresh_presets()
        dialog.destroy()

    def _delete_preset(self, *_):
        name = self.presets.get_active_text()
        if name:
            settings.delete_preset(name)
            self._refresh_presets()

    def _quit(self, *_):
        if self._save_source is not None:
            GLib.source_remove(self._save_source)
            self.lighting.save()
        self.engine.stop()   # hands the keyboard back to its own effects
        Gtk.main_quit()


def run():
    Window().show_all()
    Gtk.main()
