# Palace Sprite Source

`source/` retains the supplied high-resolution JPEG masters. `keyed/` contains
their transparent PNG conversions, and `normalized/` contains the final
square-canvas source sprites. The runtime copies live in
`Resources/art/city/palace/`.

The Levels 1 through 6 progression uses complete hand-painted sprites. These
source files are flattened artwork, not a layered PSD/KRA document, so a roof,
wall, or banner cannot be edited independently without repainting it. Keeping
the keyed and normalized PNGs makes future artist replacement non-destructive.

To regenerate the runtime PNGs after replacing a keyed source image:

```powershell
python .\cocos-client\scripts\prepare-palace-sprites.py
```

The magenta background is removed before this script runs. Use the installed
chroma-key helper with `--auto-key border --soft-matte --despill` when adding a
new JPEG master.
