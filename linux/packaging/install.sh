#!/bin/sh
# Per-user install: launcher in ~/.local/bin and the systemd user service. The udev rule needs root (printed at the end).
set -e
here=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$HOME/.local/bin" "$HOME/.config/systemd/user"
ln -sf "$here/omenhub-lighting" "$HOME/.local/bin/omenhub-lighting"
cp "$here/packaging/omenhub-lighting.service" "$HOME/.config/systemd/user/"
systemctl --user daemon-reload
echo "Now run once (needs root):"
echo "  sudo cp $here/packaging/70-omenhub.rules /etc/udev/rules.d/ && sudo udevadm control --reload && sudo udevadm trigger --subsystem-match=hidraw"
echo "Then: systemctl --user enable --now omenhub-lighting"
