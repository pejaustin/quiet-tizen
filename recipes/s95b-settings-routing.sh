#!/bin/sh
# S95B has no QuickLaunch package. Route its existing settings key directly.
# apply|restore SNAPSHOT_NAME; keep the numeric XF86More rules intact.
set -eu
test "$(id -u)" = 0
grep -qx 'BUILD_ID=T-PTMAKUC-REL-202602250225' /etc/tizen-release
action=${1:?}; name=${2:?}
case "$name" in ''|*[!A-Za-z0-9_-]*) exit 1;; esac
D=/opt/usr/share/quiet-tizen-backups/$name
test -s "$D/.app-context.db"
case "$action" in
apply)
 sqlite3 -bail /opt/dbspace/.app-context.db <<SQL
ATTACH '$D/.app-context.db' AS before;
BEGIN IMMEDIATE;
CREATE TEMP TABLE guard(ok INTEGER CHECK(ok=1));
INSERT INTO guard SELECT count(*)=1 FROM key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu';
INSERT INTO guard SELECT count(*)=1 FROM before.key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu';
INSERT INTO guard SELECT count(*)=1 FROM key_filter l JOIN before.key_filter b ON l.appid=b.appid AND l.key=b.key WHERE l.appid='org.tizen.menu' AND l.key='XF86SysMenu' AND l.target_selector IS b.target_selector AND l.args IS b.args AND l.longpress IS b.longpress AND l.override IS b.override AND l.on_selector IS b.on_selector AND l.request IS b.request;
UPDATE key_filter SET target_selector='package:com.samsung.tv.bixby-promotion',args='quiet_page:settings __K_SPLASH_SCREEN:LA_UI_VR_TRANSPARENT' WHERE appid='org.tizen.menu' AND key='XF86SysMenu';
COMMIT;
SQL
 ;;
restore)
 sqlite3 -bail /opt/dbspace/.app-context.db <<SQL
ATTACH '$D/.app-context.db' AS before;
BEGIN IMMEDIATE;
CREATE TEMP TABLE guard(ok INTEGER CHECK(ok=1));
INSERT INTO guard SELECT count(*)=1 FROM key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu' AND target_selector='package:com.samsung.tv.bixby-promotion' AND args='quiet_page:settings __K_SPLASH_SCREEN:LA_UI_VR_TRANSPARENT';
INSERT INTO guard SELECT count(*)=1 FROM before.key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu';
UPDATE key_filter SET target_selector=(SELECT target_selector FROM before.key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu'),args=(SELECT args FROM before.key_filter WHERE appid='org.tizen.menu' AND key='XF86SysMenu') WHERE appid='org.tizen.menu' AND key='XF86SysMenu';
COMMIT;
SQL
 ;;
*) exit 1;;
esac
vconftool set -f -t int memory/appfw/app_context_db_change 1
