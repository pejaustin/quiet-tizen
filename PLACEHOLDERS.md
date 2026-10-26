# S95B customization placeholders

- S95B device cards are settled: Austin supplied all four device names/ports,
  selected Fire TV as the large card and device artwork, then confirmed the
  installed result looks good and works (2026-10-25). Upstream example inputs
  remain only in the generic `inputs.json`; S95B builds use `inputs.s95b.json`.
  Later on 2026-10-25 Austin requested automatic device detection instead of
  manual names, with `HDMI N` for generic identities. The installed build uses
  CEC/source metadata and existing artwork; Austin confirmed the result.
  New human-designed replacement artwork is still unselected.

- `launcher/s95b-overlay/OverlayWindow.cs`: prototype layout, typography, colors
  and helper wording. Austin chose a small overlay, but has not approved this
  visual treatment. The successful visibility/navigation test is not art approval.
