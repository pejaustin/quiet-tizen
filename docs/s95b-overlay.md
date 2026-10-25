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
groups and boot recipes have **not** been validated on this device. Existing
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

### Not yet confirmed

- Input switching and picture writes from this overlay.
- Permanent package installation and physical Home-button routing.
- Full restart, standby/wake and running without the host after installation.
- Audio preservation during source changes and access to advanced native menus.

No stock app assembly, Home/startup selector or picture setting has been changed.

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
closes, and the timer bounds the session. This is not a permanent installation.

## Porting findings

- `Window.Default` is unavailable; this framework uses `GetDefaultWindow()`.
- `NUIApplication.Run` assumes registered `ApplicationInfo`. The **preview-only**
  bootstrap initializes the shipped internal NUI Application directly, avoiding
  stock package replacement. A deployed app must use normal lifecycle integration.
- The EFL-only experiment crashed while creating drawing objects and was replaced
  by the working NUI path. No kernel memory patches were attempted.
- `CSharpGetItemMinMaxStepValue` from upstream Quiet Home is absent. An older
  `settings_get_item_minmax_step` export was found by offline binary inspection,
  but calling it is not yet live-verified. Do not silently assume slider bounds.
- `CSharpGetItemDataList` truncates the long picture-mode list; do not treat that
  buffer as complete. The older `settings_get_item_data_list` export is another
  lead for a full list, also not yet live-verified.
- Main-operation Home lookup works. `.app-context.db` lacks the normal XF86Home
  override used on QN90A. Do not copy another model's routing rows.

## Next integration steps

1. Port and verify complete settings choices/ranges, then wire source and settings
   actions with native availability checks and readback.
2. Inventory the S95B's actual app containers, metadata, labels and routing schema.
3. Prepare private consistent snapshots and specific inverse operations before
   any package/routing change, following the upstream phased workflow.
4. Test a manually launched installed overlay before Home/startup assignment.
5. Verify source/audio, native settings access, restart and standby/wake.

Raw logs, network addresses, binaries and private backups belong under `.local/`.
Visual choices not approved by Austin remain listed in `PLACEHOLDERS.md`.
