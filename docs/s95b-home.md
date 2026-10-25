# Quiet Home on the S95B

Austin, 2026-10-25: implement actual Quiet Tizen first, not our custom overlay;
customize only where upstream does not meet his needs. Streaming belongs on an
HDMI-connected device, not in TV apps.

## Upstream launcher compatibility port

`launcher/src/Home.S95b.csproj` compiles the same `Home.cs` and `AppLibrary.cs`
as upstream. Compatibility changes are the .NET 3.1 target with private TV
references, `GetDefaultWindow()`, the verified older brightness-range export,
and default image sampling instead of the unavailable newer sampling property.
The layout, fonts, artwork, library, Quick Settings, focus/navigation and
Back-to-input behavior are upstream code.

```sh
python3 tools/build.py s95b-launcher --framework .local/tv-framework
```

Build: `.local/build/S95bHome/`. Package: `.local/package-s95b/`, including the
runtime configuration and original assets. No Samsung references are bundled.
All 12 upstream catalog IDs were found in the TV's registry.

Original example console labels/art remain for this baseline: HDMI 3 says
Nintendo Switch but connects to Austin's Fire TV; HDMI 2 says PlayStation but
the observed device was PC. HDMI 1 is an upstream example absent from the
enumerated source list. These are demo content, not owner-designed labels.

## Verified installation

- Installed actual Quiet Home into the inventoried Bixby Promotion container,
  with backed-up previous DLL/runtime configuration and hash-guarded recovery.
  Recipe: `recipes/s95b-install-home.sh`.
- Austin confirmed the GitHub-style interface, physical Home button,
  HDMI 3 picture/sound, and Advanced Settings opening the native menu.
- Home-only integration passed a full restart. Adding sidebar/browser cleanup
  passed another full restart. Automatic Quiet Home startup was then assigned
  separately and passed restart and standby/wake tests.
- Final readback: main-operation handler and Home/startup preferences all
  `com.samsung.tv.bixby-promotion`; active source 15; 45 disabled app records.
- Native state reported custom fonts loaded, 14 artwork views ready, 12 library
  entries and no catalog error. No host rendering process is required.

TV-app launches, actual controller pairing and every individual Quick Settings
adjustment have not been comprehensively tested. Austin uses an HDMI device
for streaming. The app shortcuts remain as part of the requested baseline.

## Settings routing adaptation

This TV has no `com.samsung.tv.quicklaunch` package. Its numeric/options
`XF86More` rules are different. Rather than copying an incompatible bridge,
`recipes/s95b-settings-routing.sh` routes its existing `org.tizen.menu` /
`XF86SysMenu` row to Quiet Home with `quiet_page:settings`. Apply/restore guard
the specific fields; round-trip and drift tests cover the recipe SQL.
Numeric and accessibility rules are retained.

The owner has no dedicated Settings/Menu button, so physical activation of
this route is unverified. Home → Settings → Advanced was tested. Native
settings, networking, Bluetooth and accessibility packages remain available.

## Cleanup and recovery

All six S95B groups total 45 disabled app records. Nine service stop markers
and the voice/preload preferences survived restart. Runtime-only masks remain
nonpersistent; vendor services without those persistent controls can run again.
See [cleanup evidence](s95b-cleanup.md).

Later snapshots under `/opt/usr/share/quiet-tizen-backups/`:

- `s95b-quiet-home-20261025`: previous overlay DLL/runtime configuration,
  before/expected hashes, and `restore.sh`.
- `s95b-quiet-home-routing-20261025`: original main/Home selectors, Probe,
  and `restore.sh` (main/Home only).
- `s95b-quiet-home-startup-20261025`: original startup `org.tizen.tv-viewer`
  and `restore.sh`.
- `s95b-quiet-settings-20261025`: consistent `.app-context.db` and
  `routing.sh` supporting `restore SNAPSHOT_NAME`.
- `s95b-quiet-light-shell-20261025`: registry snapshot and `restore-registry.sh`.

Use the authenticated root route and pipe saved scripts into `/bin/sh`;
unsigned-script path execution is rejected. Restore light-shell, settings
routing, Home and startup before reverting the app. Reconcile any drift rather
than overwriting live databases. To restore the original Samsung app completely,
then unwind Quiet Home, source-fix, settings-link and initial overlay installs
in reverse order. Artwork can remain inert. Keep every original backup.

Host recovery copies and evidence remain under `.local/`; public files contain
only source, sanitized compatibility findings and recipes.
