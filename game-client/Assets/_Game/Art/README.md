# Original Unity prototype artwork

These project-bound raster assets were generated on 2026-07-21 with OpenAI's built-in image-generation tool. The supplied commercial-game screenshots were **not** passed into generation; they informed only the high-level list of 4X screen types.

## Assets and prompt summaries

- `Backgrounds/capital-city.png` — portrait isometric coastal citadel; pale limestone, slate roofs, teal aqueducts, aged brass accents, warm sunrise; quiet HUD regions; environment only; no UI, text, logo, characters, copied buildings, or recognizable commercial layout.
- `Backgrounds/world-frontier.png` — portrait high-isometric frontier valley; rivers, forests, farms, mountains, ruins, quarry, watchtowers, and an observatory; readable landmark spacing for dynamic markers; no UI, text, flags, armies, grid, or copied map composition.
- `Backgrounds/frontier-marshal.png` — original adult frontier marshal in ivory-and-charcoal lamellar armor, teal half-cape, aged-brass fittings, and celestial circlet; citadel-cloud backdrop; no logo, text, celebrity likeness, known heraldry, or copied costume.
- `Backgrounds/capital-city-v2.png` — dense portrait high-isometric Luminous Frontier capital with separated interactive districts, ivory terraces, teal waterways, and an original astral observatory; no UI, text, characters, logos, or copied city composition.
- `UI/Navigation/nav-city.png` — original ivory citadel medallion with teal beacon.
- `UI/Navigation/nav-hero.png` — original frontier helm medallion with teal plume.
- `UI/Navigation/nav-inventory.png` — original brass-bound explorer chest medallion.
- `UI/Navigation/nav-mail.png` — original ivory dispatch and teal ribbon medallion.
- `UI/Navigation/nav-alliance.png` — original clasped-gauntlets alliance medallion.

Unity imports backgrounds as single non-mipmapped sprites with a 2048 maximum size and navigation art with a 512 maximum size. Android uses ASTC 6×6 overrides. Production art should retain the same original-only constraints and move to versioned Addressables groups.
