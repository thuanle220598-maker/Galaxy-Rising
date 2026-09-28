"""Generate Aurelia's approved key pose, combat sheets, and water VFX.

Usage:
    python3 Tools/Generate-AureliaSprite.py \
        Assets/Art/Characters/Aurelia/Aurelia.png \
        --preview Previews/Aurelia_x8.png
"""

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
REFERENCE = ROOT / "Previews" / "sprite-fusion-76d4805b-391e-4bc4-a82a-d4cdd624c77a.png"
COMBAT_DIR = ROOT / "Assets" / "Art" / "Characters" / "Aurelia" / "Combat"
VFX_DIR = ROOT / "Assets" / "Resources" / "VFX" / "Aurelia"
ORB_32 = (23, 14)

NAVY = (9, 20, 52, 255)
BLUE = (35, 100, 174, 255)
CYAN = (62, 190, 218, 255)
JADE = (86, 228, 200, 255)
WHITE = (238, 255, 255, 255)
GOLD = (230, 182, 70, 255)


def quantize_reference():
    source = Image.open(REFERENCE).convert("RGBA")
    assert source.size == (32, 32), f"expected 32x32 reference, got {source.size}"

    alpha = source.getchannel("A")
    rgb = Image.new("RGB", source.size)
    rgb.paste(source.convert("RGB"), mask=alpha)
    indexed = rgb.quantize(colors=48, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    result = indexed.convert("RGBA")
    result.putalpha(alpha)
    return result


def add_aurelia_details(sprite):
    draw = ImageDraw.Draw(sprite)
    for point, color in (
        ((14, 7), (86, 190, 214, 255)),
        ((13, 10), (72, 168, 202, 255)),
        ((12, 14), (54, 146, 190, 255)),
        ((11, 18), (42, 126, 178, 255)),
    ):
        if sprite.getpixel(point)[3]:
            draw.point(point, fill=color)

    draw.point((20, 9), fill=(76, 194, 208, 255))
    draw.point((20, 10), fill=(42, 146, 176, 255))
    for point in ((17, 19), (18, 21), (17, 23), (16, 25), (19, 24), (20, 26)):
        if sprite.getpixel(point)[3]:
            draw.point(point, fill=(82, 200, 224, 255))

    draw.line([(22, 14), (24, 12), (25, 10), (24, 8)], fill=(22, 104, 156, 255))
    draw.line([(23, 14), (25, 12), (25, 10)], fill=(88, 210, 232, 255))
    draw.ellipse((21, 12, 24, 15), fill=(18, 116, 122, 255))
    draw.rectangle((22, 13, 23, 14), fill=(92, 234, 204, 255))
    draw.point((23, 13), fill=WHITE)
    for point, color in (
        ((26, 9), (94, 214, 232, 255)),
        ((27, 13), (62, 176, 214, 255)),
        ((25, 17), (74, 202, 220, 255)),
    ):
        draw.point(point, fill=color)


def scale2x(source):
    target = Image.new("RGBA", (source.width * 2, source.height * 2))
    source_px = source.load()
    target_px = target.load()

    def pixel(x, y):
        return source_px[max(0, min(source.width - 1, x)), max(0, min(source.height - 1, y))]

    for y in range(source.height):
        for x in range(source.width):
            center = pixel(x, y)
            up, left, right, down = pixel(x, y - 1), pixel(x - 1, y), pixel(x + 1, y), pixel(x, y + 1)
            target_px[x * 2, y * 2] = left if left == up and left != down and up != right else center
            target_px[x * 2 + 1, y * 2] = right if up == right and up != left and right != down else center
            target_px[x * 2, y * 2 + 1] = left if left == down and left != up and down != right else center
            target_px[x * 2 + 1, y * 2 + 1] = right if down == right and left != down and up != right else center
    return target


def shifted(image, x=0, y=0):
    result = Image.new("RGBA", image.size)
    result.alpha_composite(image, (x, y))
    return result


def draw_magic(frame, center, radius, phase=0):
    draw = ImageDraw.Draw(frame)
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=CYAN)
    inner = max(1, radius - 2)
    draw.ellipse((x - inner, y - inner, x + inner, y + inner), fill=JADE)
    draw.point((x + phase % 2, y - 1), fill=WHITE)


def character_frames(base, state, count):
    frames = []
    for index in range(count):
        progress = index / max(1, count - 1)
        wave = math.sin(progress * math.tau)
        if state == "Idle":
            frame = shifted(base, 0, -1 if wave > 0.35 else 0)
            draw_magic(frame, (46, 28), 3 + (1 if index in (1, 4) else 0), index)
        elif state == "Run":
            frame = shifted(base, 1 if index % 4 in (1, 2) else 0, -1 if index % 2 else 0)
            draw = ImageDraw.Draw(frame)
            foot_y = 60 - (index % 2) * 2
            draw.line((17, foot_y, 8, foot_y + 1), fill=BLUE, width=2)
            draw.line((22, foot_y + 2, 12, foot_y + 3), fill=CYAN)
        elif state == "Basic":
            lunge = (0, 1, 2, 2, 1, 0)[index]
            frame = shifted(base, lunge, 0)
            draw = ImageDraw.Draw(frame)
            length = max(0, index - 1) * 4
            if length:
                draw.line((47, 28, 47 + length, 27 - index % 2), fill=CYAN, width=2)
                draw.point((min(63, 49 + length), 27), fill=WHITE)
        elif state == "Active":
            frame = shifted(base, 0, -min(2, index // 3))
            draw = ImageDraw.Draw(frame)
            radius = min(20, index * 3)
            if radius > 2:
                draw.arc((32 - radius, 48 - radius // 3, 32 + radius, 48 + radius // 3), 190, 350, fill=JADE, width=2)
            column_height = max(0, (index - 3) * 7)
            if column_height:
                draw.line((32, 60, 32, max(4, 60 - column_height)), fill=CYAN, width=3)
                draw.line((34, 60, 34, max(8, 60 - column_height)), fill=WHITE)
        elif state == "Ultimate":
            lift = round(math.sin(progress * math.pi) * 6)
            frame = shifted(base, 0, -lift)
            draw = ImageDraw.Draw(frame)
            radius = 8 + index * 2
            draw.arc((32 - radius, 36 - radius, 32 + radius, 36 + radius), 205, 520, fill=CYAN, width=2)
            if index >= count // 2:
                draw.line((32, 58, 32, 4), fill=JADE, width=2)
                draw.point((32, 3), fill=WHITE)
        elif state == "Hit":
            frame = shifted(base, -2 if index % 2 == 0 else 1, 0)
            if index in (1, 2):
                draw = ImageDraw.Draw(frame)
                draw.line((51, 18, 58, 12), fill=WHITE, width=2)
                draw.line((52, 22, 61, 22), fill=CYAN, width=2)
        else:
            angle = -82 * progress
            rotated = base.rotate(angle, resample=Image.Resampling.NEAREST, expand=False)
            frame = shifted(rotated, round(progress * 8), round(progress * 17))
        frames.append(frame)
    return frames


def save_sheet(path, frames):
    path.parent.mkdir(parents=True, exist_ok=True)
    width, height = frames[0].size
    sheet = Image.new("RGBA", (width * len(frames), height))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index * width, 0))
    sheet.save(path)


def ring(draw, center, radius, color, width=1):
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), outline=color, width=width)


def vfx_frame(name, size, index, count):
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    cx = cy = size // 2
    p = index / max(1, count - 1)
    pulse = 0.5 + 0.5 * math.sin(index / count * math.tau)

    if name == "AureliaWaterSerpent":
        points = []
        for step in range(8):
            x = 4 + step * (size - 8) / 7
            y = cy + math.sin(step * 1.25 + index * 0.8) * 4
            points.append((round(x), round(y)))
        draw.line(points, fill=BLUE, width=4)
        draw.line(points, fill=CYAN, width=2)
        draw.ellipse((size - 8, points[-1][1] - 3, size - 3, points[-1][1] + 2), fill=JADE)
        draw.point((size - 4, points[-1][1] - 1), fill=WHITE)
    elif name == "AureliaHydroBead":
        radius = 3 + (index % 3 == 1)
        ring(draw, (cx, cy), radius + 2, BLUE)
        draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=JADE)
        draw.point((cx + 1, cy - 2), fill=WHITE)
    elif name == "AureliaCleansingRing":
        radius = max(3, round(p * (size // 2 - 3)))
        ring(draw, (cx, cy), radius, JADE, 2)
        for dot in range(6):
            angle = dot * math.tau / 6 + index * 0.35
            x = round(cx + math.cos(angle) * radius)
            y = round(cy + math.sin(angle) * radius)
            draw.ellipse((x - 1, y - 1, x + 1, y + 1), fill=WHITE)
    elif name == "AureliaDragonAura":
        radius = size // 2 - 6
        ring(draw, (cx, cy), radius, BLUE, 2)
        for dragon in (0, math.pi):
            points = []
            for step in range(7):
                angle = dragon + step * 0.18 + index * 0.12
                r = radius - step * 2
                points.append((round(cx + math.cos(angle) * r), round(cy + math.sin(angle) * r)))
            draw.line(points, fill=CYAN, width=2)
    elif name == "AureliaDragonPressure":
        radius = max(5, round(p * (size // 2 - 5)))
        ring(draw, (cx, cy), radius, CYAN, 3)
        draw.arc((cx - radius // 2, cy - radius, cx + radius // 2, cy + radius), 190, 520, fill=JADE, width=3)
        draw.ellipse((cx + radius // 3 - 2, cy - radius + 3, cx + radius // 3 + 4, cy - radius + 8), fill=WHITE)
    elif name == "AureliaDomain":
        radius = size // 2 - 6
        ring(draw, (cx, cy), radius, BLUE, 3)
        ring(draw, (cx, cy), radius - 8, JADE, 1)
        for branch in range(8):
            angle = branch * math.tau / 8 + index * 0.035
            end = (round(cx + math.cos(angle) * radius), round(cy + math.sin(angle) * radius))
            draw.line((cx, cy, *end), fill=BLUE)
        for dragon in (0, math.pi):
            points = []
            for step in range(12):
                angle = dragon + index * 0.1 + step * 0.12
                r = 18 + step * 2
                points.append((round(cx + math.cos(angle) * r), round(cy + math.sin(angle) * r * 0.62)))
            draw.line(points, fill=CYAN, width=3)
            draw.ellipse((points[-1][0] - 2, points[-1][1] - 2, points[-1][0] + 3, points[-1][1] + 3), fill=WHITE)
    elif name == "AureliaShieldLoop":
        radius = size // 2 - 8 + round(pulse * 2)
        ring(draw, (cx, cy), radius, CYAN, 2)
        for scale in range(8):
            angle = scale * math.tau / 8 + index * 0.25
            x = round(cx + math.cos(angle) * radius)
            y = round(cy + math.sin(angle) * radius)
            draw.polygon(((x, y - 2), (x + 2, y), (x, y + 3), (x - 2, y)), fill=JADE)
    elif name == "AureliaShieldBreak":
        radius = max(5, round(p * (size // 2 - 5)))
        for ray in range(12):
            angle = ray * math.tau / 12 + index * 0.1
            start = 4 + radius // 2
            draw.line((cx + math.cos(angle) * start, cy + math.sin(angle) * start,
                       cx + math.cos(angle) * radius, cy + math.sin(angle) * radius), fill=CYAN, width=2)
    elif name == "AureliaUltimateDragon":
        radius = 10 + round(p * (size // 2 - 14))
        points = []
        for step in range(18):
            angle = math.pi * 0.75 + step * 0.22 + index * 0.08
            r = max(5, radius - step * 1.6)
            points.append((round(cx + math.cos(angle) * r), round(cy + math.sin(angle) * r)))
        draw.line(points, fill=BLUE, width=7)
        draw.line(points, fill=CYAN, width=3)
        hx, hy = points[0]
        draw.ellipse((hx - 4, hy - 3, hx + 5, hy + 4), fill=JADE)
        draw.point((hx + 2, hy - 1), fill=WHITE)
    elif name == "AureliaBlessingImpact":
        radius = max(3, round(p * (size // 2 - 4)))
        draw.line((cx, 2, cx, cy), fill=WHITE, width=2)
        ring(draw, (cx, cy), radius, JADE, 2)
        for drop in range(5):
            x = 8 + drop * 12 + (index % 2) * 2
            draw.line((x, 4, x, 10 + index * 2), fill=CYAN, width=2)
    else:
        raise ValueError(name)
    return image


def generate_combat_sheets(base):
    manifest = {
        "AureliaIdle": ("Idle", 6),
        "AureliaRun": ("Run", 8),
        "AureliaBasic": ("Basic", 6),
        "AureliaActive": ("Active", 10),
        "AureliaUltimate": ("Ultimate", 12),
        "AureliaHit": ("Hit", 4),
        "AureliaDeath": ("Death", 8),
    }
    for filename, (state, count) in manifest.items():
        save_sheet(COMBAT_DIR / f"{filename}.png", character_frames(base, state, count))


def generate_vfx_sheets():
    manifest = {
        "AureliaWaterSerpent": (32, 8),
        "AureliaHydroBead": (16, 6),
        "AureliaCleansingRing": (64, 8),
        "AureliaDragonAura": (64, 8),
        "AureliaDragonPressure": (96, 10),
        "AureliaDomain": (128, 12),
        "AureliaShieldLoop": (64, 8),
        "AureliaShieldBreak": (64, 8),
        "AureliaUltimateDragon": (128, 12),
        "AureliaBlessingImpact": (64, 8),
    }
    for name, (size, count) in manifest.items():
        save_sheet(VFX_DIR / f"{name}.png", [vfx_frame(name, size, i, count) for i in range(count)])


def validate(sprite):
    assert sprite.size == (64, 64)
    colors = sprite.getcolors(maxcolors=4096)
    assert colors is not None and len(colors) <= 64, f"expected <=64 colors, got {len(colors or [])}"
    alpha = sprite.getchannel("A")
    assert {value for _, value in alpha.getcolors()} <= {0, 255}, "sprite contains semi-transparent pixels"
    bounds = alpha.getbbox()
    assert bounds is not None and bounds[1] == 0 and bounds[3] >= 62, f"unexpected bounds: {bounds}"

    opaque = [
        (sum(sprite.getpixel((x, y))[:3]), x, y)
        for y in range(64)
        for x in range(64)
        if sprite.getpixel((x, y))[3]
    ]
    brightest = max(opaque)[0]
    orb = (ORB_32[0] * 2, ORB_32[1] * 2)
    assert any(value == brightest and math.hypot(x - orb[0], y - orb[1]) <= 4 for value, x, y in opaque), \
        "brightest pixel must belong to the Long Ngoc"


def main():
    sprite_32 = quantize_reference()
    add_aurelia_details(sprite_32)
    sprite = scale2x(sprite_32)
    validate(sprite)

    output = Path(sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else "Aurelia.png")
    output.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(output)
    generate_combat_sheets(sprite)
    generate_vfx_sheets()

    if "--preview" in sys.argv:
        preview_path = Path(sys.argv[sys.argv.index("--preview") + 1])
        preview = Image.new("RGBA", sprite.size, (70, 90, 110, 255))
        preview.alpha_composite(sprite)
        preview.resize((512, 512), Image.Resampling.NEAREST).save(preview_path)

    print(f"wrote {output}, 7 combat sheets, and 10 VFX sheets")


if __name__ == "__main__":
    main()
