from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
VFX = ROOT / "Assets/Resources/VFX/FireGod"
OUTPUT = ROOT / "Temp/FireGodVfxPreviewFrames"
CRIMSON_OUTPUT = ROOT / "Temp/FireGodCrimsonGalePreview"
ASSETS = [
    ("FLAME PROJECTILE", "FireGodFlameProjectile.png", 48, 8),
    ("ASH SIGIL", "FireGodAshSigil.png", 64, 8),
    ("ASH DETONATION", "FireGodAshDetonation.png", 96, 10),
    ("HELLFIRE IMPACT", "FireGodHellfireImpact.png", 128, 12),
    ("MAGMA POOL", "FireGodMagmaLoop.png", 128, 8),
    ("CRIMSON GALE CORE", "FireGodCrimsonGaleCore.png", (256, 160), 48),
    ("SCORCH ZONE", "FireGodScorchLoop.png", (192, 128), 8),
    ("FIRESTORM", "FireGodFirestormLoop.png", 192, 8),
    ("FLAME SHIELD", "FireGodFlameShieldLoop.png", 96, 8),
]


def composite(panel, sprite, x, y):
    alpha = sprite[:, :, 3:4] / 255.0
    target = panel[y:y + sprite.shape[0], x:x + sprite.shape[1], :3]
    target[:] = (sprite[:, :, :3] * alpha + target * (1 - alpha)).astype(np.uint8)


def battlefield_background():
    image = np.zeros((1920, 1080, 3), dtype=np.uint8)
    for y in range(1920):
        blend = y / 1919
        image[y, :] = (
            round(24 + blend * 13),
            round(10 + blend * 11),
            round(17 + blend * 15),
        )
    cv2.rectangle(image, (0, 690), (1080, 1340), (38, 22, 31), -1)
    cv2.rectangle(image, (0, 1110), (1080, 1340), (30, 17, 24), -1)
    for x in range(-120, 1200, 120):
        cv2.line(image, (540, 1110), (x, 1340), (47, 28, 38), 2, cv2.LINE_AA)
    for y in range(1140, 1340, 45):
        cv2.line(image, (0, y), (1080, y), (44, 26, 35), 1, cv2.LINE_AA)
    cv2.ellipse(image, (145, 1065), (58, 20), 0, 0, 360, (15, 9, 15), -1, cv2.LINE_AA)
    cv2.rectangle(image, (118, 930), (171, 1063), (18, 12, 23), -1)
    cv2.circle(image, (145, 910), 31, (18, 12, 23), -1, cv2.LINE_AA)
    cv2.ellipse(image, (912, 1065), (74, 23), 0, 0, 360, (15, 9, 15), -1, cv2.LINE_AA)
    cv2.rectangle(image, (878, 920), (946, 1062), (20, 14, 24), -1)
    cv2.circle(image, (912, 896), 38, (20, 14, 24), -1, cv2.LINE_AA)
    return image


def build_crimson_gale_preview():
    CRIMSON_OUTPUT.mkdir(parents=True, exist_ok=True)
    layer_names = ["Residue", "Ribbon", "Core", "Impact", "Sparks"]
    layers = {
        name: cv2.imread(
            str(VFX / f"FireGodCrimsonGale{name}.png"), cv2.IMREAD_UNCHANGED)
        for name in layer_names
    }
    for output_index in range(144):
        image = battlefield_background()
        frame_index = output_index if output_index < 48 else (output_index - 48) // 2
        frame_index = min(frame_index, 47)
        for name in layer_names:
            sheet = layers[name]
            frame = sheet[:, frame_index * 256:(frame_index + 1) * 256]
            frame = cv2.resize(frame, (1024, 640), interpolation=cv2.INTER_NEAREST)
            composite(image, frame, 28, 650)

        speed = "1.0X" if output_index < 48 else "0.5X"
        phases = ("CHARGE", "RELEASE", "IMPACT", "RECOVERY")
        phase = phases[0 if frame_index <= 7 else 1 if frame_index <= 21 else 2 if frame_index <= 30 else 3]
        cv2.putText(image, "FIRE GOD HEAVENLY DEMON", (62, 116),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.82, (132, 193, 255), 2, cv2.LINE_AA)
        cv2.putText(image, "CRIMSON GALE - SPECTACLE PASS", (62, 162),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.72, (214, 105, 235), 2, cv2.LINE_AA)
        cv2.putText(image, f"{phase}  FRAME {frame_index + 1:02}/48  {speed}", (62, 1780),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.65, (230, 238, 255), 2, cv2.LINE_AA)
        cv2.imwrite(str(CRIMSON_OUTPUT / f"frame_{output_index:03}.png"), image)


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    loaded = []
    for label, filename, size, count in ASSETS:
        width, height = (size, size) if isinstance(size, int) else size
        sheet = cv2.imread(str(VFX / filename), cv2.IMREAD_UNCHANGED)
        loaded.append((label, sheet, width, height, count))

    for output_index in range(96):
        image = np.zeros((1080, 1920, 3), dtype=np.uint8)
        image[:] = (12, 7, 14)
        for asset_index, (label, sheet, width, height, count) in enumerate(loaded):
            column = asset_index % 3
            row = asset_index // 3
            panel_x = column * 640
            panel_y = row * 360
            cv2.rectangle(image, (panel_x + 8, panel_y + 8),
                          (panel_x + 632, panel_y + 352), (42, 24, 54), 2)
            cv2.putText(image, label, (panel_x + 24, panel_y + 44),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.8, (95, 185, 255), 2, cv2.LINE_AA)
            frame_index = (output_index * count // 24) % count
            frame = sheet[:, frame_index * width:(frame_index + 1) * width]
            scale = min(5, 520 // width, 250 // height)
            frame = cv2.resize(frame, (width * scale, height * scale), interpolation=cv2.INTER_NEAREST)
            x = panel_x + (640 - frame.shape[1]) // 2
            y = panel_y + 65 + (270 - frame.shape[0]) // 2
            composite(image, frame, x, y)
        cv2.imwrite(str(OUTPUT / f"frame_{output_index:03}.png"), image)

    build_crimson_gale_preview()
    print(OUTPUT)
    print(CRIMSON_OUTPUT)


if __name__ == "__main__":
    main()
