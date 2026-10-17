#!/bin/sh
# Builds dist/omenhub-pro_<version>_all.deb with plain dpkg-deb (no debhelper needed).  Usage: build-deb.sh [version]
set -e
here=$(cd "$(dirname "$0")/.." && pwd)
version=${1:-0.1.0}
package=omenhub-pro
root=$(mktemp -d)
trap 'rm -rf "$root"' EXIT

install -d "$root/DEBIAN" "$root/usr/bin" "$root/usr/lib/python3/dist-packages" "$root/usr/lib/udev/rules.d" \
  "$root/usr/lib/systemd/user" "$root/usr/share/applications" "$root/usr/share/doc/$package"

cp -r "$here/omenhub" "$root/usr/lib/python3/dist-packages/"
find "$root" -name __pycache__ -prune -exec rm -rf {} +
printf '#!/usr/bin/python3\nimport sys\nfrom omenhub.cli import main\n\nsys.exit(main())\n' > "$root/usr/bin/omenhub-lighting"
cp "$here/packaging/70-omenhub.rules" "$root/usr/lib/udev/rules.d/70-omenhub.rules"
sed 's|%h/.local/bin|/usr/bin|' "$here/packaging/omenhub-lighting.service" > "$root/usr/lib/systemd/user/omenhub-lighting.service"
cp "$here/packaging/omenhub-pro.desktop" "$root/usr/share/applications/"
cp "$here/README.md" "$root/usr/share/doc/$package/README.md"
cp "$here/../presets/Multi.preset" "$root/usr/share/doc/$package/Multi.preset.example"
cp "$here/packaging/copyright" "$root/usr/share/doc/$package/copyright"
{ echo "$package ($version) unstable; urgency=medium"; echo; echo "  * Linux port: keyboard and light-bar lighting."; echo
  echo " -- Oleksandr Maliuk <xvergox@gmail.com>  $(date -R)"; } | gzip -9n > "$root/usr/share/doc/$package/changelog.Debian.gz"

install -m 755 "$here/packaging/postinst" "$here/packaging/postrm" "$root/DEBIAN/"
size=$(du -sk "$root" | cut -f1)
cat > "$root/DEBIAN/control" <<CONTROL
Package: $package
Version: $version
Section: utils
Priority: optional
Architecture: all
Installed-Size: $size
Depends: python3 (>= 3.8), python3-gi, gir1.2-gtk-3.0, udev
Maintainer: Oleksandr Maliuk <xvergox@gmail.com>
Homepage: https://github.com/AlexFourQRTS/OmenHubPro
Description: Keyboard and light-bar lighting for HP OMEN laptops
 Per-key RGB keyboard lighting and the bottom light bar of HP OMEN laptops
 (HP Gaming Keyboard II, USB 0d62:54bf) through the HID LampArray interface.
 Effects, presets, a GTK window, a command line tool and a background service.
CONTROL

find "$root" -type d -exec chmod 755 {} +
find "$root" -type f ! -path "$root/DEBIAN/*" -exec chmod 644 {} +
chmod 755 "$root/usr/bin/omenhub-lighting"
mkdir -p "$here/dist"
dpkg-deb --root-owner-group --build "$root" "$here/dist/${package}_${version}_all.deb"
