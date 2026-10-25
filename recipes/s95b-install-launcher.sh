#!/bin/sh
# Install the S95B overlay into the inventoried container; do not change Home/startup.
set -eu
test "$(id -u)" = 0
grep -qx 'BUILD_ID=T-PTMAKUC-REL-202602250225' /etc/tizen-release
name=${1:?Snapshot name required}
case "$name" in ''|*[!A-Za-z0-9_-]*) exit 1;; esac
D=/opt/usr/share/quiet-tizen-backups/$name
A=/opt/usr/apps/com.samsung.tv.bixby-promotion
S=/home/owner/share/tmp/sdk_tools/quiet-tizen-s95b
test -s "$D/recovery.tar.gz"
test -s "$D/restore-launcher.sh"
test ! -e "$D/installed.sha256"
sha256sum -c "$D/stock.sha256"
test -s "$S/Overlay.dll"
test -s "$S/Overlay.runtimeconfig.json"
test "$(stat -c %u:%g "$A/bin/BixbyPromotion.dll")" = 201:0
test "$(sqlite3 -readonly /opt/dbspace/.pkgmgr_parser.db "SELECT count(*) FROM package_app_info WHERE app_id='com.samsung.tv.bixby-promotion' AND app_disable='false' AND app_onboot='false' AND app_autorestart='false';")" = 1
app_launcher -u 5001 -k com.samsung.tv.bixby-promotion || true
# Keep installed hashes before the first write, so partial installs can be restored.
sha256sum "$S/Overlay.dll" "$S/Overlay.runtimeconfig.json" > "$D/new-source.sha256"
test ! -e "$D/home-native-image"
if [ -d "$A/bin/.native_image" ]; then mv "$A/bin/.native_image" "$D/home-native-image"; fi
cp "$S/Overlay.dll" "$A/bin/BixbyPromotion.dll"
cp "$S/Overlay.runtimeconfig.json" "$A/bin/BixbyPromotion.runtimeconfig.json"
chown 201:0 "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json"
chmod 755 "$A/bin/BixbyPromotion.dll"
chmod 644 "$A/bin/BixbyPromotion.runtimeconfig.json"
chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json"
sha256sum "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json" > "$D/installed.sha256"
echo 'Overlay installed. Home/startup and registry flags unchanged. Launch separately for validation.'
