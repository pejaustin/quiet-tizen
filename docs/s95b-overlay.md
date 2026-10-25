# S95B overlay port

## Owner's design

- Austin, 2026-10-23: simple, responsive input selection and picture settings only.
- Austin, 2026-10-23: a small overlay over the current picture.
- Austin, 2026-10-24: fork Quiet Tizen and use it as the base for customization.

This branch extends Quiet Tizen with an S95B-specific launcher variant. Its
build command uses `tools/build.py`; its diagnostic runner reuses Quiet Tizen's
explicit-device identification, private-output rules and vendored SDK transport.
Native source/settings interfaces follow Quiet Tizen and samsung-tv-root research.

## Verified target and limits

| Field | Observed value |
|---|---|
| Exact model | QN55S95BAFXZA |
| About-screen firmware (owner supplied) | T-PTMAKUC-1720.7 |
| Live build ID | T-PTMAKUC-REL-202602250225 |
| Platform | Tizen 6.5, ARMv7, Linux 5.4.77 |
| Runtime family | Samsung .NET 3.1 |
| Main-operation Home handler | com.samsung.tv.csfs |

The upstream U8000F uses .NET 6 and Tizen 9. Its installation container, cleanup
groups and boot recipes must not be reused without validation. The S95B's
Bixby Promotion container was separately inventoried and tested. Existing
`tools/tv.py` mutation commands still target only the upstream U8000F profile.

### Confirmed on TV

- Temporary authenticated UID/EUID 0 with samsung-tv-root v0.0.12 archive route;
  cleanup and original passwd/SDB-key hash checks passed.
- Source enumeration and current-source reads through `libsource-api.so`.
- Picture values, visibility and enablement through `libsettings-api.so`.
- NUI translucent notification window over the current HDMI picture.
- Austin confirmed the physical Up/Down keys move the highlight and Back closes
  the panel, 2026-10-24. Callback logs independently recorded those keys.
- Preview stopped cleanly and its staged files were removed.
- Native slider ranges and full choice lists were live-verified, including
  per-choice enablement and visibility.
- Brightness writes read back correctly during the remote trial. The original
  value of 50 was restored with source/mode/value guards and verified readback.
- Installed into the inventoried Bixby Promotion container using the normal
  registered NUI lifecycle. Austin, 2026-10-25: overlay, navigation, HDMI
  switching, Back, picture and sound all work during the manual-launch test.
- Austin, 2026-10-25: installed Color Tone control visibly changed the HDMI
  picture toward Warm2; he restored the starting tone afterward.

### Not yet confirmed

- Brightness-specific visible effect: the earlier temporary trial showed
  value changes only. Installed Color Tone's visible effect is confirmed.
- Physical Home-button routing.
- Full restart, standby/wake and running without the host after installation.
- Access to advanced native menus from the installed overlay.

The Bixby Promotion assembly/runtime configuration are replaced. Home/startup
selectors and registry flags are unchanged. Original package and consistent
database snapshots are kept on the TV and in the host's ignored `.local/`.

## Build

Keep the four reference DLLs from **this TV** in `.local/tv-framework/`:
`Tizen.NUI`, `Tizen.Applications.Common`, `Tizen.System`, and `Tizen`.
They are excluded from both Git and the build output. Use an installed .NET SDK
capable of targeting `netcoreapp3.1` (the prototype was built with SDK 9.0.304).

```sh
python3 tools/build.py s95b-overlay --framework .local/tv-framework
```

Output: `.local/build/S95bOverlay/`. This target deliberately does not assemble
the U8000F's `.local/package/` deployment bundle.

## Bounded preview

Samsung SDB and the tested external samsung-tv-root v0.0.12 release are needed
for the current temporary-root preview. Use their actual local paths. The
vendored access payloads have not yet been adapted/built for .NET 3.1.

```sh
python3 tools/s95b_preview.py --host "$TV_IP" --sdb "$SDB" \
  --root-cli "$SAMSUNG_TV_ROOT" --seconds 25 \
  --log .local/sessions/s95b-preview.txt
```

The runner checks the exact model and live build, uploads to a unique staging
directory, and removes its own files afterward. `--inspect` reads APIs instead
of opening a window. Preview values are read-only. Up/Down navigates, Back
closes, and the timer bounds the session. `--controls` enables OK for inputs
and Left/Right for picture settings. `--restore KEY EXPECTED ORIGINAL SOURCE MODE`
restores a numeric test value only while its context still matches.

## Porting findings

- `Window.Default` is unavailable; this framework uses `GetDefaultWindow()`.
- `NUIApplication.Run` assumes registered `ApplicationInfo`. The **preview-only**
  bootstrap initializes the shipped internal NUI Application directly, avoiding
  stock package replacement. A deployed app must use normal lifecycle integration.
- The EFL-only experiment crashed while creating drawing objects and was replaced
  by the working NUI path. No kernel memory patches were attempted.
- `CSharpGetItemMinMaxStepValue` from upstream Quiet Home is absent. An older
  `settings_get_item_minmax_step` export was found by offline binary inspection,
   and live-verified with double outputs. Use native slider bounds.
- `CSharpGetItemDataList` truncates the long picture-mode list; do not treat that
  buffer as complete. The older `settings_get_item_data_list` export is another
   live-verified source of the complete allocated JSON string.
- Main-operation Home lookup works. `.app-context.db` lacks the normal XF86Home
  override used on QN90A. Do not copy another model's routing rows.

## Next integration steps

1. Verify access to advanced native settings.
2. Prepare field-specific Home/startup rollback, then assign and test routing.
3. Verify full restart and standby/wake separately.

## Installed-app recovery

The S95B-specific recipes are `s95b-backup-launcher.sh`,
`s95b-install-launcher.sh`, and `s95b-restore-launcher.sh` under `recipes/`.
The verified snapshot is `s95b-overlay-20261025` under
`/opt/usr/share/quiet-tizen-backups/`; a hash-verified `recovery.tar.gz` is also
saved at `.local/backups/s95b-overlay-20261025/` on the host.
The on-TV snapshot includes `restore-launcher.sh`. Run it through the reviewed
temporary-root route with the snapshot name as its argument to restore the
two stock files and native-image cache. It checks for file drift first and
does not overwrite live databases. These recipes do not change Home selectors.

Manual launch uses `app_launcher -u 5001 -s com.samsung.tv.bixby-promotion`.
The installed app exits after 90 seconds of inactivity or Back; it uses the
vendor-provided environment and does not require a host rendering process.

Raw logs, network addresses, binaries and private backups belong under `.local/`.
Visual choices not approved by Austin remain listed in `PLACEHOLDERS.md`.
