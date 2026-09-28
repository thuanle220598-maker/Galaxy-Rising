# Project Character Contract

Tài liệu này mô tả cách nhân vật đang chạy trong project. Nếu code thay đổi, ưu tiên code và cập nhật lại tài liệu này trong cùng change.

## Runtime hiện tại

- Unity `6000.3.25f1`, URP 2D, portrait `360x640`.
- Scene chính: `Assets/Scenes/SampleScene.unity`.
- Combat là auto battle 5v5, free movement, target-relative attack range.
- Không có lane, center divider, ranged kiting, Ultimate Cut-In hoặc global hit-stop.
- UI và nhiều VFX được tạo bằng code; không thêm framework chỉ để tạo một nhân vật.

## Nguồn dữ liệu

| Nội dung | Nguồn chuẩn |
| --- | --- |
| Identity, class, rarity, stats, sprite arrays | `Assets/Scripts/CombatantDefinition.cs` + combatant `.asset` |
| Basic/passive/active/ultimate metadata | `Assets/Scripts/SkillDefinition.cs` + skill `.asset` |
| Tạo lại ScriptableObject | `Assets/Editor/PrototypeContentGenerator.cs` |
| Hành vi skill/combat/status | `Assets/Scripts/CombatPrototype.cs` |
| Placeholder pixel art | `Assets/Scripts/PrototypePixelArt.cs`, `PrototypeHeroPixelArt64.cs` |
| Roster và summon | `Assets/Scripts/PrototypeGacha.cs` |
| Progression và save migration | `PrototypeProgression.cs`, `PrototypeSaveSystem.cs` |
| Regression tests | `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs` |

`ScriptableObject` là data source. Text trong hồ sơ thiết kế không tự tạo gameplay.

## Enum và class rules

- Species hiện có: Unknown, Human, Dragon, Cosmic, Machine, Ocean, Fantasy.
- Class hiện có: Tanker, Fighter, Assassin, Mage, Archer, Support.
- Rarity hiện có: R, SR, SSR, UR.
- Formation được suy ra từ class:
  - Tanker/Fighter -> Front
  - Assassin/Mage -> Middle
  - Archer/Support -> Back
- Attack range được suy ra từ class; ví dụ Mage là `4.0`, Archer `4.4`, Support `3.6`.

Không thêm field formation/range riêng cho từng hero khi class rule đã biểu đạt đúng.

## Skill data

Mỗi `PrototypeSkillData` có:

- display name và description;
- cooldown;
- action duration;
- normalized hit frame;
- power multiplier;
- energy gain.

`SkillDefinition` có Basic, Passive 1, Passive 2 tùy chọn, Passive 3 tùy chọn, Active và Ultimate. Logic phức tạp hiện vẫn định tuyến theo `PrototypeSkillKit` trong `CombatPrototype.cs`, vì vậy thêm kit mới phải tìm toàn bộ caller trước khi sửa enum.

## Art và animation

- Thứ tự ưu tiên hình ảnh: ref được người dùng duyệt -> production asset đã duyệt -> style guide -> suy luận từ lore. Yêu cầu kỹ thuật import vẫn luôn áp dụng.
- Ref có thể nhỏ hơn canvas production. Giữ ngôn ngữ pixel của ref và chuẩn hóa output bằng nearest-neighbor, Scale2x hoặc redraw có kiểm soát; không upscale làm mờ.
- Lưu ref gốc trong `Previews/` hoặc `Assets/Art/Characters/<InternalId>/Source/`; không ghi đè ref khi sinh output.
- Generator deterministic đã có là source ưu tiên để sửa và tái tạo asset. Không thay bằng dependency/image pipeline mới nếu source đó đủ dùng.
- Chuẩn production mới là `64x64`; Fire Mage/Nova hiện tại là ngoại lệ `68x68` vì source PXO cũ.
- Import: Sprite, Point filter, Compression None, mipmaps off, alpha transparent, PPU bằng chiều rộng frame.
- Runtime nhận arrays: Idle, Run, Attack, Skill, Ultimate, Hit, Death.
- `sourceFacesLeft` chỉ dùng khi source gốc quay trái; không flip file thủ công nếu runtime đã xử lý.
- Foot line, pivot và canvas phải giống nhau giữa mọi state.

## Quy ước đường dẫn

```text
docs/characters/<internal-id>.md
Assets/Art/Characters/<InternalId>/
Assets/Art/Characters/<InternalId>/Source/
Assets/Art/Characters/<InternalId>/Combat/
Assets/Resources/VFX/<InternalId>/
Assets/Resources/Combatants/Allies/<InternalId>.asset
Assets/Resources/Combatants/Roster/<InternalId>.asset
Assets/Resources/Skills/<InternalId>.asset
```

Tên hiển thị có thể có dấu và khoảng trắng. `InternalId`, enum, file và folder dùng ASCII/PascalCase, ổn định sau khi phát hành.

## Hai chế độ tích hợp

### Replace existing

Dùng khi nhân vật mới thay identity/kit của một slot hiện tại.

- Giữ internal kit, asset filenames, GUID và resource paths.
- Cập nhật display name, art, skill data và runtime behavior tại chỗ.
- Nếu display name nằm trong save, tăng save version một lần và merge dữ liệu trùng bằng giá trị tiến triển cao nhất.
- Không tạo hero thứ hai chỉ vì tên thiết kế đổi.

### New slot

Dùng khi roster cần thêm một nhân vật độc lập.

- Thêm `PrototypeSkillKit` mới và tìm mọi caller của enum.
- Thêm combatant/skill generation, art binding và runtime skill path.
- Quyết định Allies hoặc Roster theo cách mở khóa; cập nhật gacha/starter chỉ khi thiết kế yêu cầu.
- Kiểm tra vòng lặp dựa vào enum range, switch, màu/VFX/audio mapping và placeholder support.
- Thêm test xác nhận data load, identity, frames và mechanic riêng.

## Baseline cân bằng

Không có formula cân bằng tự động. Chọn 2-3 hero cùng class/rarity từ `Assets/Resources/Combatants`, so:

- effective survivability: HP và Defense;
- damage cadence: Attack, AttackInterval, multiplier và hit count;
- control/utility uptime;
- range và movement speed;
- thời gian lên Ultimate và combo setup.

Một kit nhiều utility hoặc AoE phải trả giá bằng damage đơn mục tiêu, độ bền, thời gian setup hoặc cooldown.

## Giới hạn kỹ thuật cần giữ

- Không hard-code hero data vào UI.
- Không tạo status/damage path thứ hai nếu shared path có thể mở rộng.
- Không thêm package hoặc scene riêng cho character pipeline.
- Không sửa file generated trong `Library/PackageCache`.
- Không thay GUID bằng cách xóa và tạo lại asset production.
- Không coi preview đẹp là hoàn tất nếu battle timing hoặc gameplay mechanic chưa chạy.
