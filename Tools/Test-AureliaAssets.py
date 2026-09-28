"""Small deterministic validation for Aurelia's generated raster assets."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
CHARACTER = ROOT / "Assets" / "Art" / "Characters" / "Aurelia" / "Combat"
VFX = ROOT / "Assets" / "Resources" / "VFX" / "Aurelia"

SHEETS = {
    CHARACTER / "AureliaIdle.png": (64, 6),
    CHARACTER / "AureliaRun.png": (64, 8),
    CHARACTER / "AureliaBasic.png": (64, 6),
    CHARACTER / "AureliaActive.png": (64, 10),
    CHARACTER / "AureliaUltimate.png": (64, 12),
    CHARACTER / "AureliaHit.png": (64, 4),
    CHARACTER / "AureliaDeath.png": (64, 8),
    VFX / "AureliaWaterSerpent.png": (32, 8),
    VFX / "AureliaHydroBead.png": (16, 6),
    VFX / "AureliaCleansingRing.png": (64, 8),
    VFX / "AureliaDragonAura.png": (64, 8),
    VFX / "AureliaDragonPressure.png": (96, 10),
    VFX / "AureliaDomain.png": (128, 12),
    VFX / "AureliaShieldLoop.png": (64, 8),
    VFX / "AureliaShieldBreak.png": (64, 8),
    VFX / "AureliaUltimateDragon.png": (128, 12),
    VFX / "AureliaBlessingImpact.png": (64, 8),
}


def main():
    for path, (size, count) in SHEETS.items():
        image = Image.open(path).convert("RGBA")
        assert image.size == (size * count, size), f"bad dimensions: {path} {image.size}"
        assert len(image.getcolors(maxcolors=1 << 20)) <= 64, f"too many colors: {path}"
        assert {alpha for _, alpha in image.getchannel("A").getcolors()} <= {0, 255}, \
            f"semi-transparent pixels: {path}"
        for index in range(count):
            frame = image.crop((index * size, 0, (index + 1) * size, size))
            assert frame.getchannel("A").getbbox(), f"empty frame {index}: {path}"
    print(f"validated {len(SHEETS)} Aurelia sheets")


if __name__ == "__main__":
    main()
