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

## Owner's device cards and automatic discovery

Austin, 2026-10-25: implement automatic detection, using the input name
(`HDMI 1`, etc.) when detection is generic, rather than retaining custom names.
He subsequently clarified that Switch is on HDMI 4 and its playback works;
the problem was identification, not video. The final installed CEC-aware build
was confirmed by Austin: "Yes, correct now."

S95B builds refresh on Home entry/resume and every 30 seconds while Home is
visible. They combine two native sources:

- `libhdmicec-api.so.0`: device count, physical port and device name. A specific
  CEC name takes precedence. Actual results: `Switch 2` on HDMI 4,
  `Steam Machine` on HDMI 2, `Fire TV Stick` on HDMI 3.
- `libsource-api.so`: `get_source_list()` / `detectedName`, used when no specific
  CEC name is available. This list alone reported PC on HDMI 2 and an empty
  name on HDMI 4, explaining why the first implementation stayed generic.

`editName` and configured aliases are not naming fallbacks. Empty, generic or
unavailable identity becomes `HDMI N` with existing generic HDMI artwork.
Recognized device families select the existing local device illustrations;
new replacement artwork has not been selected. The Switch family currently
uses the existing Switch illustration, including the detected Switch 2.
All four configured ports remain selectable even if absent from detection;
names remain available while off only when Samsung still supplies them.
Ambiguous multiple CEC devices on one port do not supply an override.
The physical source IDs and card order stay fixed; detection changes labels
and artwork, never the input selected by a card.

CEC and source-list calls work inside the installed app without ongoing root
or a host helper. Matching native libraries were inspected to verify signatures
and allocation ownership: source JSON is `strdup`-owned, CEC names are allocated
by GLib. Buffers are freed after each refresh. Current diagnostics are written
locally to `/tmp/quiet-home-inputs.json` (overwritten, not sent anywhere).

An owner-authorized temporary `canalysis-service` trial produced no new source
names. Its stop marker was restored and the trial process stopped (MainPID 0;
the unit retained a failed status after requiring a targeted SIGKILL to finish
stopping). No registry flags were changed. CEC identifies the devices with
that service stopped, so it is not required for this implementation.

Tests cover percent-encoded names, generic-name fallback, absent/off devices,
changed port identities, malformed/duplicate source records and CEC precedence.
The S95B build compiled, installed hash matched, runtime discovery reported no
errors, and Austin confirmed the corrected cards. Fire TV playback and Switch
HDMI 4 playback were confirmed; new discovery after a physical device swap and
discovery-specific full restart testing remain unverified.

### Earlier manual configuration

Austin, 2026-10-25: HDMI 1 is Nintendo Switch, HDMI 2 Steam Machine, HDMI 3
Fire TV Stick, HDMI 4 PS4. The Switch and PS4 are connected but currently off.
He chose Fire TV for the large card and device artwork, and confirmed the
installed artwork/layout, navigation across all four cards, and HDMI 3 playback.

`launcher/assets/inputs.s95b.json` configures those four devices. The S95B build
copies it to the package's `quiet-home/inputs.json`; generic builds retain the
upstream three example devices. `InputLibrary` validates unique source IDs,
local artwork paths and supported card count. Current-source focus and Back
now include HDMI 4. Cards are configured independently of source detection,
so powered-off devices remain available. HDMI 1/4 playback awaits powered-on
testing. The home row retains Quiet Home styling, with Fire TV as an 800-pixel
featured card and three 280-pixel cards.

Austin, 2026-10-25: remove the mixed bottom app/input shortcut row and its
shelf, since header navigation already provides Apps. Center the input row
in the remaining space. A taller-card trial distorted the artwork and was
rejected; the installed result restores the original 442-pixel height and
centers the cards plus captions below the header (card top: 345 pixels).
Austin confirmed this corrected layout looks right. Down on the input row
stays there rather than selecting a removed dock item.

New Fire TV, Steam Machine and PS4 artwork consists of original vector device
illustrations; Switch artwork is the existing upstream asset. These replace
the example PlayStation 5/Switch assignments on the S95B.

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

- `s95b-cec-discovery-20261025`, then `s95b-input-discovery-20261025`:
  DLL-only discovery revisions, each with previous assembly, before hash and
  guarded `restore.sh` on TV and host. Undo these before layout revisions.
- `s95b-discovery-trial-20261025`: original analysis-service stop marker,
  service-state record and inverse script. Trial already reverted; its
  restore guard intentionally refuses to overwrite the restored marker.

- `s95b-centered-proportions-20261025`, `s95b-centered-inputs-20261025`,
  `s95b-no-dock-20261025`: DLL-only layout revisions, each with a previous
  assembly, before hash and guarded `restore.sh`, also copied to the host.
  Unwind in that order before the device-card layer. The middle revision
  is the rejected taller-card trial, not the accepted layout.

- `s95b-device-cards-20261025`: pre-card-update DLL, complete resource archive,
  before/after hashes and `restore.sh`. Host copies are under the same name
  in `.local/backups/`. Undo this update before earlier app rollback layers.

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
