"""Deterministic validation for Kronos character and VFX sheets."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
CHARACTER = ROOT / "Assets" / "Art" / "Characters" / "Kronos" / "Combat"
VFX = ROOT / "Assets" / "Resources" / "VFX" / "Kronos"

SHEETS = {
    CHARACTER / "KronosIdle.png": (64, 8),
    CHARACTER / "KronosRun.png": (64, 8),
    CHARACTER / "KronosBasic.png": (64, 8),
    CHARACTER / "KronosActive.png": (64, 12),
    CHARACTER / "KronosUltimate.png": (64, 14),
    CHARACTER / "KronosHit.png": (64, 5),
    CHARACTER / "KronosDeath.png": (64, 10),
    VFX / "KronosDimensionalTear.png": (96, 10),
    VFX / "KronosVoidField.png": (128, 12),
    VFX / "KronosMassOrbit.png": (64, 10),
    VFX / "KronosResonanceBurst.png": (96, 10),
    VFX / "KronosParasite.png": (32, 8),
    VFX / "KronosParasiteDrain.png": (64, 8),
    VFX / "KronosSingularity.png": (128, 12),
    VFX / "KronosLeviathanAwaken.png": (128, 14),
    VFX / "KronosLeviathanAura.png": (96, 12),
    VFX / "KronosVoidSlash.png": (96, 8),
    VFX / "KronosCollapse.png": (96, 10),
    VFX / "KronosDecayPulse.png": (96, 10),
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
    print(f"validated {len(SHEETS)} Kronos sheets")


if __name__ == "__main__":
    main()
