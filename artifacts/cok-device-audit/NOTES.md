# Connected-device UI audit notes

Package: `com.elex.ctw.mena` (`2.149.0`, version code `1415`)
Device viewport: 1236 x 2750 at 480 dpi
Capture date: 2026-07-21

These captures are local behavioral references only. They must not be shipped, copied into game content, or used as source assets.

## 1. Launch/loading

Evidence: `01-launch.png`

- Full-bleed portrait key art, product mark, one rotating gameplay tip, and a narrow progress indicator.
- Strong subject hierarchy but low tip contrast and no visible reduced-motion or accessibility controls.

## 2. City/home

Evidence: `02-home-or-loading.png` and `04-return-city.png`

- Persistent two-row top HUD: avatar/state, core resources, progression/power, and premium currency.
- The city itself remains the dominant interactive surface.
- Edge rails hold time-sensitive promotions and events without replacing the city.
- A contextual task prompt, scout/report strip, chat ticker, and five large bottom destinations stack above the system gesture area.
- Strength: nearly every important system is reachable in one tap.
- Risk: extreme visual density, small labels, icon-only affordances, overlapping prompts, and status communicated heavily through color and badges.

## 3. Research building

Evidence: `03-items.png`

- Building selection opens a focused full-screen subsystem rather than an overlay.
- A strong title/back bar precedes a spatial technology tree.
- Major branches use large circular nodes and explicit labels; locked secondary queue promotion remains persistent at the bottom.
- Strength: research categories are visually memorable and easy to scan.
- Risk: large nodes consume substantial vertical space and the monetization panel competes with the primary task.

## Capture limit

The current account is inside a forced `Help & Support` guide state. It intercepts city navigation, so Hero, Items, Mail, Alliance, and World could not be captured reliably without potentially sending an alliance request or changing account state. Ask the user to finish or dismiss that guide before the next device-audit pass.

## Clean-room implementation direction

Reproduce the interaction architecture, not the protected expression: dense isometric city, persistent resource/progression HUD, edge event rail, task and chat strips, five image-based destinations, focused subsystem screens, and clear back navigation. Use only original Luminous Frontier artwork, icons, writing, characters, heraldry, and layout measurements.
