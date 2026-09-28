# Ignis - Kẻ Giữ Tàn Tro

Ví dụ chuẩn cho quy trình nhân vật. Đây là design contract, chưa tự động đổi tên production asset hoặc save.

## 1. Identity

- Internal ID hiện tại: `Nova`
- Display name đề xuất: `Ignis`
- Title: `Kẻ Giữ Tàn Tro`
- Integration mode: `replace-existing`
- Existing slot: `Fire God Heavenly Demon`, internal `PrototypeSkillKit.Nova`
- Species: Human
- Class / formation: Mage / Middle
- Rarity: SSR
- Element / damage type: Fire
- One-line fantasy: Pháp sư sống sót mang một lò phản ứng Hỏa Long trong cơ thể, tích Nhiệt và gieo Tro lên kẻ địch để kích nổ chiến trường.
- Character philosophy: “Lửa không sinh ra để hủy diệt, nó sinh ra để thiêu rụi những gì đã chết, nhường chỗ cho mầm sống mới.”
- Combat loop: Gieo Ash -> tích Heat -> tạo Magma -> kích nổ/biến đổi bằng Crimson Gale.
- Strength: AoE theo thời gian, combo zone, snowball khi nhiều kẻ địch đang Burning.
- Weakness: Cần thời gian dựng Ash/Heat; sức mạnh giảm khi đổi mục tiêu liên tục hoặc bị hạ trước khi combo hoàn tất.

Giữ filename, GUID, resource path và internal kit `Nova` cho tới khi có quyết định migration display name riêng. Không tạo thêm một hero Ignis trùng slot.

## 2. Lore canon

Ignis sinh ra tại Solaria, thánh địa của các pháp sư ngọn lửa thượng cổ. Ngọn lửa từng được tôn thờ như sự sống và nuôi dưỡng những lò rèn ma thuật vĩ đại nhất lục địa. Sự ngạo mạn khiến người Solaria trói buộc Đại Hỏa Long Prometheus dưới lòng đất để rút ma lực và biến lửa thuần khiết thành vũ khí.

Năm Ignis mười sáu tuổi, ma pháp trận vỡ. Dung nham nuốt trọn Solaria và biến thành phố thành vùng đất chết phủ tro. Gia đình cùng tộc nhân của anh bị thiêu rụi. Ngọn lửa diệt vong không giết Ignis mà chảy vào mạch máu, để lại vệt sẹo đỏ rực trên ngực như dấu ấn phẫn nộ của Hỏa Long.

Ignis sống sót với cơ thể như một lò phản ứng ma thuật, liên tục tích tụ Nhiệt Lượng và Tro Tàn. Anh lang thang qua các tàn tích, chịu ngọn lửa thiêu đốt cả thể xác lẫn ký ức, nhưng tin rằng lửa chỉ nên thiêu rụi cái đã chết để mở đường cho sự sống mới.

## 3. Visual contract

- Silhouette: pháp sư cao, mảnh; tóc và áo choàng cháy xé sang trái; tay trước nâng một lõi than hồng; ngực có khe sáng như lò nung nứt vỡ.
- Hair: đen than, ngọn tóc đỏ sẫm, 2-4 lọn cam đỏ như dung nham dưới lớp tro.
- Outfit: trường bào đen cháy sém, gấu áo rách; lớp lót đỏ thẫm; giáp vai/lưng gợi hình vảy Hỏa Long nhưng không biến thành full armor.
- Story accessories: vết sẹo lò nung trên ngực; mảnh xiềng ma pháp Solaria quấn ở thắt lưng hoặc cổ tay.
- Raised-hand magic: một “tàn tâm” gồm lõi trắng-hồng, lửa đỏ và các hạt tro bay lên.
- Main dark: than `#0A0204`, nâu đen, xám tro.
- Fire accent: `#8C0A14` -> `#D21E28` -> cam đỏ.
- Glow: đỏ tươi -> cam vàng -> trắng hồng; vùng trắng dưới 5% sprite.
- Metal: bạc xỉn/xương cháy.
- Outline: đỏ đen `#0A0204`.
- Secondary light: lõi lửa chiếu lên bàn tay, ống tay và một bên mặt; sẹo ngực sáng yếu hơn lõi trên tay.
- Avoid: chiến binh giáp nặng, demon thuần túy, lửa vàng vui tươi, đầu chibi, vũ khí quá lớn, tóc sạch không có tro/cháy sém.

Prompt nền và checklist chi tiết nằm tại `docs/art/character-sprite-style-guide.md`.

## 4. Gameplay contract hiện có

Giữ stat hiện tại của slot để pipeline nhân vật không vô tình trở thành balance pass:

| Field | Value |
| --- | ---: |
| Max Health | 170 |
| Attack | 27 |
| Defense | 8 |
| Move Speed | 2.1 |
| Attack Interval | 0.95 |
| Attack Range | 4.0 (Mage rule) |

Kit canon:

- Basic - Flame Strike: Fire hit gieo Ash; đánh mục tiêu đủ 3 Ash sẽ tiêu thụ để splash và nổ trễ.
- Passive 1 - Ignition Core: direct Fire hit tích tối đa 3 Ash; mỗi stack giảm 5% Fire Resistance.
- Passive 2 - Thermal Resonance: đánh Burning/Ash detonation tích Heat, hồi energy và cường hóa Fire/move speed; máu nguy cấp kích Flame Shield.
- Passive 3 - Everburning Embers: Fire DoT kill truyền 2 Ash và một phần DoT còn lại sang mục tiêu gần.
- Active - Hellfire Impact: Fire AoE + knock-up, để lại Magma Pool; đủ Ash kích True Damage và slow giảm dần.
- Ultimate - Crimson Gale: sóng lửa hình nón tiêu Ash theo missing health, knockback, để Scorch và biến Magma thành Firestorm.

Thông số và acceptance đầy đủ: `../../../../docs/superpowers/specs/2026-09-27-fire-god-heavenly-demon-redesign.md`.

## 5. Animation contract hiện có

Slot Ignis/Nova dùng ngoại lệ canvas `68x68`, source 10 FPS:

| State | Frames | Runtime timing |
| --- | ---: | --- |
| Idle | 8 | 10 FPS loop |
| Run | 13 | 10 FPS loop |
| Basic | 8 | 0.56s, hit khoảng 0.18s |
| Active | 13 | 1.3s, impact frame 9 |
| Ultimate | 13 | 1.3s, release frame 9 |
| Hit | 9 | 10 FPS |
| Death | 9 | 10 FPS, sau đó remove |

Khi vẽ lại Ignis, giữ frame count, canvas, pivot, foot line và runtime timing ở pass đầu. Đổi timing chỉ khi test chứng minh contact/release không khớp.

## 6. VFX language

- Ash: tàn lửa nhỏ quay quanh + count; stack 3 có sigil sáng.
- Heat: aura cam đỏ tăng theo stack nhưng không sáng hơn impact.
- Flame Shield: barrier lửa + shockwave ngắn.
- Hellfire: lõi rơi, blast tròn, mặt đất nứt và Magma Pool.
- Crimson Gale: cone/crescent đỏ-tím-trắng, ember streak, residue cháy thấp.
- Firestorm: cùng ngôn ngữ Magma nhưng bán kính và mật độ ember lớn hơn; tick rate không đổi.
- Everburning: homing ember nhỏ có arc, không dùng projectile khổng lồ.

Ưu tiên tái sử dụng asset tại `Assets/Resources/VFX/FireGod/` và helper hiện có. Chỉ đổi tên/move folder khi có migration asset rõ ràng; hiện tại không cần.

## 7. Acceptance cho bản identity Ignis

- Display/lore/art thống nhất chủ đề “tro tàn tái sinh”, không chỉ là pháp sư lửa chung chung.
- Vết sẹo, tro, xiềng Solaria và lửa Hỏa Long đọc được trong idle/key pose.
- Gameplay vẫn dùng đầy đủ Ash, Heat, Magma, Scorch và Firestorm hiện có.
- Không mất save, ownership, squad placement, GUID hoặc frame reference của slot Nova.
- Full Play Mode suite vẫn pass và battle không có global pause/hit-stop mới.
