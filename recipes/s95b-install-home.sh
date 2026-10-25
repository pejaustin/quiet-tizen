#!/bin/sh
# Install the actual Quiet Home build after a fresh two-file backup and host copy.
# Invoke via stdin through the verified S95B temporary-root route.
set -eu
test "$(id -u)" = 0
grep -qx 'BUILD_ID=T-PTMAKUC-REL-202602250225' /etc/tizen-release
name=${1:?Snapshot name required}
case "$name" in ''|*[!A-Za-z0-9_-]*) exit 1;; esac
D=/opt/usr/share/quiet-tizen-backups/$name
A=/opt/usr/apps/com.samsung.tv.bixby-promotion
S=/home/owner/share/tmp/sdk_tools/quiet-tizen-s95b
test -s "$D/restore.sh"
test -s "$D/before.sha256"
test ! -e "$A/res/quiet-home"
test ! -e "$S/home-unpacked"
sha256sum -c "$D/before.sha256"
mkdir "$S/home-unpacked"
tar -xzf "$S/home-package.tar.gz" -C "$S/home-unpacked"
test -s "$S/home-unpacked/BixbyPromotion.dll"
test -s "$S/home-unpacked/BixbyPromotion.runtimeconfig.json"
test -s "$S/home-unpacked/quiet-home/apps.json"
app_launcher -u 5001 -k com.samsung.tv.bixby-promotion || true
cp "$S/home-unpacked/BixbyPromotion.dll" "$A/bin/"
cp "$S/home-unpacked/BixbyPromotion.runtimeconfig.json" "$A/bin/"
cp -R "$S/home-unpacked/quiet-home" "$A/res/"
chown 201:0 "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json"
chmod 755 "$A/bin/BixbyPromotion.dll"
chmod 644 "$A/bin/BixbyPromotion.runtimeconfig.json"
chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json"
find "$A/res/quiet-home" -type d -exec chmod 755 {} \;
find "$A/res/quiet-home" -type f -exec chmod 644 {} \;
find "$A/res/quiet-home" -exec chown 201:0 {} \;
find "$A/res/quiet-home" -exec chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO {} \;
sha256sum -c "$D/expected.sha256"
echo 'Quiet Home installed; Home/startup selectors unchanged.'
