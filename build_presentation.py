import os
import sys
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

def create_presentation():
    prs = Presentation()
    # 16:9 Widescreen format
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6] # completely blank

    # Color Palette
    BG_DARK = RGBColor(18, 21, 28)       # #12151C deep dark slate
    CARD_BG = RGBColor(27, 32, 44)       # #1B202C card background
    CARD_BORDER = RGBColor(45, 52, 68)   # #2D3444 card subtle border
    GOLD_MAIN = RGBColor(255, 199, 44)   # #FFC72C bright gold
    GOLD_LIGHT = RGBColor(255, 222, 115) # #FFDE73 soft gold
    ACCENT_RED = RGBColor(239, 68, 68)   # #EF4444 hazard red
    ACCENT_BLUE = RGBColor(59, 130, 246) # #3B82F6 tech blue
    ACCENT_GREEN = RGBColor(34, 197, 94) # #22C55E success green
    TEXT_WHITE = RGBColor(255, 255, 255)
    TEXT_MUTED = RGBColor(160, 174, 192) # #A0AEC0
    TEXT_DARK = RGBColor(30, 30, 30)

    def set_slide_bg(slide, color=BG_DARK):
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
        bg.fill.solid()
        bg.fill.fore_color.rgb = color
        bg.line.fill.background() # no outline
        return bg

    def add_header(slide, category, title, subtitle=None):
        # Category badge / kicker
        cat_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.45), Inches(11.5), Inches(0.35))
        tf_cat = cat_box.text_frame
        tf_cat.word_wrap = True
        tf_cat.margin_left = tf_cat.margin_top = tf_cat.margin_right = tf_cat.margin_bottom = 0
        p_cat = tf_cat.paragraphs[0]
        p_cat.text = category.upper()
        p_cat.font.size = Pt(11)
        p_cat.font.bold = True
        p_cat.font.color.rgb = GOLD_MAIN

        # Main Title
        title_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.8), Inches(11.5), Inches(0.65))
        tf_title = title_box.text_frame
        tf_title.word_wrap = True
        tf_title.margin_left = tf_title.margin_top = tf_title.margin_right = tf_title.margin_bottom = 0
        p_title = tf_title.paragraphs[0]
        p_title.text = title
        p_title.font.size = Pt(24)
        p_title.font.bold = True
        p_title.font.color.rgb = TEXT_WHITE

        # Divider line
        line = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), Inches(1.5), Inches(11.733), Inches(0.02))
        line.fill.solid()
        line.fill.fore_color.rgb = CARD_BORDER
        line.line.fill.background()

    def add_card(slide, left, top, width, height, bg_color=CARD_BG, border_color=CARD_BORDER):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = bg_color
        if border_color:
            card.line.color.rgb = border_color
            card.line.width = Pt(1.5)
        else:
            card.line.fill.background()
        return card

    # =========================================================================
    # SLIDE 1: TITLE SLIDE
    # =========================================================================
    slide1 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide1, RGBColor(14, 16, 22))

    # Decorative top gold bar
    top_bar = slide1.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, Inches(0.1))
    top_bar.fill.solid()
    top_bar.fill.fore_color.rgb = GOLD_MAIN
    top_bar.line.fill.background()

    # Left content box
    title_box = slide1.shapes.add_textbox(Inches(1.0), Inches(1.8), Inches(7.5), Inches(4.5))
    tf1 = title_box.text_frame
    tf1.word_wrap = True

    p_sub = tf1.paragraphs[0]
    p_sub.text = "BÁO CÁO PHÁT TRIỂN GAME 2D • UNITY & C#"
    p_sub.font.size = Pt(13)
    p_sub.font.bold = True
    p_sub.font.color.rgb = GOLD_MAIN
    p_sub.space_after = Pt(14)

    p_main = tf1.add_paragraph()
    p_main.text = "ĐÀO VÀNG CLASSIC"
    p_main.font.size = Pt(44)
    p_main.font.bold = True
    p_main.font.color.rgb = TEXT_WHITE
    p_main.space_after = Pt(10)

    p_desc = tf1.add_paragraph()
    p_desc.text = "Tối ưu hóa đa màn hình (Dọc/Ngang), hoàn thiện cơ chế kéo móc vật lý, hệ thống 3 màn chơi thử thách và cạm bẫy Bom nổ kịch tính."
    p_desc.font.size = Pt(14)
    p_desc.font.color.rgb = TEXT_MUTED
    p_desc.space_after = Pt(28)

    # Badges row
    p_meta = tf1.add_paragraph()
    p_meta.text = "★ Unity 2022+  |  ★ C# Architecture  |  ★ Responsive Camera  |  ★ 3 Levels & Bomb"
    p_meta.font.size = Pt(11)
    p_meta.font.bold = True
    p_meta.font.color.rgb = GOLD_LIGHT

    # Right Hero Illustration Card
    hero_card = add_card(slide1, Inches(8.8), Inches(1.5), Inches(3.6), Inches(4.8), bg_color=CARD_BG, border_color=GOLD_MAIN)

    # Add Miner Sprite if exists
    if os.path.exists("Assets/Sprites/Player/miner.png"):
        slide1.shapes.add_picture("Assets/Sprites/Player/miner.png", Inches(9.6), Inches(1.8), width=Inches(2.0))

    # Add Bomb and Gold sprites in front
    if os.path.exists("Assets/Sprites/Items/big_gold.png"):
        slide1.shapes.add_picture("Assets/Sprites/Items/big_gold.png", Inches(9.1), Inches(4.2), width=Inches(1.4))
    if os.path.exists("Assets/Sprites/Items/bomb.png"):
        slide1.shapes.add_picture("Assets/Sprites/Items/bomb.png", Inches(10.7), Inches(4.3), width=Inches(1.3))

    # Footer note
    ft_box = slide1.shapes.add_textbox(Inches(1.0), Inches(6.6), Inches(11.3), Inches(0.4))
    ft_p = ft_box.text_frame.paragraphs[0]
    ft_p.text = "Dự án: DaoVangUnity  •  Thiết kế & Báo cáo Kỹ thuật  •  2026"
    ft_p.font.size = Pt(10)
    ft_p.font.color.rgb = RGBColor(100, 116, 139)

    # =========================================================================
    # SLIDE 2: TỔNG QUAN DỰ ÁN (PROJECT OVERVIEW)
    # =========================================================================
    slide2 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide2)
    add_header(slide2, "Tổng quan dự án", "Giới thiệu & Mục tiêu Trò chơi Đào Vàng 2D")

    cards_data = [
        ("CẢM HỨNG & THỂ LOẠI",
         "Lối chơi Arcade kinh điển",
         ["Lấy cảm hứng từ tựa game Gold Miner huyền thoại quen thuộc.",
          "Phát triển trên nền tảng Unity 2D với đồ họa sắc nét, hiện đại.",
          "Trò chơi casual phù hợp cho cả giải trí ngắn và luyện phản xạ căn góc."],
         GOLD_MAIN),
        ("MỤC TIÊU & LUẬT CHƠI",
         "Đạt điểm Target trước khi hết giờ",
         ["Thả móc chuẩn xác để kéo vàng và kim cương có giá trị cao.",
          "Thời gian giới hạn từng màn (45s - 60s), tạo áp lực cuốn hút.",
          "Cạm bẫy đá nặng làm chậm tốc độ và Bom nổ thua ngay lập tức."],
         ACCENT_BLUE),
        ("CÔNG NGHỆ ÁP DỤNG",
         "Nền tảng kỹ thuật tối ưu",
         ["Engine Unity, ngôn ngữ C# cấu trúc module tách biệt.",
          "Hệ thống Physics2D với OverlapCircle bắt va chạm tức thì.",
          "Tự động thích ứng cả màn hình Dọc (Portrait) và Ngang (Landscape)."],
         ACCENT_GREEN)
    ]

    card_w = Inches(3.64)
    card_gap = Inches(0.4)
    start_x = Inches(0.8)

    for i, (kicker, head, bullets, accent) in enumerate(cards_data):
        cx = start_x + i * (card_w + card_gap)
        add_card(slide2, cx, Inches(1.8), card_w, Inches(4.9), CARD_BG, CARD_BORDER)

        # Accent top bar inside card
        cbar = slide2.shapes.add_shape(MSO_SHAPE.RECTANGLE, cx + Inches(0.2), Inches(1.95), Inches(1.0), Inches(0.04))
        cbar.fill.solid()
        cbar.fill.fore_color.rgb = accent
        cbar.line.fill.background()

        tbox = slide2.shapes.add_textbox(cx + Inches(0.2), Inches(2.1), card_w - Inches(0.4), Inches(4.3))
        tf = tbox.text_frame
        tf.word_wrap = True

        pk = tf.paragraphs[0]
        pk.text = kicker
        pk.font.size = Pt(10)
        pk.font.bold = True
        pk.font.color.rgb = accent
        pk.space_after = Pt(6)

        ph = tf.add_paragraph()
        ph.text = head
        ph.font.size = Pt(16)
        ph.font.bold = True
        ph.font.color.rgb = TEXT_WHITE
        ph.space_after = Pt(16)

        for b in bullets:
            pb = tf.add_paragraph()
            pb.text = "• " + b
            pb.font.size = Pt(12)
            pb.font.color.rgb = TEXT_MUTED
            pb.space_after = Pt(10)

    # =========================================================================
    # SLIDE 3: CƠ CHẾ GAMEPLAY CỐT LÕI (CORE GAMEPLAY MECHANICS)
    # =========================================================================
    slide3 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide3)
    add_header(slide3, "Cơ chế cốt lõi", "Vòng đời & Nguyên lý Hoạt động của Móc Câu")

    steps = [
        ("1. ĐU LẮC (SWINGING)",
         "Móc tự động dao động",
         "Móc liên tục đu qua lại quanh trục HookPivot với góc ±60° theo công thức sin wave mượt mà, sẵn sàng chờ lệnh thả.",
         GOLD_MAIN),
        ("2. THẢ MÓC (EXTENDING)",
         "Phóng móc theo góc nhắm",
         "Người chơi chạm màn hình / click chuột. Móc phóng thẳng xuống đáy với vận tốc cố định (extendSpeed = 9.0).",
         ACCENT_BLUE),
        ("3. CHẠM VẬT (CATCHING)",
         "Bắt va chạm OverlapCircle",
         "Quét vòng tròn quanh đầu móc với bán kính 0.15m trên Item LayerMask, nhận diện tức thì loại vật phẩm trúng phải.",
         GOLD_LIGHT),
        ("4. THU VỀ (RETRACTING)",
         "Tốc độ theo khối lượng",
         "Kéo vật phẩm về gốc. Tốc độ thu tỉ lệ nghịch với khối lượng: V = V_base / Mass * Mod. Vật nặng kéo chậm, vật nhẹ kéo nhanh.",
         ACCENT_GREEN)
    ]

    s_w = Inches(2.7)
    s_gap = Inches(0.3)
    s_x = Inches(0.8)

    for i, (stitle, ssub, sdesc, scolor) in enumerate(steps):
        sx = s_x + i * (s_w + s_gap)
        add_card(slide3, sx, Inches(1.8), s_w, Inches(4.9), CARD_BG, CARD_BORDER)

        badge = slide3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, sx + Inches(0.2), Inches(2.0), Inches(0.45), Inches(0.45))
        badge.fill.solid()
        badge.fill.fore_color.rgb = scolor
        badge.line.fill.background()
        btf = badge.text_frame
        bp = btf.paragraphs[0]
        bp.text = str(i + 1)
        bp.font.size = Pt(14)
        bp.font.bold = True
        bp.font.color.rgb = TEXT_DARK
        bp.alignment = PP_ALIGN.CENTER

        tbox = slide3.shapes.add_textbox(sx + Inches(0.2), Inches(2.6), s_w - Inches(0.4), Inches(3.8))
        tf = tbox.text_frame
        tf.word_wrap = True

        p1 = tf.paragraphs[0]
        p1.text = stitle
        p1.font.size = Pt(10)
        p1.font.bold = True
        p1.font.color.rgb = scolor
        p1.space_after = Pt(4)

        p2 = tf.add_paragraph()
        p2.text = ssub
        p2.font.size = Pt(15)
        p2.font.bold = True
        p2.font.color.rgb = TEXT_WHITE
        p2.space_after = Pt(12)

        p3 = tf.add_paragraph()
        p3.text = sdesc
        p3.font.size = Pt(12)
        p3.font.color.rgb = TEXT_MUTED

    # =========================================================================
    # SLIDE 4: HỆ THỐNG VẬT PHẨM & CÂN BẰNG (ITEM BALANCING)
    # =========================================================================
    slide4 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide4)
    add_header(slide4, "Hệ thống vật phẩm", "Bảng Thông số Cân bằng & Trọng số Xuất hiện")

    items_info = [
        ("Vàng Nhỏ", "Small Gold", "+50đ", "1.0", "Nhanh", "35%", "Assets/Sprites/Items/small_gold.png", GOLD_MAIN),
        ("Vàng Lớn", "Big Gold", "+150đ", "2.2", "Chậm", "20%", "Assets/Sprites/Items/big_gold.png", GOLD_LIGHT),
        ("Kim Cương", "Diamond", "+300đ", "1.5", "Rất nhanh", "10 - 15%", "Assets/Sprites/Items/diamond.png", ACCENT_BLUE),
        ("Hòn Đá", "Rock", "+10đ", "3.0", "Rất chậm", "20 - 25%", "Assets/Sprites/Items/rock.png", TEXT_MUTED),
        ("BOM (Mới)", "Bomb Hazard", "0đ (THUA)", "1.0", "Nổ lập tức", "15 - 25%", "Assets/Sprites/Items/bomb.png", ACCENT_RED),
    ]

    row_h = Inches(0.88)
    table_top = Inches(1.8)
    row_w = Inches(11.733)

    for idx, (name, en_name, score, mass, speed, spawn_rate, img_path, accent) in enumerate(items_info):
        ry = table_top + idx * (row_h + Inches(0.12))
        rcard = add_card(slide4, Inches(0.8), ry, row_w, row_h, CARD_BG, accent if idx == 4 else CARD_BORDER)

        # Icon image
        if os.path.exists(img_path):
            slide4.shapes.add_picture(img_path, Inches(1.05), ry + Inches(0.14), width=Inches(0.6))

        # Text box for item row
        rbox = slide4.shapes.add_textbox(Inches(1.8), ry + Inches(0.12), row_w - Inches(1.9), row_h - Inches(0.24))
        rtf = rbox.text_frame
        rtf.word_wrap = True

        rp = rtf.paragraphs[0]
        rp.text = f"{name} ({en_name})"
        rp.font.size = Pt(14)
        rp.font.bold = True
        rp.font.color.rgb = accent if idx == 4 else TEXT_WHITE

        rp2 = rtf.add_paragraph()
        rp2.text = f"Điểm số: {score}    |    Khối lượng: {mass}    |    Tốc độ kéo: {speed}    |    Tỉ lệ xuất hiện: {spawn_rate}"
        rp2.font.size = Pt(11)
        rp2.font.color.rgb = TEXT_MUTED

    # =========================================================================
    # SLIDE 5: THIẾT KẾ MÀN CHƠI & MENU CHỌN LEVEL (LEVEL PROGRESSION)
    # =========================================================================
    slide5 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide5)
    add_header(slide5, "Màn chơi & Chọn Level", "Cấu trúc 3 Màn chơi Thử thách & Menu Linh hoạt")

    levels_data = [
        ("MÀN 1: KHỞI ĐỘNG",
         "Làm quen nhịp độ",
         "60 giây",
         "300 điểm",
         "15% (2-3 quả)",
         ["Vật phẩm phân bố thưa, dễ kéo.",
          "Mục tiêu điểm số vừa sức.",
          "Luyện phản xạ canh góc móc câu."],
         GOLD_MAIN),
        ("MÀN 2: TĂNG TỐC",
         "Áp lực thời gian",
         "50 giây",
         "900 điểm",
         "20% (3-4 quả)",
         ["Thời gian rút ngắn xuống 50s.",
          "Mục tiêu gấp 3 lần Màn 1.",
          "Đòi hỏi nhắm chuẩn vàng lớn và kim cương."],
         ACCENT_BLUE),
        ("MÀN 3: ĐỈNH CAO",
         "Thử thách chuyên gia",
         "45 giây",
         "1200 điểm",
         "25% (nhiều bom)",
         ["Mật độ bom dày đặc quanh các mỏ vàng.",
          "Chỉ 45s để đạt 1200 điểm cao kỉ lục.",
          "Một cú kéo sai chạm bom là thua ngay!"],
         ACCENT_RED)
    ]

    l_w = Inches(3.64)
    l_gap = Inches(0.4)
    l_x = Inches(0.8)

    for i, (ltitle, lsub, ltime, ltarget, lbomb, desc_list, laccent) in enumerate(levels_data):
        lx = l_x + i * (l_w + l_gap)
        add_card(slide5, lx, Inches(1.8), l_w, Inches(4.9), CARD_BG, laccent)

        tbox = slide5.shapes.add_textbox(lx + Inches(0.2), Inches(2.0), l_w - Inches(0.4), Inches(4.5))
        tf = tbox.text_frame
        tf.word_wrap = True

        p1 = tf.paragraphs[0]
        p1.text = ltitle
        p1.font.size = Pt(11)
        p1.font.bold = True
        p1.font.color.rgb = laccent
        p1.space_after = Pt(4)

        p2 = tf.add_paragraph()
        p2.text = lsub
        p2.font.size = Pt(16)
        p2.font.bold = True
        p2.font.color.rgb = TEXT_WHITE
        p2.space_after = Pt(14)

        p_stats = tf.add_paragraph()
        p_stats.text = f"⏱ Thời gian: {ltime}\n🎯 Mục tiêu: {ltarget}\n💣 Tỉ lệ Bom: {lbomb}"
        p_stats.font.size = Pt(12)
        p_stats.font.bold = True
        p_stats.font.color.rgb = GOLD_LIGHT
        p_stats.space_after = Pt(14)

        for d in desc_list:
            pd = tf.add_paragraph()
            pd.text = "• " + d
            pd.font.size = Pt(11)
            pd.font.color.rgb = TEXT_MUTED
            pd.space_after = Pt(6)

    # =========================================================================
    # SLIDE 6: TỐI ƯU CAMERA VỪA MÀN HÌNH (CAMERA & RESPONSIVE VIEW)
    # =========================================================================
    slide6 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide6)
    add_header(slide6, "Tối ưu hiển thị", "Căn chỉnh Main Camera & Đa Màn hình (Dọc / Ngang)")

    col_w = Inches(5.65)
    gap = Inches(0.433)

    # Left Card: Problem
    add_card(slide6, Inches(0.8), Inches(1.8), col_w, Inches(4.9), CARD_BG, CARD_BORDER)
    lbox = slide6.shapes.add_textbox(Inches(1.05), Inches(2.05), col_w - Inches(0.5), Inches(4.4))
    ltf = lbox.text_frame
    ltf.word_wrap = True

    lp1 = ltf.paragraphs[0]
    lp1.text = "VẤN ĐỀ TRƯỚC CẢI TIẾN"
    lp1.font.size = Pt(11)
    lp1.font.bold = True
    lp1.font.color.rgb = ACCENT_RED
    lp1.space_after = Pt(8)

    lp2 = ltf.add_paragraph()
    lp2.text = "Góc nhìn bị cắt xén & lệch vùng chơi"
    lp2.font.size = Pt(17)
    lp2.font.bold = True
    lp2.font.color.rgb = TEXT_WHITE
    lp2.space_after = Pt(16)

    prob_bullets = [
        "Camera ban đầu đặt tại Y = 0 cố định, orthographicSize = 6.",
        "Vật phẩm sinh sâu tới Y = -10, dẫn đến hơn 40% vật phẩm nằm ngoài đáy màn hình, người chơi không thể nhìn thấy.",
        "Khoảng trống bên trên thừa thãi, trong khi bên dưới bị che khuất.",
        "Chuyển sang màn hình dọc (9:16) bị viền đen hoặc mất nội dung hai bên."
    ]
    for b in prob_bullets:
        pb = ltf.add_paragraph()
        pb.text = "✕ " + b
        pb.font.size = Pt(12)
        pb.font.color.rgb = RGBColor(248, 113, 113)
        pb.space_after = Pt(10)

    # Right Card: Solution
    add_card(slide6, Inches(0.8) + col_w + gap, Inches(1.8), col_w, Inches(4.9), CARD_BG, GOLD_MAIN)
    rbox = slide6.shapes.add_textbox(Inches(0.8) + col_w + gap + Inches(0.25), Inches(2.05), col_w - Inches(0.5), Inches(4.4))
    rtf = rbox.text_frame
    rtf.word_wrap = True

    rp1 = rtf.paragraphs[0]
    rp1.text = "GIẢI PHÁP VỚI GAMEVIEWADAPTER"
    rp1.font.size = Pt(11)
    rp1.font.bold = True
    rp1.font.color.rgb = GOLD_MAIN
    rp1.space_after = Pt(8)

    rp2 = rtf.add_paragraph()
    rp2.text = "Tự động căn tâm & thích ứng tỷ lệ"
    rp2.font.size = Pt(17)
    rp2.font.bold = True
    rp2.font.color.rgb = TEXT_WHITE
    rp2.space_after = Pt(16)

    sol_bullets = [
        "Định vị tâm Camera tối ưu: Y = -1.5f (màn hình ngang) và Y = -0.6f (màn hình dọc).",
        "Thu gọn vùng spawnAreaY từ Y = 0.8f đến Y = -5.8f: 100% vật phẩm luôn nằm trọn trong khung nhìn.",
        "Tự động tính toán orthographicSize theo tỷ lệ khung hình thực tế để chiều rộng luôn >= 9.2f.",
        "Background tự động co giãn theo chế độ Aspect Fill Cover: hoàn toàn không còn viền đen trên mọi độ phân giải."
    ]
    for b in sol_bullets:
        pb = rtf.add_paragraph()
        pb.text = "✓ " + b
        pb.font.size = Pt(12)
        pb.font.color.rgb = RGBColor(187, 247, 208)
        pb.space_after = Pt(10)

    # =========================================================================
    # SLIDE 7: CƠ CHẾ BOM NỔ "KÉO LÀ THUA" (BOMB HAZARD FEATURE)
    # =========================================================================
    slide7 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide7)
    add_header(slide7, "Tính năng mới", "Cơ chế Cạm bẫy Bom Nổ — Kéo Là Thua")

    b_cards = [
        ("PHÁT HIỆN TỨC THÌ",
         "Nhận diện va chạm Bomb",
         "Khi OverlapCircle của móc chạm Collider của bom, HookController lập tức ngắt trạng thái kéo thường và kích hoạt ExplodeBomb().",
         "Assets/Sprites/Items/bomb.png",
         GOLD_MAIN),
        ("HIỆU ỨNG NỔ & RUNG",
         "VFX & Camera Shake",
         "Sinh hiệu ứng nổ đa tầng rực lửa, phóng to và mờ dần trong 0.45s; đồng thời camera rung lắc mạnh tạo cảm giác uy lực chân thực.",
         "Assets/Sprites/Items/explosion.png",
         ACCENT_RED),
        ("GAME OVER TỨC KHẮC",
         "Kết thúc màn chơi",
         "Móc thu tức thì về gốc, dừng đếm giờ và kích hoạt Game Over kèm lý do thua cuộc: 'Bạn đã kéo phải bom! Bị nổ tung!'.",
         "Assets/Sprites/Player/miner.png",
         GOLD_LIGHT)
    ]

    for i, (b_title, b_sub, b_desc, b_img, b_color) in enumerate(b_cards):
        bx = start_x + i * (card_w + card_gap)
        add_card(slide7, bx, Inches(1.8), card_w, Inches(4.9), CARD_BG, b_color if i == 1 else CARD_BORDER)

        if os.path.exists(b_img):
            slide7.shapes.add_picture(b_img, bx + Inches(1.3), Inches(2.1), width=Inches(1.1))

        tbox = slide7.shapes.add_textbox(bx + Inches(0.2), Inches(3.4), card_w - Inches(0.4), Inches(3.1))
        tf = tbox.text_frame
        tf.word_wrap = True

        p1 = tf.paragraphs[0]
        p1.text = b_title
        p1.font.size = Pt(11)
        p1.font.bold = True
        p1.font.color.rgb = b_color
        p1.space_after = Pt(4)

        p2 = tf.add_paragraph()
        p2.text = b_sub
        p2.font.size = Pt(16)
        p2.font.bold = True
        p2.font.color.rgb = TEXT_WHITE
        p2.space_after = Pt(12)

        p3 = tf.add_paragraph()
        p3.text = b_desc
        p3.font.size = Pt(12)
        p3.font.color.rgb = TEXT_MUTED

    # =========================================================================
    # SLIDE 8: KIẾN TRÚC KỸ THUẬT & MẪU THIẾT KẾ (TECHNICAL ARCHITECTURE)
    # =========================================================================
    slide8 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide8)
    add_header(slide8, "Kiến trúc hệ thống", "Các Mẫu Thiết Kế Hướng Đối Tượng (Design Patterns)")

    patterns = [
        ("SINGLETON PATTERN", "GameManager.Instance",
         "Đảm bảo duy nhất 1 điểm điều phối vòng đời toàn bộ game (Menu -> LevelSelect -> Playing -> GameOver/Complete), dễ dàng truy xuất toàn cục an toàn.",
         GOLD_MAIN),
        ("OBJECT POOLING", "ItemSpawner & ObjectPool",
         "Tái sử dụng các GameObject vật phẩm (vàng, đá, bom) giữa các lượt chơi thay vì Instantiate/Destroy liên tục, loại bỏ hoàn toàn giật lag do Garbage Collection.",
         ACCENT_BLUE),
        ("SCRIPTABLE OBJECT", "LevelData Asset",
         "Dữ liệu hóa thông số màn chơi (thời gian, mục tiêu điểm, danh sách prefab, tỉ lệ spawn). Cho phép Game Designer chỉnh thông số mà không cần động vào code.",
         GOLD_LIGHT),
        ("EVENT-DRIVEN UI", "Action<T> Decoupling",
         "GameManager và LevelManager phát sự kiện OnScoreChanged, OnTimeChanged. UI subscribe nhận tin mà không phụ thuộc ngược lại vào Gameplay logic.",
         ACCENT_GREEN)
    ]

    p_w = Inches(5.65)
    p_h = Inches(2.25)
    p_gap_x = Inches(0.433)
    p_gap_y = Inches(0.3)

    coords = [
        (Inches(0.8), Inches(1.8)),
        (Inches(0.8) + p_w + p_gap_x, Inches(1.8)),
        (Inches(0.8), Inches(1.8) + p_h + p_gap_y),
        (Inches(0.8) + p_w + p_gap_x, Inches(1.8) + p_h + p_gap_y)
    ]

    for (pname, pcode, pdesc, pcol), (px, py) in zip(patterns, coords):
        add_card(slide8, px, py, p_w, p_h, CARD_BG, CARD_BORDER)

        tbox = slide8.shapes.add_textbox(px + Inches(0.3), py + Inches(0.2), p_w - Inches(0.6), p_h - Inches(0.4))
        tf = tbox.text_frame
        tf.word_wrap = True

        p1 = tf.paragraphs[0]
        p1.text = pname
        p1.font.size = Pt(11)
        p1.font.bold = True
        p1.font.color.rgb = pcol
        p1.space_after = Pt(2)

        p2 = tf.add_paragraph()
        p2.text = pcode
        p2.font.size = Pt(15)
        p2.font.bold = True
        p2.font.color.rgb = TEXT_WHITE
        p2.space_after = Pt(8)

        p3 = tf.add_paragraph()
        p3.text = pdesc
        p3.font.size = Pt(11)
        p3.font.color.rgb = TEXT_MUTED

    # =========================================================================
    # SLIDE 9: TỔNG KẾT & HƯỚNG PHÁT TRIỂN (CONCLUSION & ROADMAP)
    # =========================================================================
    slide9 = prs.slides.add_slide(blank_layout)
    set_slide_bg(slide9)
    add_header(slide9, "Tổng kết & Tương lai", "Kết Quả Đạt Được & Lộ Trình Phát Triển Tiếp Theo")

    # Left Column: Achieved
    add_card(slide9, Inches(0.8), Inches(1.8), col_w, Inches(4.9), CARD_BG, ACCENT_GREEN)
    c1_box = slide9.shapes.add_textbox(Inches(1.05), Inches(2.05), col_w - Inches(0.5), Inches(4.4))
    c1_tf = c1_box.text_frame
    c1_tf.word_wrap = True

    cp1 = c1_tf.paragraphs[0]
    cp1.text = "KẾT QUẢ ĐẠT ĐƯỢC"
    cp1.font.size = Pt(11)
    cp1.font.bold = True
    cp1.font.color.rgb = ACCENT_GREEN
    cp1.space_after = Pt(8)

    cp2 = c1_tf.add_paragraph()
    cp2.text = "Dự án hoàn thiện & Tối ưu cao"
    cp2.font.size = Pt(17)
    cp2.font.bold = True
    cp2.font.color.rgb = TEXT_WHITE
    cp2.space_after = Pt(16)

    achieved = [
        "Hoàn chỉnh Gameplay Đào Vàng 2D mượt mà, đúng chuẩn vật lý.",
        "Camera tự động căn chỉnh trọn vẹn khu vực chơi ở cả màn hình Dọc và Ngang.",
        "Tích hợp Bom cạm bẫy với hiệu ứng phát nổ và rung lắc kịch tính.",
        "Menu chọn tự do 3 màn chơi thử thách tăng dần độ khó.",
        "Mã nguồn C# sạch sẽ, kiến trúc hướng đối tượng chuẩn mực, 0 lỗi biên dịch."
    ]
    for a in achieved:
        pa = c1_tf.add_paragraph()
        pa.text = "★ " + a
        pa.font.size = Pt(12)
        pa.font.color.rgb = RGBColor(187, 247, 208)
        pa.space_after = Pt(10)

    # Right Column: Future Roadmap
    add_card(slide9, Inches(0.8) + col_w + gap, Inches(1.8), col_w, Inches(4.9), CARD_BG, GOLD_MAIN)
    c2_box = slide9.shapes.add_textbox(Inches(0.8) + col_w + gap + Inches(0.25), Inches(2.05), col_w - Inches(0.5), Inches(4.4))
    c2_tf = c2_box.text_frame
    c2_tf.word_wrap = True

    c2_p1 = c2_tf.paragraphs[0]
    c2_p1.text = "LỘ TRÌNH MỞ RỘNG (ROADMAP)"
    c2_p1.font.size = Pt(11)
    c2_p1.font.bold = True
    c2_p1.font.color.rgb = GOLD_MAIN
    c2_p1.space_after = Pt(8)

    c2_p2 = c2_tf.add_paragraph()
    c2_p2.text = "Các tính năng tiềm năng tiếp theo"
    c2_p2.font.size = Pt(17)
    c2_p2.font.bold = True
    c2_p2.font.color.rgb = TEXT_WHITE
    c2_p2.space_after = Pt(16)

    future = [
        "Cửa hàng vật phẩm giữa các màn (Thuốc nổ Dynamite phá đá, Nước tăng lực kéo nhanh, Cỏ may mắn tăng giá trị kim cương).",
        "Sinh vật di chuyển ngầm (Chuột cõng kim cương chạy ngang màn hình).",
        "Hệ thống nhiệm vụ thành tựu & Bảng xếp hạng điểm số trực tuyến (Leaderboard).",
        "Bộ âm thanh sống động (tiếng động cơ tời, tiếng nổ boom, nhạc nền vui nhộn)."
    ]
    for f in future:
        pf = c2_tf.add_paragraph()
        pf.text = "→ " + f
        pf.font.size = Pt(12)
        pf.font.color.rgb = GOLD_LIGHT
        pf.space_after = Pt(10)

    # Save presentation
    output_path = "DaoVang_Game_Presentation.pptx"
    prs.save(output_path)
    print(f"Presentation saved successfully to {output_path}!")

if __name__ == "__main__":
    create_presentation()
