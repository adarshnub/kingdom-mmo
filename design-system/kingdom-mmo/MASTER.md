# Kingdom MMO — Unity UI Design System

This document is the visual source of truth for the original Unity client. The supplied commercial-game screenshots are used only to identify common 4X information architecture; no names, art, icons, frames, layouts, or branding are copied.

## Direction

**Luminous Frontier** combines pale stone, deep ocean slate, restrained aged brass, and teal energy. City and map artwork stays bright and readable while controls use dark translucent surfaces. Ornament is reserved for titles, primary calls to action, and progression moments.

The interface targets portrait phones first at a 1080 × 1920 reference resolution and scales to tablets and landscape without relying on fixed device pixels.

## Semantic colors

| Token | Value | Use |
|---|---:|---|
| `Background` | `#0B1720` | Loading, login, and fallback backdrop |
| `Surface` | `#14242C` | HUD bars and cards |
| `SurfaceRaised` | `#20343B` | Selected cards, sheets, and navigation |
| `Ivory` | `#F4E8CC` | Primary text |
| `Muted` | `#B8C3BE` | Secondary text |
| `Brass` | `#D7A548` | Primary action and progression |
| `Teal` | `#1E7A78` | Selection and alliance state |
| `Energy` | `#52C7C2` | Timers, focus, and active marches |
| `Success` | `#5FA66D` | Completed and affordable states |
| `Danger` | `#C9534C` | Errors and destructive actions |
| `Scrim` | `rgba(3, 10, 14, 0.72)` | Readability over key art |
| `Hairline` | `rgba(244, 232, 204, 0.16)` | Borders and dividers |

Color never communicates status alone: pair it with a label, symbol, or progress value. Normal text must meet 4.5:1 contrast.

## Typography

- Display/title: humanist or inscription-inspired serif, semibold, used sparingly.
- Body/UI: highly legible sans serif with tabular figures for resources and timers.
- Prototype: TextMeshPro default font; production fonts must be OFL-licensed and added through Addressables.
- Scale: 24, 28, 32, 40, 52, and 68 at the 1080 × 1920 reference resolution.
- Minimum production body size: equivalent to 16sp on a phone.

## Spacing and geometry

- Base rhythm: 8 units; supported steps are 8, 16, 24, 32, 48, and 64.
- Minimum touch target: 96 × 96 at reference resolution, corresponding to at least 48dp.
- Screen gutter: 32; card padding: 24; component gap: 16.
- Corners: 10 for small controls, 18 for cards, 28 for sheets.
- Borders: one restrained brass or ivory hairline; avoid stacked ornamental frames.
- All fixed HUD and navigation elements must live inside the device safe area.

## Screen frame

1. Safe-area top HUD: player identity, resources, power, and premium currency.
2. Primary content: one dominant task or navigable environment.
3. Context rail: no more than three time-sensitive actions at once.
4. Status strip: chat, march, or queue state; collapsible.
5. Bottom navigation: at most five labeled destinations.

Back behavior preserves scroll position and selected filters. Deep screens use a visible title and back action rather than changing the primary navigation.

## Components

- **Primary button:** brass fill, dark text, one clear action per screen.
- **Secondary button:** raised slate surface, ivory label.
- **Quiet button:** transparent surface for navigation and filters.
- **Danger button:** muted red, separated from normal actions.
- **Card:** translucent slate with a single border; no heavy drop shadow.
- **Progress:** dark track, teal or brass fill, numeric label always visible.
- **Resource chip:** icon placeholder, abbreviated value, full accessible label.
- **List row:** minimum 112 high, title plus one metadata line and clear destination chevron.
- **Inventory tile:** square, rarity indicated by both border treatment and text label.

## Motion

- Press feedback: 100–150 ms opacity or scale to 0.97 without changing layout bounds.
- Screen transition: 220–300 ms directional fade/slide.
- Toast: 200 ms in, visible 3–5 seconds, 150 ms out.
- Animations must be interruptible and disabled when reduced motion is enabled.
- Do not allocate or search the scene in per-frame UI code.

## Artwork

- All characters, environments, crests, icons, and visual effects are original.
- Key art contains no baked-in labels, currencies, buttons, or frames.
- Environment backdrops preserve quiet top and bottom regions for HUD readability.
- Production sprites use atlases and Addressables; repeating rows and map markers use pooling.

## Acceptance checklist

- Touch targets are at least 48dp with 8dp separation.
- Five or fewer primary navigation destinations, each with icon and label.
- No content is hidden behind notches, status bars, or gesture areas.
- Lists remain usable on 375px-wide phones and tablets.
- Text and controls remain readable over every background.
- Loading, empty, offline, disabled, success, and error states are explicit.
- No emoji structural icons, copied commercial assets, or placeholder brand names ship.
- City ↔ world transitions, login, inventory, mail, alliance, hero, and training flows are covered by PlayMode smoke tests before alpha.
