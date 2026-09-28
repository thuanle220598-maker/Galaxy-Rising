# Kronos - Tàn Tích Của Tinh Vân

## 1. Identity

- Internal ID: `Kronos`
- Display name: `Kronos`
- Title: `Tàn Tích Của Tinh Vân`
- Species: `Cosmic`
- Integration mode: `replace-existing`
- Existing slot replaced: `Brakk` (giữ enum, resource path và GUID cũ)
- Class / formation: `Tanker / Front`
- Rarity: giữ rarity của Brakk
- Element / damage type: `Void / Magic`
- One-line visual fantasy: một chiến binh thiên hà bị nghiền thành dị thể hư không, mang trong cơ thể tàn tích trọng lực của một thiên hà đã chết.
- Character philosophy: "Vũ trụ không sinh ra từ ánh sáng, nó bắt đầu từ hư không. Và mọi ngôi sao rồi sẽ phải trở về với lòng mẹ đen kịt."

## 2. Lore

### Origin

Kronos từng là một chiến binh bảo hộ của Astraea, chủng tộc ánh sáng sống tại tâm Thiên Hà Kỷ Nguyên. Khi Hố Đen Mẹ bắt đầu sụp đổ và đe dọa nuốt chửng quê hương, anh tự nguyện lao vào tâm xoáy để dùng cơ thể làm neo trọng lực, giữ cho toàn bộ tinh hệ có thời gian thoát nạn.

### Inciting tragedy or vow

Kronos không chết. Cơ thể ánh sáng của anh bị kéo giãn, nghiền nát và tái cấu trúc liên tục trong hàng ngàn năm. Khi thoát khỏi chân trời sự kiện, anh trở thành một Void Anomaly chứa tàn dư trọng lực của cả thiên hà đã chết. Người Astraea không nhìn thấy người bảo hộ cũ; họ chỉ nhìn thấy một sinh vật có thể nuốt ánh sáng và xua đuổi anh khỏi quê hương.

### Present goal

Kronos tin rằng ánh sáng chỉ tạo ra kiêu ngạo, chia cắt và phản bội. Anh tìm cách đánh thức lại Hố Đen Mẹ, kéo các nền văn minh trở về trạng thái hư vô nơi không còn ai có thể bị ruồng bỏ. Mảnh bản năng bảo hộ cũ vẫn tồn tại, nhưng đã bị bóp méo thành ham muốn giữ mọi thứ vĩnh viễn bên trong trường hấp dẫn của mình.

### Cost of power

Mỗi lần mở rộng trường trọng lực, Kronos đánh mất thêm một ký ức về thời còn là Astraea. Càng mạnh, anh càng không thể phân biệt mình đang bảo vệ quê hương hay đang hủy diệt nó. Áp lực bên trong khiến thân hình luôn còng xuống và các khe giáp liên tục nứt vỡ.

### Faction, location and relationships

- Astraea: quê hương cũ và đối tượng oán hận lớn nhất; họ ghi công người hy sinh nhưng xóa tên thực thể trở về.
- Hố Đen Mẹ: vừa là nhà tù, nguồn sức mạnh, vừa là "người mẹ" duy nhất Kronos tin rằng không ruồng bỏ mình.
- Aurelia: nhận ra Kronos không bị ô nhiễm theo nghĩa thông thường; Long Thanh Tẩy không thể xóa hư không cấu thành cơ thể anh.
- Ignis: nhiệt lượng của Ignis có thể đốt các mảnh thiên thạch quanh Kronos nhưng càng tạo nhiều năng lượng để lõi hấp dẫn hấp thụ.
- Relationship rule: các liên kết này phục vụ lore và VFX; kit không khóa hiệu quả vào riêng Aurelia hoặc Ignis.

### Story details that must appear visually

- Vết nứt tím dọc giữa mặt là con mắt duy nhất.
- Ba lõi xoáy: một ở ngực và hai ở vai, trong đó lõi ngực sáng nhất sau vật thể trên tay.
- Các mảnh hành tinh vỡ bị giữ quanh hai cổ tay bằng quỹ đạo không ổn định.
- Dáng lưng còng biểu đạt sức nặng trường hấp dẫn, không phải tư thế già yếu.

## 3. Visual Contract

- Primary reference path: `TBD` - hiện chỉ có brief chữ, chưa có ảnh identity được duyệt.
- Generated key pose candidate: `Assets/Art/Characters/Kronos/Kronos.png`.
- Enlarged review preview: `Previews/Kronos_x8.png`.
- Canon comparison preview: `Previews/Kronos_Comparison_x8.png`.
- Deterministic source: `Tools/Generate-KronosSprite.py`.
- Reference role: `identity/silhouette`
- Reference resolution / alpha: `TBD`
- Traits that must remain unchanged: thân hình đồ sộ lai quái thú, đầu giáp sừng không có mặt người, mắt nứt tím dọc, da thiên hà, ba lõi hố đen và đá vũ trụ bay quanh cổ tay.
- Allowed lore additions: một dải nebula mantle rách bay về trái và một singularity nhỏ trên tay trước để giữ chữ ký silhouette của roster.
- Intended scaling/reconstruction method: `redraw`
- Differences from the current asset: nhân vật mới, chưa có asset hiện tại.
- Silhouette: vai cực rộng, đầu nhỏ chìm giữa giáp vai, lưng còng, hai cẳng tay dài và nặng; tay trước nâng một singularity, tay sau thấp hơn với móng vuốt mở.
- Body proportions: khoảng 3.7 đầu, cao 60-61 px trong canvas 64x64; vai rộng khoảng 34-38 px; bàn chân ở y=61-62.
- Hair shape and elemental streaks: không có tóc; thay bằng dải khí tinh vân tím-đỏ thoát từ gáy và kéo sang trái.
- Outfit and trailing cloth: không mặc trường bào; các mảng giáp thiên thạch ghép vào thân và một gravity shroud rách thay vai trò áo choàng dài.
- Story accessories (1-2): đá hành tinh quanh cổ tay và ba lõi xoáy ở ngực/vai.
- Raised-hand magic object: singularity tím đen cỡ 7-9 px, có vành accretion đỏ tím và 4-6 mảnh đá nhỏ.
- Main dark color: `#080915`, `#12112B`, `#21163D`.
- Element accent ramp: `#3B174F` -> `#6E246F` -> `#A43278` -> `#D04A8B`.
- Glow ramp: `#5E2AE6` -> `#9852FF` -> `#D69AFF` -> `#F7E6FF`.
- Metal/accessory color: `#302D3A` -> `#5C5568` -> `#91889B`.
- Tinted outline: `#05030D`, không dùng đen tuyệt đối.
- Secondary magic light interaction: singularity chiếu tím lên móng tay trước, cạnh phải giáp ngực và đá quỹ đạo; lõi vai tối hơn để không tranh điểm nhìn.
- Negative visual constraints: không mặt người, không mắt anime, không tóc, không áo choàng sạch, không giáp hiệp sĩ bóng loáng, không cánh, không vũ khí rời, không galaxy gradient mượt, không che mất chân bằng VFX.
- Canon comparison assets: `Assets/Art/Characters/Aurelia/Aurelia.png`, `Assets/Art/Characters/Nova/Combat/FireGodIdle.png`, `Assets/Art/Monsters/Voidroot.png`.
- Style-guide exception: tóc dài và trường bào được thay bằng nebula mantle và gravity shroud để giữ ba khối silhouette mà không phá brief quái thú vô diện.

## 4. Gameplay Data

Giữ stat nền của Brakk để không làm thay đổi power curve starter: 225 HP, 19 Attack, 12 Defense,
1.6 Move Speed, 1.2s Attack Interval và range Tanker mặc định. Devourer's Constitution cộng thêm
sát thương bằng 1.5% Max HP trong runtime.

## 5. Skill Kit

### Enter Game: Event Horizon / Void Invasion

- Khi bắt đầu trận, Kronos xuất hiện qua khe nứt không gian và để lại Void Field 5 giây.
- Toàn bộ địch giảm 15% tốc chạy và 10% sát thương đầu ra trong 5 giây.
- Kronos miễn nhiễm trạng thái bất lợi và khống chế trong 3 giây.
- VFX: `KronosDimensionalTear`, `KronosVoidField`; màn hình chớp xám tím ngắn.

### Basic: Void Claw

- Đòn đánh gây Magic damage và gieo Void Parasite trong 4 giây.
- Khi đủ 10 Void Resonance, đòn kế tiếp tiêu thụ toàn bộ tầng, gây Magic damage bằng 6% Max HP
  quanh Kronos và làm chậm 40% trong 2 giây.
- Trong Cosmic Leviathan, đòn đánh trở thành cone AoE và hồi 30% tổng sát thương đã gây.

### Passive 1: Gravitational Crust

- Mỗi lượng sát thương tích lũy bằng 5% Max HP tạo một Void Resonance, tối đa 10.
- Mỗi tầng tăng 2% Defense/Magic Resistance và phản lại 1% sát thương đã nhận dưới dạng Void.
- Khi Singularity Pull đang bảo hộ Kronos, tỷ lệ phản sát thương được nhân đôi.
- VFX: đá hành tinh quay quanh người; đủ 10 tầng sáng tím và sẵn sàng bộc phát.

### Passive 2: Void Parasite

- Basic, skill gây sát thương và Singularity Pull gieo ký sinh 4 giây.
- Mục tiêu bị giảm 15% hồi máu và tạo giáp.
- Khi mục tiêu nhiễm ký sinh đánh Kronos, 20% sát thương thực nhận được cộng thành giáp tạm thời
  cho Kronos, tối đa 60% Max HP.

### Passive 3: Devourer's Constitution

- Kronos nhận thêm Attack bằng 1.5% Max HP.
- Một đòn chí tử kích hoạt Black Hole Collapse trong 2 giây thay vì chết: Kronos còn 1 HP,
  không thể hành động, miễn nhiễm sát thương và hút 5% Max HP của địch trong 3.2m mỗi giây.
- Hết trạng thái, Kronos hồi lượng HP đã hút; cooldown 120 giây.

### Active: Singularity Pull & Taunt

- Action 0.82 giây, release frame 56%, cooldown 7 giây.
- Kéo địch trong 6m về sát Kronos, gieo Void Parasite và Taunt 2 giây.
- Kronos giảm 30% sát thương nhận vào trong thời gian bảo hộ.
- Trong Cosmic Leviathan: bán kính 9m, Taunt 2.5 giây.

### Ultimate: Cosmic Leviathan Awakens

- Action 1.05 giây, release frame 60%; trạng thái tồn tại 10 giây.
- Tăng Max HP 40%, Defense/Magic Resistance 50%, attack range 20% và scale hình ảnh x2.
- Basic trở thành cone AoE có 30% omnivamp.
- Aura mỗi giây gây Magic damage bằng 3% Max HP trong bán kính 3m.
- VFX: `KronosLeviathanAwaken`, aura nebula, star-chain và `KronosVoidSlash`.

## 6. Animation Manifest

| State | Canvas | Frames | Loop | Gameplay frame | File |
| --- | --- | ---: | --- | ---: | --- |
| Idle | 64x64 | 8 | yes | - | `KronosIdle.png` |
| Run | 64x64 | 8 | yes | - | `KronosRun.png` |
| Basic | 64x64 | 8 | no | 50% | `KronosBasic.png` |
| Active | 64x64 | 12 | no | 56% | `KronosActive.png` |
| Ultimate | 64x64 | 14 | no | 60% | `KronosUltimate.png` |
| Hit | 64x64 | 5 | no | - | `KronosHit.png` |
| Death | 64x64 | 10 | no | - | `KronosDeath.png` |

Pivot: center; foot line y=61-62; source quay phải.

## 7. VFX Manifest

`KronosDimensionalTear`, `KronosVoidField`, `KronosMassOrbit`, `KronosResonanceBurst`,
`KronosParasite`, `KronosParasiteDrain`, `KronosSingularity`, `KronosLeviathanAwaken`,
`KronosLeviathanAura`, `KronosVoidSlash`, `KronosCollapse`, `KronosDecayPulse`.

Mọi effect có lifetime giới hạn hoặc bám theo owner; aura/status bị hủy khi hết trạng thái,
chết hoặc reset farm wave.

## 8. Visual Approval

- [x] Lore và tâm lý nhân vật đã được diễn giải thành dấu hiệu ngoại hình.
- [x] Silhouette, tỷ lệ, chất liệu và bảng màu sơ bộ đã được xác định.
- [x] Ngoại lệ không tóc/không trường bào đã có giải pháp thay thế phù hợp phong cách roster.
- [ ] Có ảnh reference identity/silhouette do người dùng duyệt.
- [x] Key pose 64x64 đã được tạo ở 1x cùng preview 8x.
- [x] Key pose được người dùng duyệt và đã qua linework/readability pass.

## 9. Integration Checklist

- [x] Giữ internal kit `PrototypeSkillKit.Brakk`, asset filenames và GUID.
- [x] Display name đổi sang `Kronos`, species đổi sang `Cosmic`.
- [x] Save version 6 migrate progression, ownership và squad từ Brakk sang Kronos.
- [x] 7 animation sheets và 12 VFX sheets được sinh deterministic.
- [x] Gameplay chạy qua damage/status/action path hiện có.
- [x] Full Play Mode suite pass trong Unity Editor (46/46, Unity 6000.3.25f1).

## 10. Open Decisions

- TBD: ảnh reference identity/silhouette cho Kronos.
- TBD: duyệt nebula mantle và gravity shroud làm ngoại lệ visual chính thức.
- Không còn quyết định gameplay mở; số cân bằng có thể tinh chỉnh sau playtest boss/stage.
