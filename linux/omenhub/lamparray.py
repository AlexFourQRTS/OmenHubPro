"""Keyboard HID LampArray (usage page 0x59) driven with raw feature reports.

Report ids: 1 = array attributes, 2/3 = lamp attributes request/response, 4 = multi-lamp update,
5 = range update, 6 = autonomous-mode control. All feature reports are 51 bytes including the id.
Lamps 0..102 are the keyboard, lamps 103..119 are the bottom light bar.
"""
import struct

from . import hid

KEYBOARD_VENDOR_ID = 0x0D62
LIGHTING_USAGE_PAGE = 0x59
REPORT_LENGTH = 51
LAMPS_PER_MULTI_UPDATE = 8
KEYBOARD_LAST_LAMP = 102

_ATTRIBUTES, _LAMP_REQUEST, _LAMP_RESPONSE, _MULTI_UPDATE, _RANGE_UPDATE, _CONTROL = 1, 2, 3, 4, 5, 6
_UPDATE_COMPLETE = 1


class PermissionDenied(Exception):
    pass


class LampArray:
    def __init__(self, device, lamp_count):
        self._device = device
        self.lamp_count = lamp_count

    @classmethod
    def open(cls):
        """Returns the keyboard's LampArray, None when absent; raises PermissionDenied when hidraw is not accessible."""
        denied = False
        for interface in hid.find_interfaces(KEYBOARD_VENDOR_ID):
            if interface.usage_page != LIGHTING_USAGE_PAGE:
                continue
            try:
                device = hid.HidDevice(interface.path)
            except PermissionError:
                denied = True
                continue
            except OSError:
                continue
            report = device.get_feature(_ATTRIBUTES, REPORT_LENGTH)
            if report and (report[1] | (report[2] << 8)) > 0:
                return cls(device, report[1] | (report[2] << 8))
            device.close()
        if denied:
            raise PermissionDenied("no access to the keyboard hidraw node (install the udev rule)")
        return None

    def lamp_positions(self):
        """[(x_mm, y_mm)] per lamp; None for a lamp the device did not answer for."""
        positions = []
        for lamp in range(self.lamp_count):
            request = bytearray(REPORT_LENGTH)
            request[0] = _LAMP_REQUEST
            struct.pack_into("<H", request, 1, lamp)
            response = self._device.get_feature(_LAMP_RESPONSE, REPORT_LENGTH) if self._device.set_feature(request) else None
            if response is None:
                positions.append(None)
                continue
            x, y, _z = struct.unpack_from("<iii", response, 3)
            positions.append((x / 1000.0, y / 1000.0))
        return positions

    def set_autonomous(self, autonomous):
        """False = the host controls every lamp; True = the keyboard runs its own effect again."""
        report = bytearray(REPORT_LENGTH)
        report[0] = _CONTROL
        report[1] = 1 if autonomous else 0
        return self._device.set_feature(report)

    def paint_all(self, red, green, blue):
        report = bytearray(REPORT_LENGTH)
        report[0] = _RANGE_UPDATE
        report[1] = _UPDATE_COMPLETE
        struct.pack_into("<HH", report, 2, 0, self.lamp_count - 1)
        report[6:10] = bytes((red, green, blue, 255))
        return self._device.set_feature(report)

    def paint_lamps(self, first, last, color_of):
        """Paints lamps first..last in groups of eight; color_of(lamp) -> (r, g, b)."""
        accepted = True
        last = min(last, self.lamp_count - 1)
        for start in range(first, last + 1, LAMPS_PER_MULTI_UPDATE):
            count = min(LAMPS_PER_MULTI_UPDATE, last - start + 1)
            report = bytearray(REPORT_LENGTH)
            report[0] = _MULTI_UPDATE
            report[1] = count
            report[2] = _UPDATE_COMPLETE if start + count > last else 0
            for slot in range(count):
                struct.pack_into("<H", report, 3 + slot * 2, start + slot)
                red, green, blue = color_of(start + slot)
                offset = 3 + LAMPS_PER_MULTI_UPDATE * 2 + slot * 4
                report[offset:offset + 4] = bytes((red, green, blue, 255))
            accepted &= self._device.set_feature(report)
        return accepted

    def close(self):
        self._device.close()
