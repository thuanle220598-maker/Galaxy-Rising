# Aurelia - Trái Tim Của Long Mạch

## 1. Identity

- Internal ID: `Astra`
- Display name: `Aurelia`
- Title: `Trái Tim Của Long Mạch`
- Integration mode: `replace-existing`
- Existing slot replaced: `Astra`
- Species: Dragon
- Class / formation: Support / Back
- Rarity: SSR
- Element / damage type: Water / Physical damage path hiện tại
- One-line fantasy: Công chúa Thủy Long dùng Long Ngọc để thanh tẩy, chữa lành và dựng thủy giáp bảo vệ cả đội.
- Character philosophy: “Nước mềm mại có thể ôm trọn vạn vật, nhưng khi cuồng phong nổi lên, nước có thể nhấn chìm cả đại dương.”
- Combat loop: Tấn công tạo Thủy Ngọc -> bảo vệ đồng đội yếu nhất -> tích Hydro-Resonance -> mở Long Mạch Trận hồi máu và tạo giáp toàn đội.
- Intended strength: hồi phục ổn định, shield toàn đội, cleanse và kiểm soát nhịp giao tranh bằng slow.
- Intended weakness / counterplay: sát thương thấp, cần đồng đội còn sống để phát huy, dễ bị burst hoặc khống chế trước khi dựng trận.

Giữ enum `PrototypeSkillKit.Astra`, filename/GUID và resource path `Astra`; migration duy nhất đổi display name trong progression, gacha và squad từ `Astra` sang `Aurelia`.

## 2. Lore

### Origin

Aurelia là công chúa út của Vạn Lý Long Tộc, dòng tộc từng cai quản đại dương và sông ngòi để điều hòa nguồn ma thuật Thủy hệ. Cô mang huyết mạch thuần khiết nhất của Long Vương nhưng chọn nghiên cứu thanh tẩy và lắng nghe tiếng nói của các dòng chảy thay vì tranh đấu cùng các huynh trưởng.

### Inciting tragedy or vow

Ma thuật tà ác xâm nhập long mạch thế giới, làm ô nhiễm nguồn nước và biến sinh vật biển thành quái thú. Trong trận bảo vệ Long Tuyền Thánh Địa, Long Vương cùng các hoàng tử gieo mình xuống vực sâu để phong ấn vết nứt. Trước khi ngã xuống, ông trao Long Ngọc Bích Thủy cho Aurelia và phong ấn hình dạng rồng thật của cô trong thân xác con người.

### Present goal

Aurelia bước lên mặt đất để chữa lành toàn bộ long mạch, thanh tẩy nguồn nước và tìm cách giải phóng gia đình khỏi phong ấn dưới vực sâu.

### Cost of power

Mỗi lần mở Long Mạch Trận, Aurelia phải dẫn ô nhiễm qua chính Long Ngọc. Nếu ngọc vỡ hoặc cô mất kiểm soát Hydro-Resonance, ma lực tà ác có thể xâm nhập huyết mạch và biến cô thành thủy long cuồng nộ.

### Faction, location and relationships

- Faction: Vạn Lý Long Tộc.
- Origin: Long Tuyền Thánh Địa.
- Ignis: Aurelia nhìn thấy linh hồn kiệt sức dưới ngọn lửa của anh và dùng Long Mạch Trận để làm dịu lò phản ứng ma thuật. Hỏa lực thuần khiết của Ignis giúp cô thiêu hủy mầm ô nhiễm bám sâu trong long mạch.
- Relationship rule: hai người bổ trợ về cốt truyện và hình ảnh; không khóa hiệu quả gameplay của Aurelia vào riêng Ignis.

### Story details that must appear visually

- Hai sừng rồng nhỏ trong suốt như pha lê.
- Long Ngọc Bích Thủy là điểm sáng nhất của sprite.
- Vảy ngọc lam trên má và cánh tay.
- Tà áo xanh ngọc chảy như dòng nước, có hoa văn long mạch phân nhánh.
- Các hạt ngọc nước tượng trưng cho sự sống và Thủy Long Giáp.

## 3. Visual Contract

- Silhouette: nữ pháp sư dáng gọn theo Sprite Fusion reference, tóc bạc đổ thành một khối dài sang trái; áo hẹp, tay trước nâng Long Ngọc; hai sừng nhỏ tạo dấu hiệu rồng nhưng không biến thành giáp nặng.
- Body proportions: compact dark-fantasy khoảng 4 đầu, đỉnh sừng y=0, chân y=60-61.
- Hair shape and elemental streaks: tóc bạc-trắng với bóng xanh lạnh, một lọn Hydro-Resonance ngọc lam chạy dọc phía trái.
- Outfit and trailing cloth: trường bào xanh lam/navy hẹp theo ref, thân trước sáng vừa, gấu áo lượn sóng; tránh hai mảng khăn choàng lớn như cánh.
- Story accessories: sừng pha lê và vảy má; khóa đai hình Long Ngọc.
- Raised-hand magic object: Long Ngọc Bích Thủy với dải nước cuộn thành cổ/râu rồng và 3-5 thủy châu nhỏ.
- Main dark color: navy `#18285E` và xanh biển sâu.
- Element accent ramp: `#2A92A6` -> `#60CCD6`.
- Glow ramp: ngọc bích `#56E4C8` -> trắng lạnh.
- Metal/accessory color: vàng `#E6B646` dùng tiết chế ở đai và trang sức.
- Tinted outline: navy đen `#090C20`.
- Secondary magic light interaction: Long Ngọc chiếu lên tay, ống tay, má phải và mép tóc gần nhất.
- Negative visual constraints: không chibi, không mắt anime lớn, không full dragon armor, không đinh ba/vũ khí lớn, không váy công chúa sáng bóng, không nền/bóng đất/chữ.
- Canon comparison assets: `Previews/sprite-fusion-76d4805b-391e-4bc4-a82a-d4cdd624c77a.png`, `Assets/Art/Characters/Nova/Combat/FireGodIdle.png` và `Assets/Art/Characters/Aurelia/Aurelia.png`.

### Generator prompt

```text
Use case: stylized-concept
Asset type: 64x64 production game character sprite
Primary request: Aurelia, water dragon princess support mage, “Heart of the Leyline”
Subject: full-body compact woman in 3/4 view facing right; very long silver-white hair with cool blue shadows and one turquoise resonance streak flowing left; small translucent crystal dragon horns; turquoise cheek scales; narrow royal-blue and navy robe with wave hem and subtle branching leyline embroidery; front hand raised holding a glowing jade dragon pearl; restrained dragon-water curl around the pearl; three floating water beads
Style/medium: match the supplied 32x32 Sprite Fusion reference silhouette and color economy; crisp hand-pixeled dark-fantasy sprite; limited palette under 64 colors; no anti-aliasing
Composition/framing: transparent 64x64 canvas derived from the 32x32 reference; compact centered body; horns touch the top; feet at y 60-61; magic on the right; silver hair flowing left
Lighting/mood: main light upper-right; jade pearl is the brightest point; calm, compassionate, ancient power
Constraints: transparent background; point pixels; preserve readable three-part silhouette; no text; no watermark
Avoid: oversized cape or water wings, bulky silhouette, large anime eyes, smooth gradients, soft brush, background, ground shadow, full dragon armor, large weapon, extra character
```

## 4. Gameplay Data

Giữ stat của slot Astra trong pass thay identity để không trộn redesign với balance pass.

| Field | Value | Baseline/reason |
| --- | ---: | --- |
| Max Health | 140 | giữ slot Astra |
| Attack | 25 | giữ slot Astra |
| Defense | 6 | giữ slot Astra |
| Move Speed | 1.85 | giữ slot Astra |
| Attack Interval | 1.15 | giữ slot Astra |
| Attack Range | 3.6 | derived from Support class |

## 5. Skill Kit

### Basic: Water Serpent / Hạt Thủy Long

- Mỗi đòn gây `1.0x` damage, nhận `18` energy và tạo hai Hydro Beads cho hai đồng minh sống ngẫu nhiên.
- Bead tới đồng minh: `+3` energy, shield bằng `6% Max HP` của Aurelia trong 4 giây và kích hoạt Tidal Cleansing.
- Bead tới Aurelia: tăng `15%` move speed trong 3 giây và giảm Active cooldown `1s`.
- Projectile dùng `AureliaWaterSerpent`; bead dùng `AureliaHydroBead`.

### Passive 1: Oceanic Scales

- Là contract tạo và nhận Hydro Beads của Basic.
- Khi Dragon Realm đang tồn tại, bead mới hoặc bead còn bay sẽ bị hút vào tâm trận thay vì tới mục tiêu.

### Passive 2: Tidal Cleansing

- Mỗi heal hoặc shield do Aurelia tạo xóa ngẫu nhiên một status bất lợi của mọi đồng minh trong bán kính `3m` quanh người nhận.
- Nếu người nhận đang bị stun, airborne, knockback hoặc taunt, heal/shield tăng `30%` trước khi cleanse.
- Chỉ dùng các status runtime hiện có; không thêm Freeze/Silence giả.

### Passive 3: Dragon Pulse Aura

- Đồng minh trong `8m` nhận `8%` damage reduction và giảm `10%` thời lượng negative status mới.
- Nếu Aurelia chịu tổng damage vượt `30% Max HP` trong một cửa sổ 1 giây, cô phát Dragon Pressure: đẩy kẻ địch gần `3m`, slow `50%` trong 2 giây; cooldown `60s`.

### Active: Dragon Realm / Long Mạch Trận

- Cooldown `6s`, cast `0.7s`, release tại `57%` animation.
- Tạo domain bán kính `4.2m`, tồn tại 6 giây; heal ally trong vùng mỗi `0.5s` bằng `2% Max HP` của Aurelia.
- Ally trong vùng giảm thêm `20%` incoming damage; `10%` damage đã chặn được tích lại để heal ally thấp máu nhất.
- Mỗi Hydro Bead bị hút kéo dài domain `1s` và burst-heal toàn đội bằng `4% Max HP` của Aurelia.

### Ultimate: Draconian Aegis / Vạn Lý Long Bích

- Tốn 100 energy, cast `0.78s`, release tại `50%` animation.
- Toàn đội nhận shield bằng `35% Max HP` của Aurelia trong 8 giây.
- Khi shield còn: `+25%` outgoing damage, `+20%` move speed và `+20%` attack speed; 2 giây đầu miễn hard control.
- Nếu shield bị đánh vỡ: gây `0.8x` Magic damage trong bán kính `1.8m` và slow `40%` trong 2 giây.

## 6. Animation Manifest

Chỉ key pose Idle được sản xuất trong pass này; các state còn lại chờ duyệt silhouette.

| State | Canvas | Frames | FPS | Loop | Duration | Gameplay frame | File |
| --- | --- | ---: | ---: | --- | ---: | ---: | --- |
| Idle key pose | 64x64 | 1 | - | yes | - | - | `Assets/Art/Characters/Aurelia/Aurelia.png` |
| Idle | 64x64 | 6 | 2.2 | yes | 2.7s | - | `Assets/Art/Characters/Aurelia/Combat/AureliaIdle.png` |
| Run | 64x64 | 8 | 10 | yes | 0.8s | - | `Assets/Art/Characters/Aurelia/Combat/AureliaRun.png` |
| Basic | 64x64 | 6 | 14.3 | no | 0.42s | 50% | `Assets/Art/Characters/Aurelia/Combat/AureliaBasic.png` |
| Active | 64x64 | 10 | 14.3 | no | 0.7s | 57% | `Assets/Art/Characters/Aurelia/Combat/AureliaActive.png` |
| Ultimate | 64x64 | 12 | 15.4 | no | 0.78s | 50% | `Assets/Art/Characters/Aurelia/Combat/AureliaUltimate.png` |
| Hit | 64x64 | 4 | 16.7 | no | 0.24s | - | `Assets/Art/Characters/Aurelia/Combat/AureliaHit.png` |
| Death | 64x64 | 8 | 14.3 | no | 0.56s | - | `Assets/Art/Characters/Aurelia/Combat/AureliaDeath.png` |

Pivot/foot line: bottom-center; feet at y=61-62 across every frame.

Facing: source faces right; `sourceFacesLeft = false`.

## 7. VFX Manifest

| Effect | Trigger | Shape/size | Lifetime | Palette | Reuse/new asset |
| --- | --- | --- | ---: | --- | --- |
| Water Serpent | Basic travel | 32x32 serpentine projectile | action travel | navy/cyan/jade | `AureliaWaterSerpent.png` |
| Hydro Bead | Basic support | 16x16 floating pearl | <=0.42s | cyan/jade/white | `AureliaHydroBead.png` |
| Tidal Cleansing | heal/shield | 64x64 expanding ring | 0.44s | jade/white | `AureliaCleansingRing.png` |
| Dragon Pulse Aura | passive aura | 64x64 dual-dragon ring | loop | navy/cyan | `AureliaDragonAura.png` |
| Dragon Pressure | burst defense | 96x96 dragon shockwave | 0.6s | cyan/jade/white | `AureliaDragonPressure.png` |
| Dragon Realm | Active | 128x128 leyline + dual dragons | domain duration | navy/cyan/jade | `AureliaDomain.png` |
| Water Shield | Ultimate buff | 64x64 scale sphere | up to 8s | cyan/jade | `AureliaShieldLoop.png` |
| Shield Break | shield destroyed | 64x64 radial burst | 0.44s | cyan | `AureliaShieldBreak.png` |
| World Dragon | Ultimate cast | 128x128 rising dragon | 0.78s | navy/cyan/jade | `AureliaUltimateDragon.png` |
| Blessing Impact | Ultimate target | 64x64 falling water/ring | 0.44s | jade/white | `AureliaBlessingImpact.png` |

Readability budget at `360x640`: trận đồ nằm thấp dưới chân; dragon silhouette chỉ xuất hiện ở rìa và không phủ portrait/HP/energy.

Cleanup rule: mọi cast VFX tự hủy theo lifetime; shield VFX dừng khi shield hết; dọn sạch khi battle/farm wave reset.

## 8. Integration Checklist

- [x] Internal ID and replace/new-slot mode confirmed
- [x] Combatant and SkillDefinition paths chosen: existing `Astra` assets
- [x] Similar hero baselines recorded: Astra and Nyx
- [x] Save/GUID compatibility decision recorded
- [x] Idle key pose regenerated before full animation
- [x] User approves idle silhouette and palette
- [x] Every `Astra` enum/helper caller searched before runtime edit
- [x] Import settings and frame manifests verified
- [x] Gameplay hit frames match animation contacts
- [x] VFX readable at x1/x2 and do not cover HUD
- [x] Focused Play Mode regression added
- [x] Full Play Mode suite passes with no Console errors

## 9. Open Decisions

- Không còn quyết định mở trong scope hiện tại.

## 10. Acceptance Criteria

- [x] Lore, silhouette, skills and VFX express the same water-dragon healer fantasy
- [x] Strength and weakness are defined for auto battle
- [x] All approved mechanics exist in runtime, not only in description
- [x] All animation/VFX has a gameplay purpose
- [x] Existing save, roster and combat behavior remain compatible
