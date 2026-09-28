# Galaxy Rising

## Current phase: P1.1 - Fire God Heavenly Demon

### Aurelia character replacement

- `Astra` được thay identity tại chỗ bằng `Aurelia`; enum, resource filenames và GUID vẫn giữ `Astra`.
- Save version 5 migrate progression, ownership và squad từ `Astra` sang `Aurelia`.
- Aurelia dùng 7 imported animation sheets 64x64 và 10 water/dragon VFX sheets sinh deterministic từ `Previews/Aurelia_x8.png`.
- Runtime kit gồm Hydro Beads, Tidal Cleansing, Dragon Pulse Aura, Dragon Realm và Draconian Aegis.

### Kronos character replacement

- `Brakk` được thay identity tại chỗ bằng `Kronos`; enum, resource filenames và GUID vẫn giữ `Brakk`.
- Save version 6 migrate progression, ownership và squad từ `Brakk` sang `Kronos`.
- Kronos dùng 7 imported animation sheets 64x64 và 12 cosmic-void VFX sheets sinh deterministic từ `Tools/Generate-KronosSprite.py`.
- Runtime kit gồm Event Horizon, Gravitational Crust, Void Parasite, Devourer's Constitution, Singularity Pull và Cosmic Leviathan.

Nova is being replaced in-place by `Fire God Heavenly Demon`, an SSR Human Fire Mage. The internal kit enum, Nova resource filenames and GUIDs remain unchanged so existing references stay compatible.

Combat loop:

- Direct Fire attacks build up to three source-owned Ash Marks; each stack reduces Fire Resistance by 5%.
- A Basic Attack against three Ash consumes them for immediate splash and a delayed Scorch Burst.
- Fire damage against Burning targets or Ash detonations builds Heat, restores energy, increases movement speed and increases Fire damage.
- At 25% HP, Flame Shield scales from current Heat and knocks nearby enemies back; battle cooldown is 90 seconds.
- Fire DoT kills spread two Ash and half of the remaining tracked DoT to nearby enemies.
- `Hellfire Impact` knocks enemies up and creates an eight-tick slowing Magma Pool; three Ash trigger True Damage and a decaying slow.
- `Crimson Gale` is a directional cone that consumes Ash for missing-health damage, applies knockback, leaves Grounded Scorch, and converts intersected Magma into double-radius Firestorm.

Animation source and regeneration:

- Source: `Assets/Art/Characters/Nova/Source/Nova.pxo`, canvas `68 x 68`, 10 FPS.
- Runtime mapping: Idle 8, Run 13, Basic 8, Active 13, Ultimate 13, Hit 9, Death 9. Walk 8 is exported but not used by the current one-state movement system.
- Basic retains `NovaFlameBasicAttack.png`; confirm its source license before release.
- Regenerate sheets with `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Export-FireGodPxo.ps1`.

Fire VFX pipeline:

- Pixelorama 1.2 and FFmpeg 9 are the authoring/review tools; generated sources live under `Assets/Resources/VFX/FireGod`.
- `Tools/Generate-FireGodVfx.py` deterministically builds 25 point-filtered sheets for Basic/Ash, Hellfire/Magma, Crimson Gale/Scorch/Firestorm, Heat, Flame Shield and Everburning Embers.
- Crimson Gale combines five 48-frame, 30 FPS spectacle sheets with two continuous URP procedural-fire layers. Noise-warped violet, magenta and white flame now flows at the display frame rate while the demon silhouette, flash, sparks and residue keep their authored timing.
- All 25 Fire God sheets keep their authored sprite timing and use sprite alpha as a mask for continuous procedural fire. Ordinary effects share one material and use per-renderer profile values for projectiles, ash, ground fire, shields and aura.
- `Tools/Test-FireGodVfx.py` validates frame counts, dimensions, alpha, palette diversity and intended visual-center movement.
- `Tools/Build-FireGodVfxPreview.py` creates the review frames for `Previews/FireGodVfxPreview.mp4` and `Previews/FireGodCrimsonGaleSpectacle.mp4`.
- `Tools/Build-FireGodAllProceduralPreview.py` encodes the complete Unity-rendered kit review at `Previews/FireGodAllProceduralVfx.mp4`.

Compatibility and constraints:

- Save version 4 migrates progression, ownership and squad entries from `Nova` to `Fire God Heavenly Demon`, merging duplicate progression records by maximum value.
- Combat remains free movement with target-relative ranges. There are no lanes, center divider, ranged kiting, Ultimate Cut-In, global hit-stop or global action pause.
- Final Unity import completed successfully: `42/42` Play Mode tests passed with no C# or procedural-fire shader compiler errors.

References:

- `docs/superpowers/specs/2026-09-27-fire-god-heavenly-demon-redesign.md`
- `docs/superpowers/plans/2026-09-27-fire-god-heavenly-demon.md`
- `docs/superpowers/specs/2026-09-27-fire-god-spectacle-vfx-redesign.md`
- `docs/superpowers/plans/2026-09-27-fire-god-spectacle-vfx-redesign.md`

Tài liệu này là nguồn bàn giao chính của project. Khi chuyển sang máy khác hoặc bắt đầu một phiên làm việc mới, hãy đọc toàn bộ file trước khi sửa code.

## 1. Mục tiêu hiện tại

Galaxy Rising là prototype game idle RPG màn hình dọc với chiến đấu tự động 5v5.

Mục tiêu gần nhất không phải thêm nhiều hệ thống mới. Mục tiêu là biến các cơ chế đã có thành một vertical slice khoảng 10 phút, đủ rõ ràng và chỉn chu để người ngoài team có thể tự chơi.

Vòng lặp mục tiêu:

```text
Auto battle -> nhận thưởng -> nâng tướng -> đánh boss
            -> thua -> được gợi ý tăng lực chiến -> đánh lại
```

## Previous phase: P1.0 - Combat Animation & Ultimate Spectacle

P1.0 turns the working 5v5 combat into a clearer spectacle without pausing the simulation or changing combat rules.

Scope:

- Ultimate activation stays inside the battlefield with its character animation, timed impacts, VFX and audio; no cut-in overlay is created.
- Multi-hit and multi-target ultimates resolve through short timed sequences instead of landing in one frame.
- Ranged basic attacks show projectile travel; movement and actions can leave short bounded afterimages.
- Combatants have ground shadows, Y-based sorting, green healing numbers and an ultimate-ready marker.
- Battle HUD has native `SPEED x1/x2` and `PAUSE/RESUME` controls; only manual pause sets `Time.timeScale` to zero.
- Impact flashes use unscaled UI time. There is no global hit-stop, Timeline, Spine, shader package or new save field.
- Existing free movement, target-relative attack range, 5v5 rules, on-character status icons and monster identity remain unchanged.
- Nova's Basic Attack uses the imported eight-frame `NovaFlameBasicAttack` sheet at `68 x 68`; confirm the source asset license before release.

P0.9 remains the completed PvE monster identity baseline.
Automated validation: `42/42` Play Mode tests passed for the complete Fire God Heavenly Demon implementation and dedicated Fire VFX set, including PXO animation, procedural sprite masking, Ash/Heat/Fire-zone mechanics, save migration, timed-hit, battle-control and free-movement regressions.

Design and implementation references:

- `docs/superpowers/specs/2026-09-26-combat-animation-ultimate-spectacle-design.md`
- `docs/superpowers/plans/2026-09-26-combat-animation-ultimate-spectacle.md`

Latest handoff prompt:

```text
Read all of README.md. P0.1-P0.9 are complete. P1.0 is the current phase:
do not restore the Ultimate Cut-In; preserve free movement and target-relative attack range,
and do not restore the center divider, combat roster panel, ranged kiting or global hit-stop.
Use native Unity coroutines and code-native effects; do not add an animation framework,
package, shader dependency or save field unless production assets later require one.
```

Reference chính:

- Video: [Tam Quốc Thú Hoá - Full Code Chung Trải Nghiệm Game Tam Quốc Phong Cách Hoang Dã](https://www.youtube.com/watch?v=MpjNab5cYSE)
- Kênh: AQGaming
- Thời lượng đã phân tích: 26:18
- Chỉ tham khảo vòng lặp, cách trình bày và dẫn dắt người chơi; không sao chép chủ đề, hình ảnh hoặc hệ thống nạp tiền.

Commit nền gần nhất:

```text
11d433c Build data-driven idle RPG MVP
```

## 2. Môi trường

- Unity: `6000.3.25f1`
- Render pipeline: URP 2D
- Input: Unity Input System
- Scene chạy chính: `Assets/Scenes/SampleScene.unity`
- Tỉ lệ thiết kế: portrait `360 x 640`
- Portrait được bật; landscape và portrait upside-down bị tắt.

Các package cần thiết đã nằm trong `Packages/manifest.json`. Không cài thêm package nếu chức năng hiện tại của Unity hoặc package đã có đáp ứng được yêu cầu.

## 3. Mở project trên máy mới

1. Cài Unity Hub.
2. Cài đúng Unity Editor `6000.3.25f1`.
3. Nếu cần build Android, cài thêm Android Build Support, SDK, NDK và OpenJDK từ Unity Hub.
4. Clone hoặc copy toàn bộ repository.
5. Trong Unity Hub, chọn `Add project from disk` và trỏ tới thư mục `Galaxy-Rising`.
6. Chờ Unity import xong toàn bộ asset.
7. Mở `Assets/Scenes/SampleScene.unity`.
8. Nhấn Play.

Game được bootstrap bằng `RuntimeInitializeOnLoadMethod`, vì vậy scene không cần chứa sẵn object gameplay. Khi Editor mở project, `PrototypeContentGenerator` cũng tự tạo lại các ScriptableObject còn thiếu.

Không commit các thư mục Unity sinh tự động như `Library`, `Temp`, `Logs` hoặc `UserSettings`.

## 4. Trạng thái gameplay hiện tại

Đã có:

- Chiến đấu tự động 5v5.
- Đội hình gồm năm tướng và ba hàng `Front`, `Middle`, `Back`.
- Đánh ải idle, farm wave và boss mỗi năm ải.
- 20 stage, gồm bốn boss stage.
- Offline reward, giới hạn tối đa tám giờ.
- Ba dungeon: Credits, Experience và Materials.
- PvP 5v5 dạng prototype, chưa có ranking hoặc server reward.
- Nâng cấp tướng: level, sao, bốn skill và ba slot trang bị.
- Gacha một hoặc mười lượt, rarity rate, duplicate shard, lịch sử và pity SSR/UR.
- Năm starter hero; các nhân vật còn lại mở qua gacha.
- Onboarding sáu bước cho người chơi mới: auto battle, claim, train, boss, summon và formation.
- Save version 4 với migration tên Nova sang Fire God Heavenly Demon.
- Placeholder pixel art, animation, VFX và âm thanh được tạo bằng code.
- Soak test 100 lần chuyển battle/farm.

Các màn hình hiện có:

- Battle/Idle
- Squad/Formation
- Hero Development
- Summon
- Activities: Dungeon và PvP

## 5. Cách chơi và kiểm tra nhanh

Sau khi nhấn Play:

1. Trận idle tự chạy.
2. Dùng `CHALLENGE STAGE` để đánh ải tiếp theo khi đang farm.
3. Dùng thanh dưới để mở `SQUAD`, `HEROES`, `SUMMON`, `CLAIM` và `MODES`.
4. Boss stage thắng sẽ tăng stage; thua sẽ quay về farm stage trước.
5. Dùng dungeon để lấy tài nguyên nâng skill, level và trang bị.

Reset toàn bộ dữ liệu prototype:

1. Mở màn `SUMMON`.
2. Nhấn `DEV RESET`.
3. Nhấn `CONFIRM RESET`.

Reset sẽ xoá các key `Prototype.*` do game quản lý, sau đó tạo lại save version hiện tại.

Chạy soak test:

1. Khởi động Unity Editor với tham số `-prototypeSoak`.
2. Mở project và nhấn Play.
3. Kiểm tra Console.
4. Thành công khi thấy `SOAK PASSED: 100 battle/farm transitions`.

## 6. Kiến trúc và file quan trọng

### Runtime

- `Assets/Scripts/CombatPrototype.cs`
  - Bootstrap game sau khi scene load.
  - Quản lý session, stage, idle reward và squad save.
  - Chứa battle simulation, combatant, target selection, skill execution, feedback và HUD cũ.
  - Đây là file monolith khoảng 3.286 dòng. Không viết lại toàn bộ; chỉ tách phần đang thực sự cần sửa.

- `Assets/Scripts/PrototypeGameFlow.cs`
  - Tạo Canvas và các màn hình runtime bằng code.
  - Toàn bộ UI đang dựa trên reference resolution `360 x 640` và nhiều `Rect` cố định.
  - Đây là nơi chính cần thay đổi khi làm lại Battle/Home UI.

- `Assets/Scripts/PrototypeProgression.cs`
  - Level, XP, star, shard, skill level và equipment level.
  - Lưu progression bằng JSON trong PlayerPrefs.

- `Assets/Scripts/PrototypeGacha.cs`
  - Ownership, ticket, rarity roll, duplicate shard, pity và history.
  - Starter: Fire God Heavenly Demon, Ion, Aurelia, Lyra và Kronos.

- `Assets/Scripts/PrototypeSaveSystem.cs`
  - Save version hiện tại: 6.
  - Save cũ tự bỏ qua onboarding; save mới hoặc `DEV RESET` bắt đầu từ bước đầu tiên.
  - Migration và reset các PlayerPrefs key của prototype.

- `Assets/Scripts/PrototypePixelArt.cs`
  - Sinh sprite và animation placeholder bằng code.
  - Không coi đây là art pipeline production.

- `Assets/Scripts/PrototypeSoakTest.cs`
  - Kiểm tra 100 lần chuyển battle/farm và số lượng `PrototypeBattle` còn hợp lệ.

### Data

- `Assets/Resources/Combatants`: combatant definitions.
- `Assets/Resources/Skills`: bốn skill cho mỗi skill kit.
- `Assets/Resources/Stages`: dữ liệu 20 stage.
- `Assets/Resources/Dungeons`: dữ liệu ba dungeon.
- `Assets/Editor/PrototypeContentGenerator.cs`: tự tạo content còn thiếu khi Editor load.

ScriptableObject là nguồn dữ liệu gameplay. Không hard-code thêm stage, dungeon hoặc hero mới vào UI.

## 7. Kết quả phân tích game tham khảo

Game trong video dùng vòng lặp idle RPG quen thuộc:

1. Auto battle chạy liên tục trên màn hình chính.
2. Người chơi nhận idle reward.
3. Tăng level, sao, trang bị hoặc hệ thống phụ để tăng lực chiến.
4. Thử lại map, tower, boss hoặc PvP.
5. Khi bị kẹt, UI chỉ rõ điều kiện và nơi cần nâng cấp.

Điểm mạnh cần học:

- Chủ đề và nhân vật có nhận diện rõ ngay từ màn hình đầu.
- Sân đấu chiếm phần lớn màn hình.
- Stage, boss HP, lực chiến và phần thưởng kế tiếp luôn dễ thấy.
- Hero portrait, energy và trạng thái chiến đấu dễ đọc hơn danh sách chữ.
- Red dot và điều kiện mở khoá dẫn người chơi tới hành động tiếp theo.
- Khi thua, game tạo một đường quay lại progression thay vì để người chơi tự đoán.
- Thao tác phù hợp màn hình dọc và phiên chơi ngắn.

Điểm không nên sao chép ở giai đoạn này:

- VIP, tích nạp và nhiều gói mua.
- Quá nhiều event hoặc biểu tượng cùng lúc.
- Mount, fashion, profession, talisman và các lớp tăng lực chiến chồng lên nhau.
- Đua top, guild hoặc PvP server trước khi core loop được kiểm chứng.

## 8. Khoảng cách của project hiện tại

Project không thiếu hệ thống lõi. Khoảng cách lớn nhất là presentation và player guidance.

- Battle screen đang hiển thị danh sách ally/enemy và một khối mô tả skill dài, mang tính debug.
- Hero, Squad và Summon hoạt động nhưng chủ yếu là text và button.
- Chưa có một chỉ số `Team Power` để người chơi hiểu sức mạnh hiện tại.
- Chưa có hướng dẫn phiên đầu hoặc gợi ý sau khi thua.
- Chưa có mục tiêu ngắn hạn rõ như reward mốc ải hoặc nhiệm vụ đầu game.
- Art hiện là placeholder nên chưa tạo được nhận diện sản phẩm.
- Save bằng nhiều PlayerPrefs phù hợp prototype nhưng chưa đủ an toàn cho production.
- Test hiện mới tập trung vào chuyển trận, chưa bao phủ save, progression và gacha.

## 9. Roadmap đã thống nhất

### P0 - Vertical slice có thể đưa người khác chơi

Đây là công việc tiếp theo.

1. Làm lại màn Battle/Home nhưng giữ nguyên combat logic.
2. Bố cục mục tiêu:
   - Thanh tài nguyên và stage ở trên cùng.
   - Stage progress hoặc boss HP rõ ràng.
   - Sân đấu chiếm phần lớn màn hình.
   - Năm hero portrait với HP và energy ở dưới sân đấu.
   - `Challenge Boss`, `Claim` và navigation có ưu tiên thị giác rõ.
3. Chuyển mô tả skill chi tiết khỏi battle screen; chỉ giữ thông tin chiến đấu cần thiết.
4. Thêm onboarding tối thiểu:
   - Xem auto battle.
   - Claim reward.
   - Nâng một hero.
   - Challenge boss.
   - Summon.
   - Chỉnh formation.
5. Thêm `Team Power` và gợi ý hành động sau khi thua.
6. Chốt một art slice: năm hero, một boss, một background, skill VFX và audio nhất quán.
7. Kiểm tra portrait và safe area trên `360 x 640`, `720 x 1280` và `1080 x 1920`.

Tiêu chí hoàn thành P0:

- Người chơi mới tự hoàn thành chuỗi đầu game mà không cần giải thích ngoài game.
- Người chơi luôn nhìn thấy mục tiêu hoặc hành động tiếp theo.
- Không còn khối debug text che phần lớn battle screen.
- Stage 1-10 có nhịp tăng sức mạnh hợp lý; stage 5 và 10 là boss rõ ràng.
- Đóng và mở lại game không mất progression và nhận offline reward đúng.
- Soak test vẫn pass 100 lần chuyển trận.
- Không có lỗi UI hoặc vùng bấm quá nhỏ trên ba độ phân giải mục tiêu.

### P1 - Retention tối thiểu sau khi P0 chơi tốt

- Ba daily quest đơn giản.
- Reward theo mốc stage.
- Đăng nhập bảy ngày.
- Thêm khoảng một chapter, một boss và hai hoặc ba hero; không mở rộng roster hàng loạt.
- Thêm test nhỏ cho save migration, progression purchase và gacha pity.
- Tách UI/presentation khỏi `CombatPrototype.cs` khi phần được tách đang cần chỉnh sửa.

### P2 - Chuẩn bị public test

- Gom dữ liệu save thành một model có version và backup rõ ràng.
- Balance economy bằng dữ liệu thay vì sửa trực tiếp logic runtime.
- Analytics tối thiểu cho tutorial completion, stage fail và session return.
- Build và smoke test trên thiết bị Android thật.
- Chỉ xem xét backend, leaderboard, purchase hoặc live event khi external playtest chứng minh cần thiết.

## 10. Không làm lúc này

- Không viết lại toàn bộ `CombatPrototype.cs`.
- Không thêm framework UI hoặc dependency mới chỉ để thay vài màn hình.
- Không làm guild, ranked PvP, chat, backend hoặc live ops.
- Không làm VIP, IAP và gói nạp.
- Không thêm mount, fashion, profession hoặc nhiều hệ progression phụ.
- Không tạo hàng trăm stage hoặc hàng chục hero placeholder.

## 11. Quy tắc khi tiếp tục phát triển

- Đọc flow đang chạy từ đầu tới cuối trước khi sửa.
- Giữ combat logic hiện tại trong P0, ưu tiên thay presentation.
- Tái sử dụng ScriptableObject và API đang có.
- Chỉ tách class khi phần đó thực sự đang được thay đổi.
- Mỗi thay đổi gameplay có ít nhất một kiểm tra chạy được.
- Không xoá hoặc revert thay đổi lạ trong worktree nếu chưa xác nhận nguồn gốc.
- Trước khi tuyên bố hoàn thành, chạy Play Mode smoke test và kiểm tra Console.

## 12. Trạng thái worktree khi bàn giao

Ghi nhận ngày `2026-09-26` trước khi thêm tài liệu này:

```text
 M ProjectSettings/EditorBuildSettings.asset
 M ProjectSettings/ProjectSettings.asset
?? .vsconfig
?? Galaxy-Rising.slnx
```

Ý nghĩa:

- `ProjectSettings/ProjectSettings.asset`: thay đổi portrait có chủ đích.
- `ProjectSettings/EditorBuildSettings.asset`: thay đổi sinh ra trong quá trình cấu hình Unity; kiểm tra diff trước khi commit.
- `.vsconfig` và `Galaxy-Rising.slnx`: file hỗ trợ IDE được Unity/Visual Studio sinh ra.

Không tự động revert các file trên.

## 13. Điểm bắt đầu cho phiên làm việc tiếp theo

P0.1 đã được triển khai trong `PrototypeGameFlow.cs`:

- Battle/Home có hierarchy `stage/resources -> battlefield -> five hero cards -> primary action -> navigation`.
- Đã bỏ danh sách enemy và khối mô tả skill dài khỏi battle screen.
- Combat, progression, gacha, save và callback điều hướng được giữ nguyên.
- Play Mode regression test nằm tại `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`.

P0.2 đã được triển khai:

- Banner không chặn thao tác luôn hiển thị hành động onboarding tiếp theo.
- Tiến độ sáu bước lưu trong `Prototype.OnboardingStep`.
- Chỉ hành động thành công mới tăng bước; vào boss tự động cũng hoàn thành bước challenge.
- Save version 2 được migrate thành đã hoàn thành onboarding; `DEV RESET` tạo flow người chơi mới.

P0.3 đã được triển khai:

- Battle/Home và Squad hiển thị `Team Power` từ chỉ số và skill level hiện tại.
- Khi thua, màn kết quả giữ lại gợi ý ngắn và điều hướng trực tiếp tới `HEROES` hoặc `SQUAD`.
- Power chỉ là chỉ số UI heuristic; chưa ảnh hưởng combat và không thêm save field.

P0.4 đã được triển khai:

- Năm hero khởi đầu có pixel signature riêng theo skill kit; boss có silhouette, aura và crown riêng.
- Arena dùng starfield, nebula và battle deck nhiều lớp thay cho nền phẳng.
- Skill VFX dùng core/ray nhiều lớp; battle có loop chiptune cùng hit/skill/ultimate SFX procedural.
- Art slice vẫn code-native để không thêm pipeline import/slicing trước khi chốt asset production.

P0.5 đã được triển khai:

- UI chạy trong `Safe Area -> Portrait Content 360 x 640` và tự co vừa vùng notch/cutout.
- Safe-area math được kiểm tra cho `360 x 640`, `720 x 1280` và `1080 x 1920`.
- Squad/Heroes/Result/Activities/Gacha dùng touch target chính tối thiểu 44 px; text bật best-fit.

P0.6 đã được triển khai:

```text
Stage 1-10 được kiểm tra tự động: hệ số địch tăng đều, boss ở stage 5/10,
reward boss gấp đôi nhịp reward thường và mỗi boss cho một summon ticket.
Save stage/tài nguyên/đội hình, offline reward giới hạn tám giờ và soak 100 lần
chuyển battle/farm đều nằm trong cùng Play Mode regression suite.
```

P0.7 đã được triển khai:

- UI dùng chung phong cách pixel sci-fi command deck với frame, accent rail, shadow và trạng thái button rõ ràng.
- Battle/Home có resource chip riêng, năm hero card dùng portrait thật cùng thanh HP/energy và selected state.
- Squad, Heroes, Summon, Activities, onboarding và result panel dùng cùng visual language.
- Toàn bộ presentation vẫn code-native, không thêm package, asset pipeline hoặc thay đổi gameplay/save.

P0.1-P0.8 đã hoàn thành. P0.9 tách nhận diện kẻ địch theo mode: Idle/Dungeon chỉ dùng
quái vật riêng, PvP tiếp tục dùng đội hình hero có thể summon. Sau khi kiểm tra hình ảnh
sáu quái trong Unity, bước tiếp theo là external playtest cho vertical slice.
