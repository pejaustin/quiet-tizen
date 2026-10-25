#!/bin/sh
# Snapshot only: exact S95B stock package, consistent databases, and routing values.
set -eu
test "$(id -u)" = 0
grep -qx 'BUILD_ID=T-PTMAKUC-REL-202602250225' /etc/tizen-release
name=${1:?Snapshot name required}
case "$name" in ''|*[!A-Za-z0-9_-]*) exit 1;; esac
A=/opt/usr/apps/com.samsung.tv.bixby-promotion
D=/opt/usr/share/quiet-tizen-backups/$name
test ! -e "$D"
test "$(sha256sum "$A/bin/BixbyPromotion.dll" | cut -d ' ' -f 1)" = ea5b72ded18ee30c8f96bc590aad6832de9a5549b2da7c2c080469a7cceb7fc8
test "$(stat -c %u:%g "$A/bin/BixbyPromotion.dll")" = 201:0
umask 077
mkdir -p "$D"
cp -a "$A" "$D/bixby-promotion-original"
sqlite3 /opt/dbspace/.pkgmgr_parser.db ".backup '$D/.pkgmgr_parser.db'"
sqlite3 /opt/dbspace/.app-context.db ".backup '$D/.app-context.db'"
vconftool get db/appfw/home_appid > "$D/preferences-before.txt"
vconftool get db/appfw/startup_appid >> "$D/preferences-before.txt"
vconftool get db/first_screen/type >> "$D/preferences-before.txt"
sqlite3 -readonly -header -csv "$D/.pkgmgr_parser.db" "SELECT app_id,app_disable,app_onboot,app_autorestart FROM package_app_info WHERE app_id='com.samsung.tv.bixby-promotion';" > "$D/flags-before.csv"
chsmack "$A/bin/BixbyPromotion.dll" > "$D/label-before.txt"
sha256sum "$A/bin/BixbyPromotion.dll" "$A/bin/BixbyPromotion.runtimeconfig.json" > "$D/stock.sha256"
tar -czf "$D/recovery.tar.gz" -C "$D" bixby-promotion-original .pkgmgr_parser.db .app-context.db preferences-before.txt flags-before.csv label-before.txt stock.sha256
sha256sum "$D/recovery.tar.gz"
stat -c %s "$D/recovery.tar.gz"
echo "Snapshot complete: $D"
