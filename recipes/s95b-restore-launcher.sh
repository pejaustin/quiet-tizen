#!/bin/sh
# Restore only our two replaced files and cached native-image directory.
# This recipe intentionally does not restore databases or change Home selectors.
set -eu
test "$(id -u)" = 0
grep -qx 'BUILD_ID=T-PTMAKUC-REL-202602250225' /etc/tizen-release
name=${1:?Snapshot name required}
case "$name" in ''|*[!A-Za-z0-9_-]*) exit 1;; esac
D=/opt/usr/share/quiet-tizen-backups/$name
A=/opt/usr/apps/com.samsung.tv.bixby-promotion
test -s "$D/new-source.sha256"
# Accept either our build or the original for each file (interrupted installation).
check_file() {
    target=$1
    staged=$2
    actual=$(sha256sum "$A/bin/$target" | cut -d ' ' -f 1)
    stock=$(sha256sum "$D/bixby-promotion-original/bin/$target" | cut -d ' ' -f 1)
    changed=$(awk -v file="$staged" '$2 ~ ("/" file "$") {print $1}' "$D/new-source.sha256")
    test "$actual" = "$stock" || test "$actual" = "$changed"
}
check_file BixbyPromotion.dll Overlay.dll
check_file BixbyPromotion.runtimeconfig.json Overlay.runtimeconfig.json
test ! -e "$D/overlay-native-image"
app_launcher -u 5001 -k com.samsung.tv.bixby-promotion || true
cp -a "$D/bixby-promotion-original/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.dll"
cp -a "$D/bixby-promotion-original/bin/BixbyPromotion.runtimeconfig.json" "$A/bin/BixbyPromotion.runtimeconfig.json"
chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json"
if [ -d "$D/home-native-image" ]; then
    if [ -d "$A/bin/.native_image" ]; then mv "$A/bin/.native_image" "$D/overlay-native-image"; fi
    mv "$D/home-native-image" "$A/bin/.native_image"
fi
sha256sum -c "$D/stock.sha256"
echo 'Original package files and native cache restored. Home/startup were not changed.'
