from pathlib import Path
import math

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/Resources/VFX/FireGod"

INK = (2, 1, 5, 255)
DARK = (22, 8, 80, 255)
CRIMSON = (22, 8, 133, 255)
RED = (57, 54, 242, 255)
HOT = (73, 115, 250, 255)
GOLD = (103, 174, 254, 255)
WHITE = (248, 253, 253, 255)
SMOKE = (49, 46, 57, 210)
VIOLET = (190, 35, 205, 235)
INDIGO = (112, 20, 78, 220)
CYAN = (255, 230, 80, 255)
MAGENTA = (220, 28, 248, 245)


def canvas(size):
    return np.zeros((size[1], size[0], 4), dtype=np.uint8)


def circle(frame, center, radius, color, thickness=-1):
    cv2.circle(frame, tuple(map(int, center)), max(1, int(radius)), color, thickness, cv2.LINE_8)


def line(frame, start, end, color, thickness=1):
    cv2.line(frame, tuple(map(int, start)), tuple(map(int, end)), color, thickness, cv2.LINE_8)


def polygon(frame, points, color):
    cv2.fillPoly(frame, [np.asarray(points, dtype=np.int32)], color, cv2.LINE_8)


def polyline(frame, points, color, thickness=1, closed=False):
    cv2.polylines(
        frame,
        [np.asarray(points, dtype=np.int32)],
        closed,
        color,
        thickness,
        cv2.LINE_8,
    )


def with_alpha(color, alpha):
    return color[:3] + (max(1, min(255, round(color[3] * alpha))),)


def ease_out_cubic(value):
    return 1 - (1 - value) ** 3


def seeded_wave(index, seed, amplitude):
    return math.sin(index * 0.61 + seed * 1.73) * amplitude


def polar(center, angle, radius):
    return (
        round(center[0] + math.cos(angle) * radius),
        round(center[1] + math.sin(angle) * radius),
    )


def flame(frame, center, radius, phase=0.0):
    cx, cy = center
    sway = round(math.sin(phase) * 2)
    polygon(frame, [
        (cx - radius, cy + radius),
        (cx - radius // 2, cy - radius),
        (cx + sway, cy - radius - 5),
        (cx + radius // 3, cy - radius + 1),
        (cx + radius, cy + radius),
    ], INK)
    polygon(frame, [
        (cx - radius + 2, cy + radius - 1),
        (cx - radius // 3, cy - radius + 1),
        (cx + sway, cy - radius - 2),
        (cx + radius - 2, cy + radius - 1),
    ], CRIMSON)
    circle(frame, (cx, cy + 1), radius - 3, RED)
    circle(frame, (cx + 1, cy + 1), max(2, radius - 5), HOT)
    circle(frame, (cx + 2, cy + 2), max(1, radius - 7), WHITE)


def flame_projectile():
    frames = []
    for index in range(8):
        frame = canvas((48, 48))
        center = (29, 24)
        wave = round(math.sin(index * math.pi / 4) * 3)
        polygon(frame, [
            (27, 18), (17 + wave, 19), (6, 15 + wave), (13, 24),
            (5, 31 - wave), (18 - wave, 29), (27, 30),
        ], INK)
        polygon(frame, [
            (27, 20), (18 + wave, 21), (10, 18 + wave), (16, 24),
            (10, 28 - wave), (19 - wave, 27), (27, 28),
        ], CRIMSON)
        polygon(frame, [(26, 22), (15 + wave, 24), (26, 26)], HOT)
        circle(frame, center, 8, INK)
        circle(frame, center, 6, RED)
        circle(frame, (31, 23), 4, HOT)
        circle(frame, (33, 22), 2, WHITE)
        for spark in range(2):
            x = 9 + ((index * 7 + spark * 11) % 14)
            y = 13 + ((index * 5 + spark * 9) % 22)
            circle(frame, (x, y), 1, GOLD)
        frames.append(frame)
    return frames


def flame_impact():
    frames = []
    center = (32, 32)
    for index in range(8):
        frame = canvas((64, 64))
        progress = index / 7
        radius = 4 + index * 3
        if index < 5:
            circle(frame, center, radius + 3, INK)
            circle(frame, center, radius, CRIMSON if index < 2 else RED)
            circle(frame, center, max(2, radius - 5), HOT)
            circle(frame, center, max(1, 6 - index), WHITE)
        circle(frame, center, 8 + index * 3, RED if index < 5 else CRIMSON, 2)
        for ray in range(8):
            angle = ray * math.pi / 4 + index * 0.13
            start = polar(center, angle, 8 + progress * 5)
            end = polar(center, angle, 15 + progress * 15)
            line(frame, start, end, HOT if ray % 2 else GOLD, 2 if index < 5 else 1)
        frames.append(frame)
    return frames


def ash_orbit(count):
    frames = []
    center = (16, 16)
    for index in range(8):
        frame = canvas((32, 32))
        circle(frame, center, 6, DARK, 1)
        circle(frame, center, 2, CRIMSON)
        for ember in range(count):
            angle = index * math.pi / 4 + ember * math.pi
            point = polar(center, angle, 8)
            circle(frame, point, 3, INK)
            circle(frame, point, 2, RED)
            tail = polar(point, angle + math.pi, 4)
            line(frame, point, tail, HOT, 1)
        frames.append(frame)
    return frames


def ash_sigil():
    frames = []
    center = (32, 32)
    for index in range(8):
        frame = canvas((64, 64))
        circle(frame, center, 22, INK, 3)
        circle(frame, center, 20, CRIMSON, 1)
        circle(frame, center, 14, RED, 2)
        for rune in range(6):
            angle = index * math.pi / 16 + rune * math.pi / 3
            a = polar(center, angle, 17)
            b = polar(center, angle + 0.28, 20)
            line(frame, a, b, GOLD, 2)
        diamond = [polar(center, math.pi / 4 + step * math.pi / 2, 8) for step in range(4)]
        polygon(frame, diamond, HOT)
        circle(frame, center, 3, WHITE)
        frames.append(frame)
    return frames


def ash_warning():
    frames = []
    center = (32, 32)
    for index in range(6):
        frame = canvas((64, 64))
        pulse = index if index < 3 else 5 - index
        outer = 18 + pulse * 3
        circle(frame, center, outer + 2, INK, 2)
        circle(frame, center, outer, RED, 2)
        circle(frame, center, 8 + pulse, HOT, 2)
        for ray in range(4):
            angle = ray * math.pi / 2 + index * 0.2
            line(frame, polar(center, angle, 11), polar(center, angle, outer - 3), GOLD, 2)
        frames.append(frame)
    return frames


def ash_detonation():
    frames = []
    center = (48, 48)
    for index in range(10):
        frame = canvas((96, 96))
        if index < 2:
            radius = 28 - index * 9
            circle(frame, center, radius, RED, 3)
            circle(frame, center, radius - 5, HOT, 2)
        else:
            progress = (index - 2) / 7
            core = max(3, round(18 * (1 - progress)))
            circle(frame, center, core + 6, INK)
            circle(frame, center, core + 3, RED)
            circle(frame, center, core, WHITE if index < 6 else HOT)
            for ray in range(12):
                angle = ray * math.pi / 6 + index * 0.11
                inner = 8 + progress * 10
                outer = 22 + progress * 30 + (ray % 3) * 4
                line(frame, polar(center, angle, inner), polar(center, angle, outer),
                     GOLD if ray % 2 else RED, 3 if index < 6 else 2)
            circle(frame, center, 24 + round(progress * 20), CRIMSON, 2)
        frames.append(frame)
    return frames


def magma_pool_frame(frame, center, phase, scale=1.0):
    cx, cy = center
    rx = round(44 * scale)
    ry = round(17 * scale)
    cv2.ellipse(frame, center, (rx + 3, ry + 3), 0, 0, 360, INK, -1, cv2.LINE_8)
    cv2.ellipse(frame, center, (rx, ry), 0, 0, 360, DARK, -1, cv2.LINE_8)
    cv2.ellipse(frame, center, (rx - 4, ry - 4), 0, 0, 360, CRIMSON, 2, cv2.LINE_8)
    for vein in range(5):
        angle = phase + vein * math.pi * 2 / 5
        inner = polar(center, angle, 6)
        outer = polar(center, angle, rx - 7)
        middle = ((inner[0] + outer[0]) // 2, inner[1] + round(math.sin(angle * 3) * 5))
        line(frame, inner, middle, RED, 2)
        line(frame, middle, outer, HOT, 1)
    for bubble in range(3):
        angle = phase * (1 + bubble * 0.15) + bubble * 2.1
        point = (round(cx + math.cos(angle) * rx * 0.55), round(cy + math.sin(angle) * ry * 0.55))
        circle(frame, point, 3 + bubble % 2, RED)
        circle(frame, (point[0] - 1, point[1] - 1), 1, GOLD)


def hellfire_impact():
    frames = []
    center = (64, 79)
    for index in range(12):
        frame = canvas((128, 128))
        if index < 4:
            pulse = 10 + index * 5
            circle(frame, center, pulse + 2, INK, 2)
            circle(frame, center, pulse, RED, 2)
            for shard in range(5):
                angle = shard * math.pi * 2 / 5 + index * 0.25
                start = polar(center, angle, pulse + 8)
                end = polar(center, angle, pulse + 2)
                line(frame, start, end, GOLD, 2)
            flame(frame, (64, 74 - index * 2), 8 + index * 2, index)
        else:
            progress = (index - 4) / 7
            radius = 12 + round(progress * 38)
            circle(frame, center, radius + 4, INK, 3)
            circle(frame, center, radius, RED if index < 9 else CRIMSON, 3)
            if index < 9:
                circle(frame, center, max(4, 22 - (index - 4) * 3), WHITE)
                circle(frame, center, max(3, 17 - (index - 4) * 2), HOT)
            for ray in range(10):
                angle = ray * math.pi / 5 + index * 0.09
                line(frame, polar(center, angle, 10), polar(center, angle, radius + 12),
                     GOLD if ray % 2 else RED, 3 if index < 8 else 2)
            for rock in range(7):
                angle = rock * math.pi * 2 / 7 + 0.3
                distance = 16 + (index - 4) * (3 + rock % 2)
                point = polar(center, angle, distance)
                cv2.rectangle(frame, (point[0] - 2, point[1] - 2), (point[0] + 2, point[1] + 2),
                              SMOKE if index > 8 else CRIMSON, -1)
        frames.append(frame)
    return frames


def magma_loop():
    frames = []
    center = (64, 79)
    for index in range(8):
        frame = canvas((128, 128))
        phase = index * math.pi / 4
        magma_pool_frame(frame, center, phase)
        for vent in range(3):
            angle = phase + vent * math.pi * 2 / 3
            x = round(center[0] + math.cos(angle) * 27)
            y = round(center[1] + math.sin(angle) * 8)
            height = 5 + ((index + vent * 2) % 4) * 3
            polygon(frame, [(x - 3, y), (x, y - height), (x + 3, y)], RED)
            circle(frame, (x, y - height), 2, HOT)
        frames.append(frame)
    return frames


def magma_burst():
    frames = []
    center = (64, 79)
    for index in range(10):
        frame = canvas((128, 128))
        phase = index * math.pi / 5
        magma_pool_frame(frame, center, phase, 1.0 + min(index, 5) * 0.025)
        progress = index / 9
        plume_height = round(math.sin(progress * math.pi) * 54)
        plume_width = max(5, round(math.sin(progress * math.pi) * 17))
        if plume_height > 0:
            polygon(frame, [
                (64 - plume_width, 78),
                (60 - plume_width // 3, 78 - plume_height // 2),
                (64, 78 - plume_height),
                (69 + plume_width // 3, 78 - plume_height // 2),
                (64 + plume_width, 78),
            ], INK)
            polygon(frame, [
                (64 - plume_width + 3, 77),
                (64, 80 - plume_height),
                (64 + plume_width - 3, 77),
            ], RED)
            circle(frame, (64, 80 - plume_height), max(2, plume_width // 3), HOT)
        for shard in range(8):
            angle = shard * math.pi / 4 + 0.15
            distance = round(progress * (28 + shard % 3 * 5))
            point = polar(center, angle, distance)
            circle(frame, point, 2, GOLD if shard % 2 else RED)
        frames.append(frame)
    return frames


def flame_stream(frame, start_x, front_x, center_y, width, phase, outer, inner):
    count = 11
    top = []
    bottom = []
    distance = max(1, front_x - start_x)
    for point_index in range(count):
        progress = point_index / (count - 1)
        x = round(start_x + distance * progress)
        body = math.sin(progress * math.pi) ** 0.7
        half = max(1, round((3 + body * width) * (1 - progress * 0.72)))
        center = center_y + round(math.sin(progress * 8.7 + phase) * (2 + body * 4))
        top.append((x, center - half - round(seeded_wave(point_index, phase, 2))))
        bottom.append((x, center + half + round(seeded_wave(point_index, phase + 3, 2))))
    polygon(frame, top + list(reversed(bottom)), outer)

    inset_top = [(x, y + 3) for x, y in top[1:-1]]
    inset_bottom = [(x, y - 3) for x, y in bottom[1:-1]]
    if inset_top and inset_bottom:
        polygon(frame, inset_top + list(reversed(inset_bottom)), inner)


def curved_ribbon(frame, start_x, end_x, center_y, amplitude, phase, color, thickness):
    points = []
    for point_index in range(15):
        progress = point_index / 14
        x = start_x + round((end_x - start_x) * progress)
        envelope = math.sin(progress * math.pi)
        y = center_y + round(math.sin(progress * 7.2 + phase) * amplitude * envelope)
        points.append((x, y))
    polyline(frame, points, color, thickness)


def impact_star(frame, center, radius, phase, color):
    points = []
    for point_index in range(18):
        angle = point_index * math.pi / 9 + phase
        irregular = 1.0 if point_index % 2 == 0 else 0.34 + (point_index % 3) * 0.08
        local_radius = radius * irregular * (1 + 0.08 * math.sin(point_index * 2.4 + phase))
        points.append(polar(center, angle, local_radius))
    polygon(frame, points, color)


def organic_blob(frame, center, radius, phase, color, vertical_scale=1.0):
    points = []
    for point_index in range(22):
        angle = point_index * math.pi * 2 / 22
        variation = 0.78 + 0.16 * math.sin(point_index * 2.17 + phase)
        variation += 0.09 * math.sin(point_index * 4.31 - phase * 0.7)
        local_radius = radius * variation
        points.append((
            round(center[0] + math.cos(angle) * local_radius),
            round(center[1] + math.sin(angle) * local_radius * vertical_scale),
        ))
    polygon(frame, points, color)


def flame_creature_head(frame, front_x, center_y, scale, phase):
    def point(x, y):
        return (front_x + round(x * scale), center_y + round(y * scale))

    polygon(frame, [
        point(-30, -14), point(-18, -27), point(-7, -21), point(1, -30),
        point(7, -17), point(18, -12), point(30, -4), point(15, 1),
        point(27, 10), point(7, 9), point(-2, 20), point(-18, 14),
        point(-31, 5),
    ], INK)
    polygon(frame, [
        point(-24, -11), point(-13, -21), point(-5, -15), point(1, -23),
        point(5, -12), point(17, -8), point(25, -4), point(11, 0),
        point(21, 7), point(4, 5), point(-4, 14), point(-17, 10),
    ], CRIMSON)
    polygon(frame, [
        point(-17, -7), point(-6, -13), point(4, -9), point(18, -4),
        point(7, 0), point(16, 5), point(-2, 4), point(-10, 10),
    ], RED)
    polygon(frame, [point(-9, -4), point(4, -7), point(13, -3), point(2, 1)], HOT)
    polygon(frame, [point(12, 2), point(27, 9), point(10, 8)], GOLD)
    eye = point(2 + math.sin(phase) * 1.5, -10)
    circle(frame, eye, max(1, round(2 * scale)), CYAN)
    circle(frame, point(3, -10), 1, WHITE)


def flame_impact_plumes(frame, center, radius, phase, fade):
    for plume in range(9):
        angle = -2.55 + plume * 0.64 + math.sin(phase + plume) * 0.08
        length = radius * (0.72 + (plume % 4) * 0.12)
        width = max(3, round(radius * (0.15 + plume % 3 * 0.025)))
        direction = (math.cos(angle), math.sin(angle))
        normal = (-direction[1], direction[0])
        tip = (
            round(center[0] + direction[0] * length),
            round(center[1] + direction[1] * length),
        )
        bend = (
            round(center[0] + direction[0] * length * 0.56 + normal[0] * seeded_wave(plume, phase, 8)),
            round(center[1] + direction[1] * length * 0.56 + normal[1] * seeded_wave(plume, phase, 8)),
        )
        polygon(frame, [
            (round(center[0] + normal[0] * width), round(center[1] + normal[1] * width)),
            (round(bend[0] + normal[0] * width * 0.55), round(bend[1] + normal[1] * width * 0.55)),
            tip,
            (round(bend[0] - normal[0] * width * 0.55), round(bend[1] - normal[1] * width * 0.55)),
            (round(center[0] - normal[0] * width), round(center[1] - normal[1] * width)),
        ], with_alpha(RED if plume % 2 else VIOLET, fade))
        line(frame, center, tip, with_alpha(HOT if plume % 3 else CYAN, fade * 0.72),
             max(1, width // 3))


def demon_silhouette(frame, front_x, phase, alpha):
    body = [
        (front_x - 205, 104), (front_x - 188, 61), (front_x - 158, 28),
        (front_x - 124, 46), (front_x - 96, 13), (front_x - 69, 49),
        (front_x - 42, 31), (front_x - 18, 50), (front_x + 8, 61),
        (front_x + 25, 79), (front_x + 7, 91), (front_x + 25, 105),
        (front_x - 7, 111), (front_x - 34, 143), (front_x - 72, 118),
        (front_x - 118, 151), (front_x - 151, 115),
    ]
    polygon(frame, body, with_alpha(INK, alpha))
    polygon(frame, [
        (front_x - 177, 98), (front_x - 157, 64), (front_x - 128, 46),
        (front_x - 101, 59), (front_x - 78, 35), (front_x - 60, 61),
        (front_x - 36, 48), (front_x - 12, 63), (front_x + 12, 76),
        (front_x - 3, 88), (front_x + 10, 100), (front_x - 28, 100),
        (front_x - 50, 127), (front_x - 78, 107), (front_x - 115, 132),
        (front_x - 139, 104),
    ], with_alpha(INDIGO, alpha * 0.9))

    wing_lift = round(math.sin(phase) * 7)
    polygon(frame, [
        (front_x - 153, 85), (front_x - 203, 31 + wing_lift),
        (front_x - 164, 42), (front_x - 181, 8 + wing_lift),
        (front_x - 126, 57), (front_x - 110, 88),
    ], with_alpha(VIOLET, alpha * 0.8))
    polygon(frame, [
        (front_x - 128, 93), (front_x - 185, 135 - wing_lift),
        (front_x - 143, 126), (front_x - 158, 154 - wing_lift),
        (front_x - 103, 111), (front_x - 89, 90),
    ], with_alpha(MAGENTA, alpha * 0.58))
    polyline(frame, [
        (front_x - 190, 34 + wing_lift), (front_x - 150, 62),
        (front_x - 111, 87), (front_x - 63, 92),
    ], with_alpha(CYAN, alpha * 0.5), 2)
    line(frame, (front_x - 25, 54), (front_x - 4, 34), with_alpha(VIOLET, alpha), 5)
    line(frame, (front_x - 18, 58), (front_x + 4, 45), with_alpha(MAGENTA, alpha), 3)
    circle(frame, (front_x - 3, 69), 3, with_alpha(CYAN, alpha))


def diagonal_slash(frame, progress, alpha):
    travel = round(ease_out_cubic(progress) * 210)
    center_x = 28 + travel
    for slash in range(4):
        offset = slash * 9 - 13
        points = [
            (center_x - 72 + offset, 151),
            (center_x - 28 + offset, 102),
            (center_x + 19 + offset, 57),
            (center_x + 58 + offset, 8),
        ]
        color = MAGENTA if slash % 2 == 0 else CYAN
        polyline(frame, points, with_alpha(color, alpha * (1 - slash * 0.12)), 7 - slash)
    polyline(frame, [(center_x - 62, 154), (center_x + 70, 4)],
             with_alpha(WHITE, alpha), 3)


def battlefield_flash(frame, index):
    strength = (0.84, 0.95, 0.76)[index - 22]
    cv2.rectangle(frame, (0, 0), (255, 159), with_alpha(WHITE, strength), -1)
    polygon(frame, [
        (0, 8), (72, 0), (42, 40), (136, 18), (103, 67),
        (256, 37), (256, 0),
    ], with_alpha(VIOLET, 0.42))
    polygon(frame, [
        (0, 160), (0, 116), (75, 139), (48, 92), (147, 126),
        (196, 83), (256, 107), (256, 160),
    ], with_alpha(MAGENTA, 0.46))
    for slash in range(7):
        x = 18 + slash * 38 + (index - 22) * 7
        line(frame, (x - 24, 159), (x + 55, 0),
             with_alpha(CYAN if slash % 3 == 0 else WHITE, 0.34), 2 + slash % 2)


def crimson_gale_layers():
    layers = {name: [] for name in ("Core", "Ribbon", "Sparks", "Impact", "Residue")}
    origin = (36, 80)
    contact = (222, 80)

    for index in range(48):
        frames = {name: canvas((256, 160)) for name in layers}
        core = frames["Core"]
        ribbon = frames["Ribbon"]
        sparks = frames["Sparks"]
        impact = frames["Impact"]
        residue = frames["Residue"]

        if index <= 7:
            progress = index / 7
            pulse = 9 + round(progress * 13)
            flame_stream(core, 18, 47 + round(progress * 9), 80, pulse,
                         index * 0.68, with_alpha(INK, 0.78), CRIMSON)
            flame_stream(core, 27, 53 + round(progress * 7), 78, max(5, pulse - 7),
                         index * 0.83, RED, HOT)
            organic_blob(core, (45, 78), 5 + round(progress * 7), index * 0.6, WHITE, 0.82)

            demon_silhouette(ribbon, 80 + round(progress * 40), index * 0.31,
                             0.14 + progress * 0.34)
            for orbit in range(4):
                angle = index * 0.55 + orbit * math.pi / 2
                point = polar(origin, angle, 14 + round(progress * 18))
                tail = polar(point, angle + math.pi, 9)
                line(sparks, point, tail, CYAN if orbit % 2 == 0 else MAGENTA, 2)
        elif index <= 21:
            progress = (index - 8) / 13
            front_x = round(50 + ease_out_cubic(progress) * 176)
            width = 24 + round(progress * 25)
            flame_stream(core, 14, front_x, 82, width, index * 0.57, INK, CRIMSON)
            flame_stream(core, 25, front_x + 1, 78, max(10, width - 10), index * 0.79, RED, HOT)
            flame_stream(core, 38, front_x + 4, 76, max(5, width - 21), index * 0.95, GOLD, WHITE)
            flame_creature_head(core, front_x - 8, 79, 0.72 + progress * 0.48, index * 0.23)

            demon_silhouette(ribbon, front_x + 3, index * 0.28, 0.58 + progress * 0.36)
            for echo in range(3):
                echo_front = front_x - 18 - echo * 15
                demon_silhouette(ribbon, echo_front, index * 0.25 + echo,
                                 0.18 - echo * 0.045)
            for ribbon_index in range(6):
                offset = (-31, -20, -8, 9, 21, 33)[ribbon_index]
                curved_ribbon(
                    ribbon,
                    5 + ribbon_index * 2,
                    front_x - ribbon_index * 3,
                    80 + offset,
                    18 + ribbon_index * 2,
                    index * 0.41 + ribbon_index * 1.47,
                    with_alpha(MAGENTA if ribbon_index % 3 == 0 else
                               VIOLET if ribbon_index % 2 == 0 else INDIGO, 0.86),
                    max(2, 7 - ribbon_index // 2),
                )
            if index >= 16:
                diagonal_slash(impact, (index - 16) / 5, 0.42 + (index - 16) * 0.09)
        elif index <= 24:
            battlefield_flash(impact, index)
            progress = (index - 22) / 2
            demon_silhouette(ribbon, 228, index * 0.3, 0.82 - progress * 0.22)
            organic_blob(core, contact, 42 + round(progress * 16), index * 0.4,
                         with_alpha(WHITE, 0.92), 0.82)
            diagonal_slash(sparks, progress, 0.95)
        elif index <= 30:
            progress = (index - 25) / 5
            fade = 1 - progress
            flame_stream(core, 28 + round(progress * 48), 230, 80,
                         39 - round(progress * 23), index * 0.62,
                         with_alpha(INK, 0.42 + fade * 0.58),
                         with_alpha(CRIMSON, 0.35 + fade * 0.65))
            organic_blob(core, contact, max(4, round(34 * fade)), index * 0.3,
                         with_alpha(WHITE, 0.22 + fade * 0.78), 0.8)
            flame_impact_plumes(impact, contact, 74 + round(progress * 34),
                                index * 0.19, fade)
            organic_blob(impact, contact, 38 + round(fade * 30), index * 0.41,
                         with_alpha(HOT, fade * 0.92), 0.82)
            organic_blob(impact, (contact[0] + 3, contact[1] - 3),
                         21 + round(fade * 24), index * 0.57,
                         with_alpha(WHITE, fade), 0.76)
            diagonal_slash(impact, min(1, progress + 0.35), fade * 0.82)

            for fragment in range(8):
                angle = -2.7 + fragment * 0.72 + index * 0.035
                end = polar(contact, angle, 54 + round(progress * 66) + fragment * 3)
                bend = ((contact[0] + end[0]) // 2 - 10,
                        (contact[1] + end[1]) // 2 + round(math.sin(angle * 2) * 14))
                polyline(ribbon, [contact, bend, end],
                         with_alpha(MAGENTA if fragment % 3 == 0 else
                                    VIOLET if fragment % 2 else INDIGO, fade),
                         max(2, 8 - fragment // 2))
        else:
            progress = (index - 31) / 16
            fade = 1 - progress
            flame_stream(core, 138 + round(progress * 34), 230, 82,
                         max(3, round(15 * fade)), index,
                         with_alpha(INK, fade * 0.68), with_alpha(CRIMSON, fade * 0.58))
            circle(core, (222, 80), max(1, round(6 * fade)), with_alpha(HOT, fade))

            for wisp in range(9):
                start = (91 + wisp * 18, 91 + wisp % 3 * 7)
                rise = round(progress * (30 + wisp * 5))
                points = [
                    start,
                    (start[0] - 11 + round(seeded_wave(index, wisp, 7)), start[1] - rise // 2),
                    (start[0] + round(seeded_wave(index, wisp + 4, 12)), start[1] - rise),
                ]
                polyline(ribbon, points,
                         with_alpha(MAGENTA if wisp % 3 == 0 else
                                    VIOLET if wisp % 2 else INDIGO, fade * 0.75),
                         6 if wisp < 3 else 3)

            arc_alpha = max(0.05, fade * 0.7)
            cv2.ellipse(impact, contact, (56 + round(progress * 45), 35 + round(progress * 27)),
                        -12, 194, 344, with_alpha(CYAN, arc_alpha), 2, cv2.LINE_8)
            cv2.ellipse(impact, contact, (42 + round(progress * 52), 49 + round(progress * 38)),
                        18, 12, 145, with_alpha(VIOLET, arc_alpha), 3, cv2.LINE_8)

        front_hint = 53 if index <= 7 else min(228, 50 + max(0, index - 8) * 17)
        for spark in range(24):
            if index <= 21:
                travel = ((spark * 29 + index * 23) % max(28, front_hint - 12))
                x = 10 + travel
                y = 80 + round(seeded_wave(index, spark, 41))
                direction = 7 + spark % 7
                color = CYAN if spark % 4 == 0 else MAGENTA if spark % 5 == 0 else GOLD
                line(sparks, (x, y), (x - direction, y + (spark % 5 - 2) * 2), color,
                     3 if spark % 6 == 0 else 2)
            elif index > 24:
                progress = (index - 25) / 22
                angle = spark * math.pi * 2 / 24 + 0.11
                distance = 18 + round(progress * (56 + spark % 6 * 11))
                point = polar(contact, angle, distance)
                tail = polar(point, angle + math.pi, 7 + spark % 6)
                line(sparks, point, tail,
                     with_alpha(CYAN if spark % 4 == 0 else MAGENTA if spark % 5 == 0 else GOLD,
                                1 - progress * 0.82),
                     3 if index < 33 and spark % 4 == 0 else 2)

        if index < 22:
            charge = index / 21
            for mark in range(4):
                y = 50 + mark * 20
                length = 4 + round(charge * 18)
                line(impact, (233 - length, y), (235, y + (mark % 2) * 5 - 2),
                     with_alpha(MAGENTA if mark % 2 else CYAN, 0.2 + charge * 0.58),
                     3 if mark < 2 else 2)

        recovery = max(0.0, (index - 15) / 32)
        residue_alpha = 0.2 + min(0.8, recovery * 1.7)
        ground_points = [(38, 113), (71, 109), (106, 116), (142, 108), (181, 115), (226, 109)]
        polyline(residue, ground_points, with_alpha(INK, residue_alpha), 10)
        polyline(residue, ground_points[1:], with_alpha(CRIMSON, residue_alpha * 0.78), 4)
        polyline(residue, [(61, 120), (105, 126), (154, 119), (218, 126)],
                 with_alpha(INDIGO, residue_alpha * 0.62), 6)
        for ember in range(16):
            x = 31 + ((ember * 31 + index * 7) % 202)
            y = 121 - round(recovery * (14 + ember * 4)) + round(seeded_wave(index, ember, 7))
            circle(residue, (x, y), 1 + ember % 2,
                   with_alpha(GOLD if ember % 3 else MAGENTA,
                              residue_alpha * (1 - recovery * 0.68)))

        for name, frame in frames.items():
            layers[name].append(frame)
    return layers


def scorch_loop():
    frames = []
    origin = (22, 64)
    for index in range(8):
        frame = canvas((192, 128))
        polygon(frame, [origin, (174, 18), (160, 64), (174, 110)], INK)
        polygon(frame, [(29, 64), (164, 25), (151, 64), (164, 103)], DARK)
        phase = index * math.pi / 4
        for crack in range(7):
            angle = -0.55 + crack * 1.1 / 6
            length = 48 + crack * 14
            start = (38, 64)
            end = (round(start[0] + math.cos(angle) * length),
                   round(start[1] + math.sin(angle) * length))
            line(frame, start, end, CRIMSON, 3)
            line(frame, start, (end[0] - 6, end[1]), RED, 1)
        for vent in range(6):
            x = 45 + vent * 21
            y = 64 + round(math.sin(phase + vent) * (8 + vent * 2))
            height = 5 + ((index + vent) % 4) * 3
            polygon(frame, [(x - 3, y), (x, y - height), (x + 3, y)],
                    HOT if vent % 2 else RED)
        frames.append(frame)
    return frames


def firestorm_convert():
    frames = []
    center = (96, 119)
    for index in range(10):
        frame = canvas((192, 192))
        progress = index / 9
        radius = 28 + round(progress * 56)
        cv2.ellipse(frame, center, (radius, max(10, radius // 3)), 0, 0, 360, INK, 4, cv2.LINE_8)
        cv2.ellipse(frame, center, (radius - 4, max(7, radius // 3 - 3)), 0, 0, 360,
                    CRIMSON, 3, cv2.LINE_8)
        flame_height = round(math.sin(progress * math.pi / 2) * 105)
        for column in range(7):
            x = 56 + column * 13
            local = max(8, flame_height - abs(column - 3) * 10)
            if local > 9:
                polygon(frame, [(x - 6, 117), (x - 2, 117 - local // 2),
                                (x + (column % 2) * 4, 117 - local),
                                (x + 6, 117)], INK)
                polygon(frame, [(x - 3, 116), (x, 121 - local), (x + 3, 116)],
                        RED if column % 2 else HOT)
        for spark in range(12):
            angle = index * 0.35 + spark * math.pi / 6
            point = polar((96, 98), angle, 18 + progress * 58)
            circle(frame, point, 2, GOLD if spark % 3 else RED)
        frames.append(frame)
    return frames


def firestorm_loop():
    frames = []
    center = (96, 118)
    for index in range(8):
        frame = canvas((192, 192))
        phase = index * math.pi / 4
        cv2.ellipse(frame, center, (76, 25), 0, 0, 360, INK, 5, cv2.LINE_8)
        cv2.ellipse(frame, center, (71, 21), 0, 0, 360, CRIMSON, 3, cv2.LINE_8)
        for column in range(9):
            angle = phase + column * math.pi * 2 / 9
            x = round(96 + math.cos(angle) * 58)
            base_y = round(116 + math.sin(angle) * 14)
            height = 42 + ((index + column * 2) % 5) * 11
            polygon(frame, [(x - 7, base_y), (x - 2, base_y - height // 2),
                            (x + round(math.sin(angle) * 5), base_y - height),
                            (x + 7, base_y)], INK)
            polygon(frame, [(x - 4, base_y - 2), (x, base_y - height + 5),
                            (x + 4, base_y - 2)], RED if column % 2 else HOT)
        for spark in range(14):
            x = 30 + ((spark * 23 + index * 11) % 132)
            y = 18 + ((spark * 17 + index * 9) % 88)
            circle(frame, (x, y), 1 + spark % 2, GOLD)
        frames.append(frame)
    return frames


def flame_shield(mode, count):
    frames = []
    center = (48, 48)
    for index in range(count):
        frame = canvas((96, 96))
        if mode == "spawn":
            radius = 8 + index * 5
            alpha_color = RED if index < 5 else HOT
            circle(frame, center, radius + 3, INK, 3)
            circle(frame, center, radius, alpha_color, 2)
        elif mode == "loop":
            radius = 38 + round(math.sin(index * math.pi / 4) * 2)
            circle(frame, center, radius + 3, INK, 3)
            circle(frame, center, radius, CRIMSON, 2)
            for rune in range(8):
                angle = index * math.pi / 16 + rune * math.pi / 4
                line(frame, polar(center, angle, radius - 5), polar(center, angle, radius + 1),
                     GOLD if rune % 2 else RED, 2)
        else:
            radius = 38
            for shard in range(12):
                angle = shard * math.pi / 6 + index * 0.08
                start = polar(center, angle, radius - 4 + index * 3)
                end = polar(center, angle, radius + 4 + index * 7)
                line(frame, start, end, HOT if shard % 2 else RED, 3 if index < 3 else 2)
        frames.append(frame)
    return frames


def heat_aura():
    frames = []
    center = (48, 61)
    for index in range(8):
        frame = canvas((96, 96))
        phase = index * math.pi / 4
        cv2.ellipse(frame, center, (32, 14), 0, 0, 360, CRIMSON, 2, cv2.LINE_8)
        for flame_index in range(7):
            x = 24 + flame_index * 8
            height = 12 + round((math.sin(phase + flame_index) + 1) * 7)
            polygon(frame, [(x - 4, 66), (x, 66 - height), (x + 4, 66)],
                    RED if flame_index % 2 else HOT)
        frames.append(frame)
    return frames


def ember_projectile():
    frames = []
    for index in range(8):
        frame = canvas((48, 48))
        center = (30, 24)
        wave = round(math.sin(index * math.pi / 4) * 4)
        line(frame, (7, 24 + wave), (25, 24), CRIMSON, 3)
        line(frame, (13, 22 - wave // 2), (27, 23), HOT, 2)
        circle(frame, center, 6, INK)
        circle(frame, center, 4, RED)
        circle(frame, (32, 22), 2, WHITE)
        frames.append(frame)
    return frames


def ember_ignite():
    frames = []
    center = (32, 39)
    for index in range(6):
        frame = canvas((64, 64))
        height = 8 + index * 6 if index < 3 else 26 - (index - 3) * 6
        polygon(frame, [(22, 46), (27, 37), (29, 46 - height), (34, 35),
                        (38, 45 - height // 2), (43, 46)], INK)
        polygon(frame, [(25, 45), (31, 48 - height), (35, 41), (40, 45)], RED)
        circle(frame, center, max(2, 7 - index), HOT)
        frames.append(frame)
    return frames


def ash_dissolve():
    frames = []
    for index in range(10):
        frame = canvas((96, 96))
        progress = index / 9
        remaining_height = round(52 * (1 - progress))
        if remaining_height > 0:
            cv2.rectangle(frame, (37, 73 - remaining_height), (59, 73), DARK, -1)
            circle(frame, (48, 69 - remaining_height), 12, INK)
            line(frame, (37, 48), (28, 63), CRIMSON, 4)
            line(frame, (59, 48), (68, 63), CRIMSON, 4)
        for particle in range(14):
            x = 24 + ((particle * 17 + index * 7) % 49)
            y = 72 - round(progress * (18 + particle * 3) % 60)
            circle(frame, (x, y), 1 + particle % 2, GOLD if particle % 3 else RED)
        frames.append(frame)
    return frames


def save(name, frames):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    cv2.imwrite(str(OUTPUT / name), np.hstack(frames))


def main():
    save("FireGodFlameProjectile.png", flame_projectile())
    save("FireGodFlameImpact.png", flame_impact())
    save("FireGodAshOne.png", ash_orbit(1))
    save("FireGodAshTwo.png", ash_orbit(2))
    save("FireGodAshSigil.png", ash_sigil())
    save("FireGodAshWarning.png", ash_warning())
    save("FireGodAshDetonation.png", ash_detonation())
    save("FireGodHellfireImpact.png", hellfire_impact())
    save("FireGodMagmaLoop.png", magma_loop())
    save("FireGodMagmaBurst.png", magma_burst())
    for layer_name, frames in crimson_gale_layers().items():
        save(f"FireGodCrimsonGale{layer_name}.png", frames)
    save("FireGodScorchLoop.png", scorch_loop())
    save("FireGodFirestormConvert.png", firestorm_convert())
    save("FireGodFirestormLoop.png", firestorm_loop())
    save("FireGodFlameShieldSpawn.png", flame_shield("spawn", 8))
    save("FireGodFlameShieldLoop.png", flame_shield("loop", 8))
    save("FireGodFlameShieldBreak.png", flame_shield("break", 6))
    save("FireGodHeatAura.png", heat_aura())
    save("FireGodEmberProjectile.png", ember_projectile())
    save("FireGodEmberIgnite.png", ember_ignite())
    save("FireGodAshDissolve.png", ash_dissolve())
    print(f"Generated Fire God VFX in {OUTPUT}")


if __name__ == "__main__":
    main()
