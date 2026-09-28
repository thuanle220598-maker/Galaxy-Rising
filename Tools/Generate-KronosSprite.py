"""Generate Kronos's 64x64 key pose, combat sheets, and void VFX."""

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "Assets" / "Art" / "Characters" / "Kronos" / "Kronos.png"
DEFAULT_PREVIEW = ROOT / "Previews" / "Kronos_x8.png"
DEFAULT_COMPARISON = ROOT / "Previews" / "Kronos_Comparison_x8.png"
COMBAT_DIR = ROOT / "Assets" / "Art" / "Characters" / "Kronos" / "Combat"
VFX_DIR = ROOT / "Assets" / "Resources" / "VFX" / "Kronos"

TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (5, 3, 13, 255)
VOID_0 = (8, 9, 21, 255)
VOID_1 = (25, 23, 52, 255)
VOID_2 = (45, 31, 76, 255)
VOID_3 = (73, 41, 99, 255)
METAL_0 = (30, 28, 39, 255)
METAL_1 = (67, 62, 82, 255)
METAL_2 = (111, 101, 126, 255)
METAL_3 = (161, 147, 174, 255)
NEBULA_0 = (59, 23, 79, 255)
NEBULA_1 = (110, 36, 111, 255)
NEBULA_2 = (164, 50, 120, 255)
NEBULA_3 = (208, 74, 139, 255)
GLOW_0 = (73, 31, 184, 255)
GLOW_1 = (113, 48, 232, 255)
GLOW_2 = (152, 82, 255, 255)
GLOW_3 = (214, 154, 255, 255)
WHITE = (247, 230, 255, 255)

MAGIC_CENTER = (53, 18)


def outlined(body):
    alpha = body.getchannel("A")
    source = alpha.load()
    result = Image.new("RGBA", body.size, TRANSPARENT)
    pixels = result.load()
    for y in range(body.height):
        for x in range(body.width):
            if source[x, y]:
                continue
            if any(
                0 <= nx < body.width and 0 <= ny < body.height and source[nx, ny]
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            ):
                pixels[x, y] = OUTLINE
    result.alpha_composite(body)
    return result


def draw_core(draw, center, radius, bright=False):
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=VOID_0)
    draw.arc((x - radius, y - radius, x + radius, y + radius), 205, 515, fill=GLOW_0, width=1)
    draw.arc((x - radius + 1, y - radius + 1, x + radius - 1, y + radius - 1), 20, 250,
             fill=GLOW_2 if bright else NEBULA_2, width=1)
    draw.point((x + 1, y - 1), fill=GLOW_3 if bright else GLOW_1)


def draw_body():
    body = Image.new("RGBA", (64, 64), TRANSPARENT)
    draw = ImageDraw.Draw(body)

    # Nebula mantle replaces the roster's long hair/cape signature.
    draw.polygon(
        ((18, 11), (10, 14), (3, 21), (11, 23), (4, 30), (13, 31),
         (5, 40), (14, 39), (8, 50), (20, 43), (25, 24)),
        fill=VOID_1,
    )
    draw.polygon(((13, 14), (5, 20), (15, 22), (7, 29), (18, 28), (11, 38), (22, 34)), fill=NEBULA_0)
    draw.polygon(((10, 18), (5, 21), (13, 22), (8, 27), (17, 25)), fill=NEBULA_2)
    draw.polygon(((9, 34), (5, 40), (14, 38), (9, 47), (18, 41)), fill=NEBULA_1)

    # Rear leg and trailing gravity shroud.
    draw.polygon(((20, 37), (27, 37), (27, 56), (24, 61), (17, 61), (19, 54)), fill=VOID_1)
    draw.polygon(((18, 39), (23, 39), (22, 57), (18, 60), (15, 55)), fill=VOID_2)
    draw.polygon(((17, 59), (25, 58), (27, 61), (16, 61)), fill=METAL_0)

    # Front leg carries more light to establish the 3/4 right-facing pose.
    draw.polygon(((31, 37), (40, 38), (43, 56), (39, 61), (30, 61), (31, 53)), fill=VOID_2)
    draw.polygon(((36, 39), (41, 41), (41, 56), (37, 59), (34, 54)), fill=VOID_3)
    draw.polygon(((31, 59), (41, 58), (44, 61), (30, 61)), fill=METAL_0)
    draw.line((38, 59, 42, 60), fill=METAL_2, width=1)

    # Massive hunched torso.
    draw.polygon(
        ((19, 14), (26, 10), (38, 10), (46, 15), (45, 28),
         (40, 41), (28, 43), (19, 35), (16, 23)),
        fill=VOID_1,
    )
    draw.polygon(((29, 11), (39, 12), (45, 17), (42, 29), (37, 37), (31, 38)), fill=VOID_2)
    draw.polygon(((36, 13), (42, 16), (42, 27), (38, 31), (36, 24)), fill=VOID_3)
    draw.polygon(((18, 24), (23, 17), (27, 18), (25, 38), (20, 35)), fill=METAL_0)
    draw.line((38, 16, 42, 20), fill=METAL_2, width=2)
    draw.line((37, 34, 31, 38), fill=NEBULA_2, width=1)
    draw.line((27, 14, 27, 34), fill=VOID_3, width=1)
    draw.line((28, 38, 38, 38), fill=NEBULA_1, width=1)

    # Back shoulder and long lowered claw.
    draw.ellipse((13, 11, 27, 25), fill=METAL_0)
    draw.polygon(((17, 19), (23, 21), (18, 38), (12, 47), (8, 44), (12, 34)), fill=VOID_2)
    draw.polygon(((11, 36), (17, 37), (13, 48), (9, 51), (6, 48), (9, 44)), fill=METAL_1)
    draw.line((17, 22, 14, 35), fill=METAL_2, width=1)
    draw.line((13, 38, 10, 47), fill=METAL_3, width=1)
    draw.line((9, 49, 5, 55), fill=METAL_2, width=2)
    draw.line((11, 50, 9, 57), fill=METAL_3, width=1)
    draw.line((13, 49, 13, 56), fill=METAL_2, width=1)

    # Faceless meteor helm and asymmetric horns.
    draw.polygon(((22, 8), (25, 2), (31, 0), (37, 3), (42, 1), (41, 8), (38, 13), (27, 13)), fill=METAL_0)
    draw.polygon(((24, 5), (28, 1), (30, 7)), fill=METAL_2)
    draw.polygon(((36, 4), (43, 1), (40, 9)), fill=METAL_1)
    draw.polygon(((28, 3), (36, 4), (39, 8), (36, 12), (28, 11), (25, 8)), fill=METAL_1)
    draw.polygon(((33, 4), (38, 6), (37, 10), (34, 12)), fill=METAL_2)
    draw.line((27, 11, 36, 12), fill=METAL_3, width=1)
    draw.line((26, 8, 29, 4), fill=METAL_2, width=1)

    # Front shoulder and raised elongated arm.
    draw.ellipse((37, 10, 51, 24), fill=METAL_1)
    draw.polygon(((43, 18), (49, 20), (52, 29), (48, 36), (43, 32), (45, 27)), fill=VOID_2)
    draw.polygon(((47, 19), (52, 22), (54, 29), (50, 32), (47, 28)), fill=VOID_3)
    draw.polygon(((46, 28), (54, 28), (58, 24), (60, 26), (55, 34), (49, 37), (44, 33)), fill=METAL_1)
    draw.line((43, 19, 48, 21), fill=METAL_3, width=1)
    draw.line((48, 22, 50, 28), fill=METAL_2, width=1)
    draw.line((46, 32, 52, 34), fill=METAL_3, width=1)
    draw.line((55, 28, 61, 24), fill=METAL_3, width=1)
    draw.line((55, 31, 61, 29), fill=METAL_2, width=1)
    draw.line((53, 34, 60, 35), fill=METAL_2, width=1)

    # Upper-right key light and occlusion accents.
    draw.line((35, 4, 38, 6), fill=METAL_3, width=1)
    draw.line((43, 13, 48, 16), fill=METAL_3, width=1)
    draw.line((49, 21, 52, 26), fill=METAL_2, width=1)
    draw.line((37, 39, 39, 52), fill=NEBULA_2, width=1)
    draw.line((22, 42, 20, 55), fill=VOID_0, width=2)
    draw.line((25, 42, 25, 56), fill=METAL_1, width=1)
    draw.line((33, 42, 34, 55), fill=NEBULA_3, width=1)
    draw.line((18, 58, 24, 58), fill=METAL_2, width=1)
    draw.line((34, 58, 40, 58), fill=METAL_3, width=1)

    # Vertical void eye: the only facial feature.
    draw.line((33, 5, 33, 10), fill=GLOW_1, width=2)
    draw.point((34, 6), fill=GLOW_3)
    draw.point((33, 5), fill=GLOW_2)

    # Three black-hole cores.
    draw_core(draw, (21, 17), 3)
    draw_core(draw, (44, 16), 3)
    draw_core(draw, (34, 23), 4, bright=True)

    # Sparse nebula/star texture contained inside the body.
    stars = (
        (23, 27, NEBULA_2), (29, 16, GLOW_1), (38, 29, NEBULA_3),
        (27, 34, GLOW_2), (34, 32, NEBULA_1), (38, 20, GLOW_1),
        (21, 31, NEBULA_3), (35, 37, GLOW_2), (25, 21, NEBULA_1),
    )
    for x, y, color in stars:
        if body.getpixel((x, y))[3]:
            draw.point((x, y), fill=color)

    return outlined(body)


def draw_magic(sprite):
    magic = Image.new("RGBA", sprite.size, TRANSPARENT)
    draw = ImageDraw.Draw(magic)
    x, y = MAGIC_CENTER

    # Wrist fragments and their implied orbits.
    for box, color in (
        ((45, 23, 48, 26), METAL_2), ((54, 26, 57, 29), METAL_1),
        ((48, 35, 51, 38), METAL_2), ((7, 42, 10, 45), METAL_1),
        ((12, 50, 15, 53), METAL_2),
    ):
        draw.rectangle(box, fill=color)
    draw.arc((43, 20, 59, 38), 205, 500, fill=NEBULA_2, width=1)
    draw.arc((4, 38, 18, 54), 80, 300, fill=GLOW_0, width=1)

    # Hand-held singularity: brightest point of the sprite.
    draw.arc((x - 8, y - 5, x + 8, y + 5), 190, 510, fill=NEBULA_2, width=2)
    draw.arc((x - 6, y - 7, x + 6, y + 7), 20, 280, fill=GLOW_2, width=2)
    draw.ellipse((x - 4, y - 4, x + 4, y + 4), fill=GLOW_0)
    draw.ellipse((x - 3, y - 3, x + 3, y + 3), fill=VOID_0)
    draw.point((x + 4, y - 3), fill=WHITE)
    draw.point((x + 5, y - 1), fill=GLOW_3)
    draw.point((x - 5, y + 2), fill=GLOW_2)

    for px, py, color in (
        (61, 14, GLOW_2), (59, 10, NEBULA_3), (61, 21, GLOW_1),
        (48, 8, NEBULA_2), (57, 5, GLOW_0),
    ):
        draw.point((px, py), fill=color)

    sprite.alpha_composite(magic)


def generate_sprite():
    sprite = draw_body()
    draw_magic(sprite)
    return sprite


def shifted(image, x=0, y=0):
    result = Image.new("RGBA", image.size, TRANSPARENT)
    result.alpha_composite(image, (x, y))
    return result


def ring(draw, center, radius, color, width=1):
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), outline=color, width=width)


def character_frames(base, state, count):
    frames = []
    for index in range(count):
        progress = index / max(1, count - 1)
        wave = math.sin(progress * math.tau)
        if state == "Idle":
            frame = shifted(base, 0, -1 if wave > 0.45 else 0)
            draw = ImageDraw.Draw(frame)
            ring(draw, (34, 23), 5 + (index % 3 == 1), GLOW_1)
            draw.point((34 + index % 2, 22), fill=GLOW_3)
        elif state == "Run":
            frame = shifted(base, 0, -1 if index % 2 else 0)
            draw = ImageDraw.Draw(frame)
            trail = 3 + index % 4
            draw.line((17, 59, 17 - trail, 61), fill=NEBULA_2, width=2)
            draw.line((38, 59, 38 - trail, 62), fill=GLOW_0)
        elif state == "Basic":
            frame = shifted(base, 0, 0)
            draw = ImageDraw.Draw(frame)
            reach = (0, 2, 5, 8, 11, 7, 3, 0)[index]
            draw.line((47, 33, min(63, 49 + reach), 28 - index % 2), fill=NEBULA_3, width=3)
            draw.line((49, 32, min(63, 51 + reach), 27 - index % 2), fill=GLOW_2)
            if index >= 3:
                draw.line((54, 29, 62, 21), fill=GLOW_3, width=2)
                draw.point((63, 20), fill=WHITE)
        elif state == "Active":
            frame = shifted(base, 0, 1 if index < 4 else 0)
            draw = ImageDraw.Draw(frame)
            radius = min(28, 4 + index * 3)
            ring(draw, (32, 53), radius, GLOW_0, 2)
            ring(draw, (32, 53), max(2, radius - 5), NEBULA_2)
            if index >= 4:
                draw.line((14, 55, 50, 55), fill=GLOW_2, width=2)
                draw.line((22, 59, 42, 44), fill=NEBULA_3)
        elif state == "Ultimate":
            lift = round(math.sin(progress * math.pi) * 4)
            frame = shifted(base, 0, -lift)
            draw = ImageDraw.Draw(frame)
            radius = min(30, 7 + index * 2)
            ring(draw, (32, 31 - lift), radius, NEBULA_2, 2)
            ring(draw, (32, 31 - lift), max(3, radius - 7), GLOW_1)
            for horn in (-1, 1):
                draw.line((32 + horn * 8, 7 - lift, 32 + horn * (10 + index // 2),
                           max(0, 1 - lift)), fill=METAL_3, width=2)
            if index >= count // 2:
                draw.line((4, 36, 60, 24), fill=GLOW_2)
                draw.line((7, 44, 58, 13), fill=NEBULA_3)
        elif state == "Hit":
            frame = shifted(base, -2 if index in (1, 2) else 0, 0)
            if index in (1, 2, 3):
                draw = ImageDraw.Draw(frame)
                draw.line((49, 14, 60, 6), fill=WHITE, width=2)
                draw.line((51, 21, 63, 20), fill=GLOW_2, width=2)
        else:
            angle = 78 * progress
            collapsed = base.rotate(angle, resample=Image.Resampling.NEAREST, expand=False)
            frame = shifted(collapsed, round(progress * 5), round(progress * 16))
            draw = ImageDraw.Draw(frame)
            radius = max(2, round((1 - progress) * 13))
            ring(draw, (37, 51), radius, GLOW_0)
        frames.append(frame)
    return frames


def save_sheet(path, frames):
    path.parent.mkdir(parents=True, exist_ok=True)
    size = frames[0].size[0]
    sheet = Image.new("RGBA", (size * len(frames), size), TRANSPARENT)
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (index * size, 0))
    sheet.save(path)


def vfx_frame(name, size, index, count):
    image = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(image)
    cx = cy = size // 2
    p = index / max(1, count - 1)
    pulse = 0.5 + 0.5 * math.sin(index / count * math.tau)

    if name == "KronosDimensionalTear":
        half = max(2, round(p * (size // 2 - 8)))
        points = [(cx + round(math.sin(y * 0.31 + index) * 4), cy - half + y)
                  for y in range(0, half * 2 + 1, 3)]
        if len(points) > 1:
            draw.line(points, fill=GLOW_3, width=3)
            draw.line([(x - 3, y) for x, y in points], fill=NEBULA_2)
        for ray in range(6):
            angle = ray * math.tau / 6 + index * 0.17
            draw.line((cx, cy, cx + math.cos(angle) * half, cy + math.sin(angle) * half), fill=GLOW_0)
    elif name == "KronosVoidField":
        radius = size // 2 - 7
        ring(draw, (cx, cy), radius, NEBULA_1, 3)
        ring(draw, (cx, cy), radius - 10, GLOW_0, 2)
        for star in range(18):
            angle = star * math.tau / 18 + index * 0.08
            r = 12 + (star * 11) % max(13, radius - 12)
            draw.point((round(cx + math.cos(angle) * r), round(cy + math.sin(angle) * r * 0.55)),
                       fill=GLOW_2 if star % 4 == 0 else NEBULA_3)
    elif name == "KronosMassOrbit":
        radius = size // 2 - 10
        for rock in range(10):
            angle = rock * math.tau / 10 + index * 0.2
            x = round(cx + math.cos(angle) * radius)
            y = round(cy + math.sin(angle) * radius * 0.48)
            color = GLOW_2 if index >= count - 2 else METAL_2
            draw.rectangle((x - 2, y - 2, x + 2, y + 2), fill=color)
    elif name == "KronosResonanceBurst":
        radius = max(3, round(p * (size // 2 - 5)))
        ring(draw, (cx, cy), radius, GLOW_2, 3)
        for ray in range(12):
            angle = ray * math.tau / 12 + index * 0.11
            draw.line((cx + math.cos(angle) * radius * 0.35, cy + math.sin(angle) * radius * 0.35,
                       cx + math.cos(angle) * radius, cy + math.sin(angle) * radius), fill=NEBULA_3, width=2)
    elif name == "KronosParasite":
        for bug in range(4):
            angle = bug * math.tau / 4 + index * 0.28
            x = round(cx + math.cos(angle) * (7 + pulse * 3))
            y = round(cy + math.sin(angle) * (5 + pulse * 2))
            draw.ellipse((x - 2, y - 1, x + 2, y + 1), fill=NEBULA_3)
            draw.point((x, y), fill=GLOW_3)
    elif name == "KronosParasiteDrain":
        points = []
        for step in range(9):
            x = 4 + step * (size - 8) / 8
            y = cy + math.sin(step * 1.1 + index * 0.75) * 5
            points.append((round(x), round(y)))
        draw.line(points, fill=NEBULA_2, width=4)
        draw.line(points, fill=GLOW_1, width=2)
        draw.ellipse((size - 9, points[-1][1] - 3, size - 3, points[-1][1] + 3), fill=GLOW_3)
    elif name == "KronosSingularity":
        radius = 8 + round(p * (size // 2 - 14))
        draw.ellipse((cx - radius // 3, cy - radius // 3, cx + radius // 3, cy + radius // 3), fill=VOID_0)
        for arm in range(4):
            box = (cx - radius, cy - radius // 2, cx + radius, cy + radius // 2)
            draw.arc(box, arm * 70 + index * 12, arm * 70 + 145 + index * 12,
                     fill=GLOW_1 if arm % 2 else NEBULA_3, width=3)
        draw.point((cx + radius // 3, cy - radius // 4), fill=WHITE)
    elif name == "KronosLeviathanAwaken":
        radius = 10 + round(p * (size // 2 - 14))
        ring(draw, (cx, cy), radius, NEBULA_2, 4)
        for chain in range(5):
            angle = chain * math.tau / 5 + index * 0.09
            x = round(cx + math.cos(angle) * radius)
            y = round(cy + math.sin(angle) * radius)
            draw.line((cx, cy, x, y), fill=METAL_2, width=2)
            draw.rectangle((x - 2, y - 2, x + 2, y + 2), fill=GLOW_2)
        draw.ellipse((cx - 8, cy - 8, cx + 8, cy + 8), fill=VOID_0)
    elif name == "KronosLeviathanAura":
        radius = size // 2 - 8 + round(pulse * 3)
        ring(draw, (cx, cy), radius, NEBULA_2, 3)
        ring(draw, (cx, cy), radius - 8, GLOW_0)
        for star in range(8):
            angle = star * math.tau / 8 - index * 0.16
            x = round(cx + math.cos(angle) * radius)
            y = round(cy + math.sin(angle) * radius)
            draw.polygon(((x, y - 3), (x + 2, y), (x, y + 3), (x - 2, y)), fill=GLOW_2)
    elif name == "KronosVoidSlash":
        width = round(p * (size - 12))
        draw.arc((6, cy - 22, 6 + width, cy + 22), 225, 495, fill=GLOW_3, width=4)
        draw.arc((9, cy - 18, 9 + width, cy + 18), 225, 495, fill=NEBULA_2, width=2)
    elif name == "KronosCollapse":
        radius = max(3, round((1 - p) * (size // 2 - 8)))
        ring(draw, (cx, cy), radius, GLOW_1, 3)
        draw.ellipse((cx - max(2, radius // 3), cy - max(2, radius // 3),
                      cx + max(2, radius // 3), cy + max(2, radius // 3)), fill=VOID_0)
        for ray in range(8):
            angle = ray * math.tau / 8 + index * 0.15
            draw.line((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius, cx, cy), fill=NEBULA_3)
    elif name == "KronosDecayPulse":
        radius = max(4, round(p * (size // 2 - 5)))
        ring(draw, (cx, cy), radius, NEBULA_3, 3)
        ring(draw, (cx, cy), max(2, radius - 6), GLOW_0)
        for mote in range(10):
            angle = mote * math.tau / 10 + index * 0.2
            draw.point((round(cx + math.cos(angle) * radius), round(cy + math.sin(angle) * radius)), fill=GLOW_3)
    else:
        raise ValueError(name)
    return image


def generate_combat_sheets(base):
    manifest = {
        "KronosIdle": ("Idle", 8),
        "KronosRun": ("Run", 8),
        "KronosBasic": ("Basic", 8),
        "KronosActive": ("Active", 12),
        "KronosUltimate": ("Ultimate", 14),
        "KronosHit": ("Hit", 5),
        "KronosDeath": ("Death", 10),
    }
    for filename, (state, count) in manifest.items():
        save_sheet(COMBAT_DIR / f"{filename}.png", character_frames(base, state, count))


def generate_vfx_sheets():
    manifest = {
        "KronosDimensionalTear": (96, 10),
        "KronosVoidField": (128, 12),
        "KronosMassOrbit": (64, 10),
        "KronosResonanceBurst": (96, 10),
        "KronosParasite": (32, 8),
        "KronosParasiteDrain": (64, 8),
        "KronosSingularity": (128, 12),
        "KronosLeviathanAwaken": (128, 14),
        "KronosLeviathanAura": (96, 12),
        "KronosVoidSlash": (96, 8),
        "KronosCollapse": (96, 10),
        "KronosDecayPulse": (96, 10),
    }
    for name, (size, count) in manifest.items():
        save_sheet(VFX_DIR / f"{name}.png", [vfx_frame(name, size, index, count) for index in range(count)])


def validate(sprite):
    assert sprite.size == (64, 64)
    colors = sprite.getcolors(maxcolors=4096)
    assert colors is not None and len(colors) <= 64, f"expected <=64 colors, got {len(colors or [])}"
    alpha_values = {value for _, value in sprite.getchannel("A").getcolors()}
    assert alpha_values <= {0, 255}, f"unexpected alpha values: {alpha_values}"
    bounds = sprite.getchannel("A").getbbox()
    assert bounds is not None and bounds[0] >= 1 and bounds[1] <= 1 and bounds[2] <= 63 and bounds[3] == 63, \
        f"unexpected bounds: {bounds}"

    opaque = [
        (sum(sprite.getpixel((x, y))[:3]), x, y)
        for y in range(64)
        for x in range(64)
        if sprite.getpixel((x, y))[3]
    ]
    brightest = max(value for value, _, _ in opaque)
    assert any(
        value == brightest and math.hypot(x - MAGIC_CENTER[0], y - MAGIC_CENTER[1]) <= 7
        for value, x, y in opaque
    ), "brightest pixel must belong to the held singularity"


def save_preview(sprite, path):
    preview = Image.new("RGBA", (64, 64), (70, 90, 110, 255))
    preview.alpha_composite(sprite)
    path.parent.mkdir(parents=True, exist_ok=True)
    preview.resize((512, 512), Image.Resampling.NEAREST).save(path)


def save_comparison(sprite, path):
    aurelia_path = ROOT / "Assets" / "Art" / "Characters" / "Aurelia" / "Aurelia.png"
    fire_path = ROOT / "Assets" / "Art" / "Characters" / "Nova" / "Combat" / "FireGodIdle.png"
    board = Image.new("RGBA", (216, 72), (18, 25, 42, 255))

    aurelia = Image.open(aurelia_path).convert("RGBA")
    fire_sheet = Image.open(fire_path).convert("RGBA")
    fire = fire_sheet.crop((0, 0, 68, 68)).resize((64, 64), Image.Resampling.NEAREST)
    for index, character in enumerate((fire, aurelia, sprite)):
        board.alpha_composite(character, (4 + index * 72, 4))

    path.parent.mkdir(parents=True, exist_ok=True)
    board.resize((864, 288), Image.Resampling.NEAREST).save(path)


def main():
    output = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_OUTPUT
    sprite = generate_sprite()
    validate(sprite)

    output.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(output)
    generate_combat_sheets(sprite)
    generate_vfx_sheets()
    save_preview(sprite, DEFAULT_PREVIEW)
    save_comparison(sprite, DEFAULT_COMPARISON)
    print(f"wrote {output}, 7 combat sheets, 12 VFX sheets, and review previews")


if __name__ == "__main__":
    main()
