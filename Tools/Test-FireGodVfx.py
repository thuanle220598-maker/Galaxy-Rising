from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
VFX = ROOT / "Assets/Resources/VFX/FireGod"
SHEETS = {
    "FireGodFlameProjectile.png": (48, 48, 8, 7.0),
    "FireGodFlameImpact.png": (64, 64, 8, 3.0),
    "FireGodAshOne.png": (32, 32, 8, 4.0),
    "FireGodAshTwo.png": (32, 32, 8, 2.0),
    "FireGodAshSigil.png": (64, 64, 8, 2.0),
    "FireGodAshWarning.png": (64, 64, 6, 2.0),
    "FireGodAshDetonation.png": (96, 96, 10, 3.0),
    "FireGodHellfireImpact.png": (128, 128, 12, 5.0),
    "FireGodMagmaLoop.png": (128, 128, 8, 4.0),
    "FireGodMagmaBurst.png": (128, 128, 10, 5.0),
    "FireGodCrimsonGaleCore.png": (256, 160, 48, 105.0),
    "FireGodCrimsonGaleRibbon.png": (256, 160, 48, 110.0),
    "FireGodCrimsonGaleSparks.png": (256, 160, 48, 145.0),
    "FireGodCrimsonGaleImpact.png": (256, 160, 48, 150.0),
    "FireGodCrimsonGaleResidue.png": (256, 160, 48, 3.0),
    "FireGodScorchLoop.png": (192, 128, 8, 5.0),
    "FireGodFirestormConvert.png": (192, 192, 10, 12.0),
    "FireGodFirestormLoop.png": (192, 192, 8, 8.0),
    "FireGodFlameShieldSpawn.png": (96, 96, 8, 4.0),
    "FireGodFlameShieldLoop.png": (96, 96, 8, 4.0),
    "FireGodFlameShieldBreak.png": (96, 96, 6, 6.0),
    "FireGodHeatAura.png": (96, 96, 8, 4.0),
    "FireGodEmberProjectile.png": (48, 48, 8, 7.0),
    "FireGodEmberIgnite.png": (64, 64, 6, 4.0),
    "FireGodAshDissolve.png": (96, 96, 10, 18.0),
}

SPECTACLE_SHEETS = {
    filename for filename in SHEETS if filename.startswith("FireGodCrimsonGale")
}


def main():
    spectacle_colors = []
    for filename, (width, height, count, max_center_drift) in SHEETS.items():
        path = VFX / filename
        assert path.exists(), f"Missing VFX sheet: {path}"
        image = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
        assert image is not None and image.shape == (height, width * count, 4), (
            f"{filename} must be {width * count}x{height} RGBA"
        )

        centers = []
        for index in range(count):
            frame = image[:, index * width:(index + 1) * width]
            alpha = frame[:, :, 3]
            points = np.argwhere(alpha > 0)
            assert len(points) >= 4, f"{filename} frame {index} is empty"
            centers.append(points.mean(axis=0))

            colored = frame[alpha > 0, :3]
            if filename in SPECTACLE_SHEETS:
                spectacle_colors.append(colored)
            else:
                assert np.all(colored[:, 2] >= colored[:, 1]), f"{filename} contains non-fire hues"
                assert np.all(colored[:, 2] >= colored[:, 0]), f"{filename} contains non-fire hues"

        centers = np.asarray(centers)
        drift = np.linalg.norm(centers - np.median(centers, axis=0), axis=1).max()
        assert drift <= max_center_drift, f"{filename} center drifts {drift:.2f}px"

    colors = np.vstack(spectacle_colors)
    blue, green, red = colors[:, 0], colors[:, 1], colors[:, 2]
    assert np.any((red > green + 35) & (red > blue + 20)), "Crimson Gale lacks warm fire"
    assert np.any((red > green + 35) & (blue > green + 35)), "Crimson Gale lacks violet energy"
    assert np.any((blue > red + 50) & (green > red + 50)), "Crimson Gale lacks cyan sparks"

    impact = cv2.imread(str(VFX / "FireGodCrimsonGaleImpact.png"), cv2.IMREAD_UNCHANGED)
    for index in range(22, 25):
        frame = impact[:, index * 256:(index + 1) * 256]
        opaque_ratio = np.count_nonzero(frame[:, :, 3] > 0) / (256 * 160)
        white_ratio = np.count_nonzero(
            (frame[:, :, 0] > 220) & (frame[:, :, 1] > 220) &
            (frame[:, :, 2] > 220) & (frame[:, :, 3] > 0)
        ) / (256 * 160)
        assert opaque_ratio >= 0.55, f"Crimson Gale flash {index} covers only {opaque_ratio:.1%}"
        assert white_ratio >= 0.18, f"Crimson Gale flash {index} lacks white-hot exposure"

    ribbon = cv2.imread(str(VFX / "FireGodCrimsonGaleRibbon.png"), cv2.IMREAD_UNCHANGED)
    silhouette_coverage = []
    for index in range(12, 22):
        frame = ribbon[:, index * 256:(index + 1) * 256]
        silhouette_coverage.append(np.count_nonzero(frame[:, :, 3] > 0) / (256 * 160))
    assert max(silhouette_coverage) >= 0.28, "Crimson Gale lacks a battlefield-scale silhouette"

    print(f"Fire God VFX asset check passed: {len(SHEETS)} sheets")


if __name__ == "__main__":
    main()
