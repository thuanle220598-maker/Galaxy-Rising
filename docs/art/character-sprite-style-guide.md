# Character Sprite Style Guide — Idle Galaxy Rising

Mục tiêu: mọi nhân vật trong game đều trông như cùng một họa sĩ vẽ. Dùng tài liệu này khi
tạo nhân vật mới (bằng AI generator, bằng script, hay vẽ tay trong Pixelorama/Aseprite).

Ảnh chuẩn (canon):
- **Nova** (Hỏa hệ) — ảnh gốc `sprite-fusion-pixel-art-generator-68x68.png`, bản in-game ở
  `Assets/Art/Characters/Nova/`.
- **Aurelia** (Thủy hệ) — `Assets/Art/Characters/Aurelia/Aurelia.png`, sinh bởi
  `Tools/Generate-AureliaSprite.py`, preview phóng to `Previews/Aurelia_x8.png`.

Khi phân vân, mở 2 ảnh này cạnh nhau và so sánh.

---

## 1. Khung hình & bố cục

| Thuộc tính | Quy chuẩn |
|---|---|
| Kích thước | **64×64 px**, nền trong suốt (Nova cũ là 68×68, xem mục 8) |
| Góc nhìn | 3/4, **mặt và thân quay sang phải** |
| Tư thế | Đứng. Tay phía sau buông xuống, **tay phía trước giơ lên ngang vai, cầm nguồn ma thuật của hệ** |
| Chiều cao | Nhân vật gần như kín chiều cao khung: đỉnh đầu/sừng ở y≈0–3, **bàn chân ở y≈61–62** |
| Tỷ lệ | ~4–4.5 đầu. Đầu (tóc + mặt) cao ~14–16 px, thân từ cổ đến chân ~42 px |
| Vị trí ngang | Thân ở giữa (x≈24–42). Tóc/áo choàng bay **ra sau sang trái**, hiệu ứng ma thuật ở **góc trên bên phải** |
| Khoảng trống | Chừa 1 px trống quanh mọi mép (trừ khi tóc/sừng chạm mép trên) |

Hình dáng: đọc được ngay ở kích thước 1x. Phải có 3 khối tách bạch: **tóc/áo choàng tung bay
bên trái — thân áo dài đứng giữa — tay giơ + ma thuật bên phải**.

## 2. Viền (outline)

- Viền ngoài dày **1 px**, **không dùng đen tuyệt đối**. Pha sắc màu tối của hệ:
  Hỏa ≈ `#0A0204` (đen ngả đỏ), Thủy `#090C20` (đen ngả navy).
- Viền chỉ 4 hướng (trên/dưới/trái/phải), không viền chéo → góc mềm, không bị "dày".
- Hiệu ứng ma thuật (lửa, nước, sét...) **không** dùng viền tối; viền bằng màu tối nhất của dải
  màu hiệu ứng để nó đọc như ánh sáng chứ không phải vật thể.
- Bên trong nhân vật không kẻ viền đen giữa các mảng; phân tách bằng bóng đổ (xem mục 3).

## 3. Ánh sáng & đổ bóng

- **Nguồn sáng chính: phía trên bên phải.** Mép trên-phải của mỗi mảng sáng hơn 1 bậc, mép
  dưới-trái tối hơn 1 bậc (đôi khi 2 bậc).
- **Bóng che khuất:** chỗ một mảng nằm sau mảng khác (tóc sau cổ, áo sau tay) tối hơn 1 bậc.
- **Nguồn sáng phụ: vật ma thuật trên tay.** Trong bán kính ~9 px quanh vật đó, mọi bề mặt
  sáng lên 1 bậc (tay, cổ tay áo, lọn tóc gần đó).
- Không dùng gradient mượt, không anti-alias ra nền trong suốt.
- Nhiễu nhẹ kiểu vẽ tay: ~5% pixel ở **tóc, vải, áo choàng** lệch ±1 bậc. **Da mặt luôn sạch.**

## 4. Màu sắc

### Dải màu (ramp)
- Mỗi chất liệu có **3–5 bậc màu**, bậc tối nhất gần màu viền.
- Có dịch sắc độ (hue shift): bóng lạnh/ngả tím hơn, highlight ấm/ngả trắng hơn. Không chỉ
  tăng/giảm độ sáng.
- Tổng màu toàn sprite **≤ 64** (Aurelia dùng 53).

### Tỷ lệ phối màu (áp cho mọi hệ)
| Vai trò | Diện tích | Nơi dùng |
|---|---|---|
| Màu chủ đạo tối | ~55–60% | Thân áo, phần lớn tóc |
| **Màu hệ (accent)** | ~25% | **Lọn tóc sọc**, lớp lót/áo choàng bay, chi tiết viền áo |
| Kim loại/phụ kiện | ~5% | Thắt lưng, khóa, trang sức (vàng hoặc bạc) |
| Da | ~5–8% | Mặt, cổ, bàn tay |
| Sáng nhất / phát sáng | <5% | **Chỉ** ở vật ma thuật, tia hạt, tròng mắt |

Pixel sáng nhất của cả sprite phải nằm ở vật ma thuật. Không để quần áo sáng hơn nó.

### Bảng màu theo hệ
| Hệ | Chủ đạo tối | Accent của hệ | Phát sáng | Kim loại | Viền |
|---|---|---|---|---|---|
| Hỏa (Nova) | đen than, nâu xám | đỏ thẫm `#8C0A14` → `#D21E28` | đỏ tươi → trắng hồng | bạc xỉn, xương | `#0A0204` |
| Thủy (Aurelia) | navy `#18285E` → `#3A6CB4` | ngọc lam `#2A92A6` → `#60CCD6` | ngọc bích `#56E4C8` → trắng | vàng `#E6B646` | `#090C20` |
| Lôi *(đề xuất)* | tím than | tím điện → vàng chanh | trắng vàng | bạc | đen ngả tím |
| Mộc/Thổ *(đề xuất)* | nâu rêu | xanh lá cây | xanh ngọc sáng | đồng | đen ngả nâu |
| Phong *(đề xuất)* | xám xanh | xanh bạc hà | trắng | bạc | đen ngả xám xanh |
| Ám *(đề xuất)* | đen tím | tím mận | tím hồng | vàng xỉn | đen tím |

Khi chốt màu cho một hệ mới, cập nhật bảng này và bỏ chữ *(đề xuất)*.

## 5. Chi tiết nhận diện bắt buộc

Nhân vật nào cũng phải có đủ các chi tiết sau, vì đây là "chữ ký" chung của cả dàn nhân vật:

1. **Tóc dài, bay ra sau sang trái**, xơ ở đuôi, có **2–4 lọn sọc màu hệ** chạy dọc tóc.
2. **Trang phục dài** (áo choàng, trường bào) chấm hoặc quá gối, **gấu áo rách hoặc lượn sóng**,
   có một lớp vải/áo choàng bay phía sau.
3. **Vật ma thuật trên tay giơ lên**, kèm **3–8 hạt/giọt/tàn lửa rời rạc** rải ở góc trên phải.
4. **Mắt 2 px có tròng phát sáng màu hệ** (1 px màu hệ + 1 px highlight). Miệng tối đa 1 px hoặc bỏ.
5. **1–2 phụ kiện kể chuyện** (Nova: sọ, dây da; Aurelia: sừng rồng, vây tai, vảy má, long ngọc).
6. Giày/boot tối màu, 1 vệt highlight nhỏ.

Tránh: đầu chibi to, mắt anime to, hình khối góc vuông cứng, nền, bóng đổ xuống đất, chữ.

## 6. Prompt mẫu cho AI generator

Ảnh Nova được tạo bằng Sprite Fusion pixel art generator. Giữ nguyên phần **[STYLE]** và
**[NEGATIVE]** cho mọi nhân vật, chỉ thay phần **[CHARACTER]**.

```
[STYLE]
64x64 pixel art game character sprite, full body, standing, 3/4 view facing right,
dark fantasy mage, detailed hand-pixeled shading, 1px dark tinted outline,
light source from upper right, limited palette (under 64 colors), no anti-aliasing,
transparent background, long flowing hair blowing to the left with colored streaks,
long robe with tattered flowing hem, cape trailing behind,
front hand raised at shoulder height holding glowing elemental magic,
small magic particles floating at the upper right, the brightest pixels are the magic,
slender realistic proportions about 4.5 heads tall, small glowing eyes

[CHARACTER]
<tên, hệ>, <màu chủ đạo> and <màu hệ> color scheme,
<tóc: màu + màu sọc>, <trang phục>, <phụ kiện kể chuyện 1-2 cái>,
holding <vật ma thuật> in the raised hand

[NEGATIVE]
chibi, big head, big anime eyes, blurry, anti-aliased, gradient, soft brush,
background, ground shadow, text, watermark, frame, multiple characters,
front view, back view, cropped, weapon too large
```

Ví dụ phần [CHARACTER] của Aurelia:
```
Aurelia, water dragon princess mage, deep navy blue and turquoise color scheme,
dark ocean-blue hair with turquoise streaks, small cream-gold dragon horns,
turquoise fin instead of an ear, tiny scales on the cheek,
navy mage robe with silver-white front panel and wave embroidery, gold belt,
turquoise sash flowing behind like water,
holding a glowing jade dragon pearl with a spiraling ribbon of water in the raised hand
```

Sau khi generator trả ảnh: resize về 64×64 bằng **nearest neighbor**, xóa nền, rồi chạy qua
checklist ở mục 9. Generator thường sai ở viền, màu nền lẫn vào và số màu, nên sửa tay các phần đó.

## 7. Tạo bằng script (cách đã dùng cho Aurelia)

`Tools/Generate-AureliaSprite.py` là template. Để tạo nhân vật mới:

1. Copy thành `Tools/Generate-<Tên>Sprite.py`.
2. Sửa `RAMPS` theo bảng màu hệ (mục 4). Giữ nguyên số bậc của mỗi ramp.
3. Sửa các `region(...)`: đa giác theo thứ tự vẽ **từ sau ra trước** (tóc sau → áo choàng →
   tay sau → thân → đầu → tay trước → ma thuật). Giữ khung tư thế và vị trí đầu/chân như Aurelia
   để các nhân vật đứng cạnh nhau trông cùng tỷ lệ.
4. Đổi `ORB` thành tâm vật ma thuật mới (điểm phát sáng phụ).
5. Không đổi phần đổ bóng/viền/nhiễu (`# Key light`, `# Occlusion`, `# Painterly texture`,
   `# outline`): đó là phần giữ style đồng nhất.
6. Chạy: `python3 Tools/Generate-<Tên>Sprite.py Assets/Art/Characters/<Tên>/<Tên>.png --preview Previews/<Tên>_x8.png`

## 8. Import vào Unity

Giống sprite của Nova:
- Texture Type: Sprite (2D and UI)
- **Filter Mode: Point (no filter)**
- **Compression: None**
- Pixels Per Unit: **bằng chiều rộng sprite** (64 cho sprite 64×64; Nova 68×68 dùng 68). Như vậy mọi
  nhân vật đều cao 1 unit. Lưu ý: vì Nova lớn hơn 4 px, mật độ pixel của hai nhân vật lệch nhau
  khoảng 6%. Nếu cần đồng bộ tuyệt đối, chuẩn hóa tất cả về cùng một kích thước canvas.

## 9. Checklist trước khi chốt ảnh

- [ ] 64×64, nền trong suốt, không có pixel bán trong suốt
- [ ] Quay phải, tay trước giơ cầm ma thuật, tóc/áo bay sang trái
- [ ] Chân ở y≈61–62, đỉnh đầu gần mép trên
- [ ] Viền 1 px màu tối pha sắc hệ, hiệu ứng ma thuật không có viền đen
- [ ] Sáng trên-phải, tối dưới-trái; có bóng che khuất giữa các lớp
- [ ] ≤ 64 màu; pixel sáng nhất nằm ở vật ma thuật
- [ ] Có sọc tóc màu hệ, gấu áo rách/lượn sóng, hạt ma thuật rải rác
- [ ] Mắt phát sáng màu hệ
- [ ] Đặt cạnh Nova và Aurelia ở 1x và 4x: cùng chiều cao, cùng độ dày viền, cùng mức chi tiết
- [ ] Cập nhật mục "Ảnh chuẩn" và bảng màu theo hệ trong file này
