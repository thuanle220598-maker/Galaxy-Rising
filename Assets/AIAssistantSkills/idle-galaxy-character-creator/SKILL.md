---
name: idle-galaxy-character-creator
description: Thiết kế và triển khai nhân vật tương thích với Idle Galaxy Rising, gồm lore, ảnh ref/reference, pixel-art sprite, gameplay kit, animation, VFX, ScriptableObject và kiểm thử. Luôn dùng skill này khi người dùng yêu cầu tạo, thêm, thiết kế, sửa, làm lại hoặc hoàn thiện hero/character/nhân vật và asset chiến đấu của họ trong project này.
metadata:
  required-editor-version: ">=6000.3"
---

# Idle Galaxy Character Creator

Tạo nhân vật theo canon hiện có, không xây một pipeline song song.

Đọc trước khi làm:

1. `references/project-character-contract.md` để biết cấu trúc runtime, đường dẫn và giới hạn kỹ thuật.
2. `resources/character-design-template.md` để tạo hồ sơ nhân vật.
3. `../../../../docs/art/character-sprite-style-guide.md` để khóa phong cách pixel art.
4. `../../../../README.md` để nắm phase hiện tại và các hành vi không được làm hồi quy.
5. Nếu nhân vật là Ignis hoặc Fire Mage hiện tại, đọc `examples/ignis.md` và spec Fire God được dẫn trong đó.
6. Nếu nhân vật là Aurelia, đọc `../../../../docs/characters/aurelia.md`, xem ref `../../../../Previews/sprite-fusion-76d4805b-391e-4bc4-a82a-d4cdd624c77a.png` và tái sử dụng `../../../../Tools/Generate-AureliaSprite.py`.

## Nguyên tắc

- Dùng lore để giải thích gameplay và hình ảnh; mỗi cơ chế chủ đạo phải có dấu hiệu hình ảnh rõ ràng.
- Giữ ID nội bộ, GUID và đường dẫn cũ khi đang thay thế một nhân vật hiện có. Chỉ tạo ID mới khi đây thực sự là một slot nhân vật mới.
- Dùng `CombatantDefinition` và `SkillDefinition` làm nguồn dữ liệu gameplay. Không hard-code dữ liệu mới vào UI.
- Tái sử dụng coroutine, sprite renderer, projectile, zone, status, damage feedback và VFX helper hiện có trước khi viết hệ thống mới.
- Không thêm package, Animator Controller, Timeline, shader dependency hoặc save field nếu runtime hiện tại đã đáp ứng.
- Mọi sprite production phải đọc được ở portrait `360x640`, cả tốc độ `x1` và `x2`.
- Không ghi đè asset đã duyệt chỉ để thử biến thể. Đặt bản thử trong thư mục preview hoặc tên tạm rõ ràng.
- Khi người dùng chỉ định một ảnh ref, ref là nguồn chuẩn cho silhouette, pose, palette và mật độ chi tiết. Style guide vẫn khóa yêu cầu kỹ thuật, nhưng không được dùng lore để tự ý làm lệch ref.
- Chi tiết lore bổ sung vào ref phải nhỏ, dễ đọc và không thay đổi khối hình chính; ví dụ orb, sừng, vảy hoặc hoa văn, không tự thêm cánh/áo choàng/giáp lớn.

## Quy trình

### 1. Khảo sát canon

- Đọc toàn bộ hồ sơ người dùng cung cấp.
- Mở character, skill, animation và VFX gần nhất về hệ hoặc vai trò để làm chuẩn so sánh.
- Tìm mọi caller của enum, helper hoặc asset định sửa trước khi thay đổi.
- Xác định một trong hai chế độ:
  - `replace-existing`: đổi identity/presentation nhưng giữ ID, GUID, save compatibility.
  - `new-slot`: thêm nhân vật, enum, data, roster/gacha và xử lý save cần thiết.

### 2. Lập hồ sơ thiết kế

- Sao chép cấu trúc trong `resources/character-design-template.md` vào `docs/characters/<internal-id>.md`.
- Điền các giả định hợp lý từ lore thay vì hỏi dồn người dùng.
- Nêu rõ điểm chưa chốt bằng `TBD`; không biến suy đoán thành canon.
- Kiểm tra kit có một vòng lặp chiến đấu dễ mô tả trong một câu và có điểm yếu thực sự.
- Trình hồ sơ để người dùng duyệt trước khi sinh hàng loạt sprite/VFX hoặc sửa runtime lớn, trừ khi họ yêu cầu triển khai ngay.

### 3. Khóa gameplay contract

- Chọn species, class, rarity và formation từ enum hiện có; chỉ mở rộng enum nếu nội dung không thể biểu đạt bằng giá trị hiện tại.
- So stat với 2-3 hero cùng class/rarity; không chọn số chỉ từ lore.
- Định nghĩa Basic, Passive 1-3 khi cần, Active và Ultimate bằng dữ liệu hiện có.
- Với mỗi skill, ghi rõ: mục tiêu, phạm vi, damage type, multiplier, timing, cooldown/energy, status, combo, counterplay và VFX cue.
- Nếu cần mechanic mới, thêm primitive nhỏ nhất vào shared damage/status/action path để mọi caller liên quan dùng chung.

### 4. Khóa visual contract

- Tạo một idle/key pose trước. Chỉ làm animation sau khi silhouette, palette và phụ kiện kể chuyện đã được duyệt.
- Mặc định canvas `64x64`, quay phải, nền trong suốt, point filter, không compression, PPU bằng chiều rộng sprite.
- Giữ ánh sáng trên-phải, outline 1 px pha sắc hệ, palette không quá 64 màu và pixel sáng nhất ở nguồn ma thuật.
- Khóa linework trước khi thêm texture: ở kích thước `1x` phải phân biệt ngay đầu, thân, tay trước, tay sau, hai chân và vật ma thuật; nếu các khối này nhập thành một mảng tối thì key pose chưa đạt.
- Outline ngoài phải liên tục, dày 1 px và không tạo cụm đen dày che silhouette. Các bộ phận tối nằm cạnh nhau phải tách bằng ít nhất một bậc palette, occlusion seam hoặc selective rim light.
- So cạnh ref được chỉ định và canon gần nhất ở `1x`, `4x` hoặc `8x`; sửa silhouette, scale và độ dày outline trước khi tiếp tục.
- Nếu dùng generator, dùng cùng key pose/reference image và seed cho mọi state để giảm character drift.

#### Linework and readability gate

- Kiểm tra sprite trên cả nền sáng trung tính và nền chiến trường tối; không dùng preview phóng lớn để che lỗi khó đọc ở `1x`.
- Ở `1x`, sừng/tóc, mặt, vai, khuỷu tay, cổ tay, bàn tay hoặc móng vuốt, đầu gối và bàn chân phải có contour hoặc value break đủ rõ.
- Chỉ dùng đường tối bên trong tại chỗ che khuất thật. Ưu tiên đổi bậc màu hoặc rim light thay vì kẻ nhiều đường đen làm bẩn sprite.
- Chi tiết thiên hà, vảy, hoa văn và particle không được thay thế cho cấu trúc hình thể; tắt các chi tiết này đi thì pose vẫn phải đọc được.
- Nếu người dùng phản hồi “đường nét chưa rõ”, quay lại silhouette/linework pass và không tiếp tục animation, VFX hay tích hợp runtime.

#### Reference-first workflow

- Ghi rõ vai trò của từng ảnh: `identity/silhouette`, `pose`, `palette`, `style` hay `edit target`. Không mặc định mọi ref đều là ảnh cần sửa.
- Mở ref ở kích thước gốc và bản nearest-neighbor phóng lớn. Ghi lại resolution, alpha, bounding box và các đặc điểm người dùng muốn giữ.
- Liệt kê delta giữa ref và output hiện tại trước khi sửa, ưu tiên khối hình lớn: tỷ lệ, tóc, trang phục, pose, rồi mới đến phụ kiện.
- Nếu project đã có source chỉnh sửa được hoặc generator deterministic, sửa source đó trước khi gọi image generator mới.
- Với ref pixel-art độ phân giải thấp, dùng nearest-neighbor để giữ đúng block hoặc Scale2x để làm sạch đường chéo. Không dùng upscale làm mờ.
- Pass đầu phải giống ref trước; chỉ sau đó thêm các dấu hiệu lore tối thiểu mà ref còn thiếu.
- Luôn xuất key pose và preview phóng lớn để người dùng duyệt. Feedback như “chưa ưng, tham khảo ref này” quay lại bước so delta, không tiếp tục animation.

### 5. Animation contract

- Bắt buộc: Idle, Run, Basic, Active, Ultimate, Hit, Death. Walk chỉ tạo khi runtime dùng nó.
- Mỗi sheet phải khai báo canvas, số frame, FPS, loop, action duration và hit/release frame.
- Gameplay impact phải khớp frame tiếp xúc hoặc frame phóng projectile, không mặc định giữa animation.
- Dùng horizontal sprite sheet, thứ tự trái sang phải, cùng pivot/foot line cho mọi state.
- Không tạo global pause hoặc hit-stop mới để che timing sai.

### 6. VFX contract

- Mỗi skill chỉ dùng các lớp cần thiết: anticipation/telegraph, travel, impact và residue/status.
- Màu sáng nhất dành cho hit quan trọng; residue và aura phải tối hơn để không che sprite/HP/energy.
- Tái sử dụng material và helper hiện có. Tách sheet mới chỉ khi silhouette hoặc timing không thể tái sử dụng.
- Giới hạn effect tồn tại theo action/zone; dọn sạch khi battle hoặc farm wave kết thúc.

### 7. Tích hợp Unity

- Lưu art tại `Assets/Art/Characters/<InternalId>/` và VFX tại `Assets/Resources/VFX/<InternalId>/`.
- Tạo/cập nhật `Assets/Resources/Combatants/.../<InternalId>.asset` và `Assets/Resources/Skills/<InternalId>.asset` qua `PrototypeContentGenerator` khi có thể.
- Gắn sprite arrays vào `CombatantDefinition`; giữ `sourceFacesLeft` đúng với source.
- Với `new-slot`, cập nhật đầy đủ enum/callers, content generator, roster/gacha, progression/save và test. Không dựa vào thứ tự enum nếu có thể tránh.
- Với `replace-existing`, giữ tên file, GUID và internal skill kit; thêm migration duy nhất nếu display name lưu trong save thay đổi.

### 8. Kiểm tra

- Chạy kiểm tra nhỏ nhất có thể bắt lỗi logic mới, sau đó chạy toàn bộ Play Mode suite trước khi hoàn tất.
- Xác nhận không có compiler error, missing sprite, frame trong suốt, material hồng hoặc object VFX rò rỉ.
- Với sprite, kiểm tra kích thước, alpha nhị phân, palette, bounds, import Point/PPU/compression và vị trí pixel sáng nhất; ghi rõ ngoại lệ nếu ref đã duyệt yêu cầu khác style guide.
- Kiểm tra linework ở `1x` trên nền sáng và tối: silhouette ngoài liền mạch, anatomy chính không dính khối và các chi tiết nhỏ không lấn át contour.
- Xem battle portrait ở `x1` và `x2`: facing, foot line, hit timing, readability, status overlap và death cleanup.
- So sức mạnh với hero cùng rarity/class trong ít nhất một trận bình thường và một boss stage.
- Báo rõ file đã tạo, test đã chạy và mọi `TBD` còn lại.

## Definition of done

Một nhân vật chỉ hoàn tất khi hồ sơ, data, sprite/animation, VFX, runtime behavior và test mô tả cùng một kit; không có mechanic chỉ tồn tại trong text và không có effect không gắn với gameplay cue.
