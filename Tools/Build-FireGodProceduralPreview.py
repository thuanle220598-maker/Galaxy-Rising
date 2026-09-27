from pathlib import Path

import cv2


ROOT = Path(__file__).resolve().parents[1]
FRAMES = ROOT / "Library/FireGodProceduralPreviewFrames"
OUTPUT = ROOT / "Previews/FireGodCrimsonGaleProcedural.mp4"
FPS = 60
NATIVE_FRAMES = 96
SLOW_FRAMES = 192


def main():
    paths = [FRAMES / f"frame_{index:03}.png"
             for index in range(NATIVE_FRAMES + SLOW_FRAMES)]
    missing = [path for path in paths if not path.exists()]
    if missing:
        raise SystemExit(f"Missing preview frame: {missing[0]}")

    first = cv2.imread(str(paths[0]))
    height, width = first.shape[:2]
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    writer = cv2.VideoWriter(
        str(OUTPUT), cv2.VideoWriter_fourcc(*"mp4v"), FPS, (width, height))
    if not writer.isOpened():
        raise SystemExit("Could not open MP4 writer")

    for index, path in enumerate(paths):
        frame = cv2.imread(str(path))
        speed = "1.0X" if index < NATIVE_FRAMES else "0.5X"
        source_frame = index if index < NATIVE_FRAMES else (index - NATIVE_FRAMES) // 2
        cv2.putText(frame, "FIRE GOD HEAVENLY DEMON", (24, 48),
                    cv2.FONT_HERSHEY_SIMPLEX, .64, (230, 205, 255), 2, cv2.LINE_AA)
        cv2.putText(frame, f"CRIMSON GALE - PROCEDURAL FIRE  {speed}", (24, 80),
                    cv2.FONT_HERSHEY_SIMPLEX, .5, (255, 110, 235), 1, cv2.LINE_AA)
        cv2.putText(frame, f"SOURCE FRAME {min(source_frame + 1, 96):02}/96", (24, height - 28),
                    cv2.FONT_HERSHEY_SIMPLEX, .46, (245, 235, 255), 1, cv2.LINE_AA)
        writer.write(frame)

    writer.release()
    print(OUTPUT)


if __name__ == "__main__":
    main()
