"""Normalize transparent Palace source art for runtime use in Cocos2d-x."""

from __future__ import annotations

from pathlib import Path

from PIL import Image


CANVAS_SIZE = 1024
GROUND_BASELINE = 900
CLIENT_ROOT = Path(__file__).resolve().parents[1]
KEYED_ROOT = CLIENT_ROOT / "art-source" / "city" / "palace" / "keyed"
NORMALIZED_ROOT = CLIENT_ROOT / "art-source" / "city" / "palace" / "normalized"
RUNTIME_ROOT = CLIENT_ROOT / "Resources" / "art" / "city" / "palace"

# Each stage remains anchored on the same stone footprint. Wider late-game
# citadels deliberately get more horizontal room instead of being squeezed.
SPRITES = (
    ("palace_level_1_keyed.png", "palace_level_1.png", 820, 820),
    ("palace_level_2_keyed.png", "palace_level_2.png", 820, 820),
    ("palace_level_3_keyed.png", "palace_level_3.png", 840, 820),
    ("palace_level_4_keyed.png", "palace_level_4.png", 920, 720),
    ("palace_level_5_keyed.png", "palace_level_5.png", 920, 720),
    ("palace_level_6_keyed.png", "palace_level_6.png", 970, 620),
)


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    # Ignore faint residual JPEG-key pixels when finding the subject bounds.
    opaque = alpha.point(lambda value: 255 if value > 128 else 0)
    bbox = opaque.getbbox()
    if bbox is None:
        raise ValueError("The image has no visible Palace pixels.")
    return bbox


def clear_alpha_noise(image: Image.Image) -> Image.Image:
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha < 24:
                pixels[x, y] = (0, 0, 0, 0)
    return image


def crop_with_padding(image: Image.Image, bbox: tuple[int, int, int, int]) -> Image.Image:
    left, top, right, bottom = bbox
    padding = 12
    return image.crop(
        (
            max(0, left - padding),
            max(0, top - padding),
            min(image.width, right + padding),
            min(image.height, bottom + padding),
        )
    )


def normalize(source: Image.Image, max_width: int, max_height: int) -> Image.Image:
    crop = crop_with_padding(source, alpha_bbox(source))
    scale = min(max_width / crop.width, max_height / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    sprite = crop.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - sprite.width) // 2
    y = GROUND_BASELINE - sprite.height
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def main() -> None:
    NORMALIZED_ROOT.mkdir(parents=True, exist_ok=True)
    RUNTIME_ROOT.mkdir(parents=True, exist_ok=True)

    for source_name, output_name, max_width, max_height in SPRITES:
        source_path = KEYED_ROOT / source_name
        if not source_path.exists():
            raise FileNotFoundError(f"Missing keyed source: {source_path}")

        with Image.open(source_path) as opened:
            source = clear_alpha_noise(opened.convert("RGBA"))
        output = normalize(source, max_width, max_height)
        output.save(NORMALIZED_ROOT / output_name, "PNG", compress_level=6)
        output.save(RUNTIME_ROOT / output_name, "PNG", compress_level=6)
        print(f"Wrote {output_name}")


if __name__ == "__main__":
    main()
