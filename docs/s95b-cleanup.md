# S95B cleanup: observed results and recovery

**Current status:** the subsequent actual Quiet Home port passed the integration
tests. All six groups (45 records) are now applied, including sidebar/browser.
Home, startup, HDMI and standby/wake passed owner tests. See [current setup
and recovery](s95b-home.md). The rollback narrative below records the earlier
custom-overlay phase, not the final state.

Austin, 2026-10-25: requested full Quiet Tizen cleanup, including unwanted
advertising/viewing analysis, voice, context, smart extras and TV Plus.
The separate `profiles/qn55s95bafxza-1720.7.json` records the exact installed
targets for QN55S95BAFXZA, About firmware T-PTMAKUC-1720.7, live build
T-PTMAKUC-REL-202602250225. It is not an interchangeable U8000F profile.

## Applied and verified after full restart

| Group | App records disabled |
|---|---:|
| Advertising / viewing analysis | 8 |
| Voice | 17 |
| Context / content grab | 7 |
| Smart extras | 8 |
| TV Plus | 3 |
| **Total** | **43** |

Each target has `app_disable=true`, `app_onboot=false`, and
`app_autorestart=false`. Each group has a consistent SQLite snapshot on the
TV and a hash-verified host copy, plus field-specific guarded inverse SQL.
The apply/restore plans were exercised against a private copy of the actual
snapshot; round trips preserved all original app rows exactly.

Nine stop markers match exact negated `ConditionPathExists` declarations in
the TV's installed units. After restart, all nine units were inactive with
`ConditionResult=no`: pisa, canalysis, on-device voice, voice app, two context
services, gaming-hub daemon, health service, and slive provider. Adagent shares
the pisa condition. App-manager kill requests after disabling app records
reported them unavailable; those messages were not treated as proof that
already-running processes had exited.

Voice recognition and assistant prelaunch are off. Live and cached
`preload_by_country` are the string `false`; all were read back after restart.
TV Plus autorun and wakeupless voice were already off and were left that way.
The cache update changed only `$.rsp.preloadByCountry` in `RESERVED_18`.
Vendor refreshes may change preload preferences later.

Runtime masks were tested for contents recognition, sp-kpi and data-service.
They did not survive restart and are **not permanent cleanup**. These services
can run again; no enforcement daemon was installed. This is targeted disabling,
not an OS wipe or proof that every tracking path is eliminated.

## Earlier custom-overlay regression and rollback

The updated overlay includes a tested Advanced settings link to `org.tizen.menu`.
Home routing worked during the initial test. After assigning Home/startup and
disabling the sidebar/browser, a full restart exposed failed HDMI selection and
a black picture. Matching upstream's nonnegative `connect_source` success
convention and launching the TV viewer did not resolve it; requests returned
1 but current source remained 0. The native input selector also failed in
that state.

The two `light-shell` app records were restored, as were the main-operation
Home handler and Home preference (`com.samsung.tv.csfs`). The vendor had
already reverted startup to its recorded original `org.tizen.tv-viewer`;
rollback preserved that value after inspecting the drift. Following another
full restart, Austin confirmed HDMI picture and sound returned. Readback
confirmed current source 15 and all 43 cleanup records remained disabled.

Austin then confirmed picture, sound, volume and settings worked after
standby/wake. At that point, five groups were verified and the shell phase was
rolled back. The two changes were rolled back together, so evidence did not
isolate which caused the failure. The later actual Quiet Home port passed
Home-only, then light-shell, then startup/restart tests separately.

## Recovery locations and order

On-TV snapshots are under `/opt/usr/share/quiet-tizen-backups/`:

- `s95b-{advertising,voice,context,smart-extras,tv-plus}-20261025`:
  `.pkgmgr_parser.db`, `restore-registry.sh`, and applicable marker state plus
  `stop-marker.sh`.
- `s95b-preferences-20261025`: original web-app cache, preference/service
  records, `restore.sh`, and `restore-runtime.sh`.
- `s95b-light-shell-20261025` and `s95b-home-20261025`: already rolled back.
- `s95b-overlay-20261025`: original Samsung Bixby Promotion package recovery.
- `s95b-settings-link-20261025` and `s95b-source-fix-20261025`: successive
  overlay updates, original DLLs and hash-guarded `restore.sh` files.

Host snapshots and plans remain under `.local/backups/` and `.local/plans/`.
Raw command evidence is under `.local/sessions/`. No proprietary data is public.

Run saved scripts through the authenticated temporary-root route, using
`cat SCRIPT | /bin/sh` (direct execution of unsigned script paths is rejected
on this TV). Restore registry fields for the desired group first, then its
markers using the saved script:

```sh
cat "$D/restore-registry.sh" | /bin/sh
cat "$D/stop-marker.sh" | /bin/sh -s -- restore UNIT MARKER SNAPSHOT_NAME
```

Use only that snapshot's reviewed unit/marker pairs from the S95B profile.
Marker restoration uses recorded prior presence and activity. Stops/starts
are asynchronous to respect the root session's command budget; verify actual
completion separately. Do not replay an apply after timeout without inspection.

Preference rollback guards current values and restores only the changed
values/JSON field. If a vendor refresh caused drift, reconcile it rather than
replacing the live database. Runtime-mask restoration is only applicable while
the recorded masks still exist; they have already disappeared after restart.

To restore the stock Bixby package, undo the source-fix update, then the settings
link update, then the original overlay installation, in that order. Preserve
every original snapshot. Current Quiet Home adds later rollback layers listed
in [its recovery notes](s95b-home.md); unwind those first.
