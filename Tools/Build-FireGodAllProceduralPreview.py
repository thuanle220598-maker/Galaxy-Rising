from pathlib import Path

import cv2


ROOT = Path(__file__).resolve().parents[1]
FRAMES = ROOT / "Library/FireGodAllProceduralPreviewFrames"
OUTPUT = ROOT / "Previews/FireGodAllProceduralVfx.mp4"
FPS = 60
SECTION_FRAMES = 144
SECTIONS = (
    "BASIC / PROJECTILE / ASH",
    "HELLFIRE / MAGMA",
    "ULTIMATE / SCORCH / FIRESTORM",
    "SHIELD / HEAT / EMBERS",
)


def main():
    paths = [FRAMES / f"frame_{index:03}.png"
             for index in range(SECTION_FRAMES * len(SECTIONS))]
    missing = [path for path in paths if not path.exists()]
    if missing:
        raise SystemExit(f"Missing preview frame: {missing[0]}")

    first = cv2.imread(str(paths[0]))
    if first is None:
        raise SystemExit(f"Could not read preview frame: {paths[0]}")
    height, width = first.shape[:2]
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    writer = cv2.VideoWriter(
        str(OUTPUT), cv2.VideoWriter_fourcc(*"mp4v"), FPS, (width, height))
    if not writer.isOpened():
        raise SystemExit("Could not open MP4 writer")

    for index, path in enumerate(paths):
        frame = cv2.imread(str(path))
        if frame is None or frame.shape[:2] != (height, width):
            raise SystemExit(f"Invalid preview frame: {path}")
        section = SECTIONS[index // SECTION_FRAMES]
        cv2.putText(frame, "FIRE GOD HEAVENLY DEMON", (24, 48),
                    cv2.FONT_HERSHEY_SIMPLEX, .64, (230, 205, 255), 2, cv2.LINE_AA)
        cv2.putText(frame, section, (24, 80),
                    cv2.FONT_HERSHEY_SIMPLEX, .5, (255, 110, 235), 1, cv2.LINE_AA)
        cv2.putText(frame, "60 FPS UNITY PROCEDURAL RENDER", (24, height - 28),
                    cv2.FONT_HERSHEY_SIMPLEX, .46, (245, 235, 255), 1, cv2.LINE_AA)
        writer.write(frame)

    writer.release()
    capture = cv2.VideoCapture(str(OUTPUT))
    assert int(capture.get(cv2.CAP_PROP_FRAME_COUNT)) == 576
    assert capture.get(cv2.CAP_PROP_FPS) == 60
    assert int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)) == 540
    assert int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)) == 960
    capture.release()
    print(OUTPUT)


if __name__ == "__main__":
    main()
