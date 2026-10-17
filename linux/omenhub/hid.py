"""Minimal hidraw access: find a vendor's interfaces by sysfs, read/write feature reports with ioctl."""
import fcntl
import glob
import os
from dataclasses import dataclass

_IOC_READ, _IOC_WRITE = 2, 1


def _ioc(direction, number, size):
    return (direction << 30) | (size << 16) | (ord("H") << 8) | number


def _hidiocsfeature(length):
    return _ioc(_IOC_READ | _IOC_WRITE, 0x06, length)


def _hidiocgfeature(length):
    return _ioc(_IOC_READ | _IOC_WRITE, 0x07, length)


@dataclass
class HidInterface:
    path: str            # /dev/hidrawN
    vendor_id: int
    product_id: int
    usage_page: int
    descriptor: bytes


def _usage_page(descriptor):
    """Usage page of the first top-level item (0x05 = 1-byte, 0x06 = 2-byte Usage Page)."""
    if len(descriptor) >= 2 and descriptor[0] == 0x05:
        return descriptor[1]
    if len(descriptor) >= 3 and descriptor[0] == 0x06:
        return descriptor[1] | (descriptor[2] << 8)
    return 0


def find_interfaces(vendor_id):
    """Every hidraw node of the vendor, read from sysfs (no device access, so no permissions needed)."""
    found = []
    for node in sorted(glob.glob("/sys/class/hidraw/hidraw*")):
        try:
            with open(os.path.join(node, "device", "uevent")) as stream:
                uevent = stream.read()
            hid_id = next(l for l in uevent.splitlines() if l.startswith("HID_ID=")).split("=")[1]
            _bus, vendor, product = (int(part, 16) for part in hid_id.split(":"))
            if vendor != vendor_id:
                continue
            with open(os.path.join(node, "device", "report_descriptor"), "rb") as stream:
                descriptor = stream.read()
        except (OSError, StopIteration, ValueError):
            continue
        found.append(HidInterface("/dev/" + os.path.basename(node), vendor, product, _usage_page(descriptor), descriptor))
    return found


class HidDevice:
    """An open hidraw node. Feature reports include the report id as the first byte."""

    def __init__(self, path):
        self.fd = os.open(path, os.O_RDWR)

    def set_feature(self, report):
        buffer = bytearray(report)
        try:
            fcntl.ioctl(self.fd, _hidiocsfeature(len(buffer)), buffer, True)
            return True
        except OSError:
            return False

    def get_feature(self, report_id, length):
        buffer = bytearray(length)
        buffer[0] = report_id
        try:
            fcntl.ioctl(self.fd, _hidiocgfeature(length), buffer, True)
            return bytes(buffer)
        except OSError:
            return None

    def close(self):
        if self.fd is not None:
            os.close(self.fd)
            self.fd = None
