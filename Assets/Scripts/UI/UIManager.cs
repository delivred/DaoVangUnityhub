using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaoVang.Core;
using DaoVang.Data;
using DaoVang.Level;
using DaoVang.Shop;

namespace DaoVang.UI
{
    /// <summary>
    /// UIManager điều phối toàn bộ giao diện trò chơi Đào Vàng:
    /// - Menu chính hoàn chỉnh: Thương hiệu Đào Vàng Classic, Kỷ lục, Túi Token, 5 nút chức năng.
    /// - Màn chọn Level (Level Select) 8 Màn chơi thử thách với 2 trang chuyển đổi mượt mà.
    /// - Cửa hàng Buff (Shop) với 6 loại buff độc đáo mua bằng TOKEN.
    /// - HUD khi chơi: Điểm số nảy số, Mục tiêu, Thời gian (nhấp nháy khẩn cấp), Token, Nút TNT [Space], Icon Buff.
    /// - Bảng Level Complete & Game Over chi tiết thưởng Token và điều hướng linh hoạt.
    /// - Modal Hướng Dẫn (Tutorial) trực quan.
    /// - Tối ưu hóa bộ nhớ: Triệt tiêu hoàn toàn rác chuỗi (string allocations) mỗi frame.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panel gốc — bật/tắt theo GameState")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject shopPanel;

        [Header("Tham chiếu module")]
        [SerializeField] private LevelManager levelManager;

        // Menu Components
        private TMP_Text _menuTitleText;
        private TMP_Text _menuTokenText;
        private TMP_Text _menuHighScoreText;
        private Button _playButton;
        private Button _menuShopButton;
        private Button _tutorialButton;
        private Button _soundToggleButton;
        private TMP_Text _soundToggleText;
        private Button _quitButton;

        // Level Select Components (8 Màn chơi, 2 trang: 1-4 và 5-8)
        private TMP_Text _levelSelectTitleText;
        private TMP_Text _levelSelectTokenText;
        private readonly List<GameObject> _levelCardObjs = new();
        private Button _prevPageButton;
        private Button _nextPageButton;
        private TMP_Text _pageIndicatorText;
        private Button _backToMenuButton;
        private GameObject _lockedLevelToastObj;
        private TMP_Text _lockedToastText;
        private Coroutine _lockedToastCoroutine;
        private int _currentLevelPage = 0; // 0: Màn 1-4, 1: Màn 5-8
        private const int LevelsPerPage = 4;
        private const int TotalLevels = 8;

        // Playing HUD Components
        private TMP_Text _scoreText;
        private TMP_Text _timeText;
        private TMP_Text _targetText;
        private TMP_Text _hudTokenText;
        private TMP_Text _hudBuffStatusText;
        private Button _dynamiteHudButton;
        private TMP_Text _dynamiteHudText;

        // Level Complete Components
        private TMP_Text _completeTitleText;
        private TMP_Text _levelCompleteScoreText;
        private TMP_Text _rewardText;
        private GameObject _nextLevelButton;
        private Button _completeShopButton;
        private Button _completeMenuButton;

        // Game Over Components
        private TMP_Text _gameOverTitleText;
        private TMP_Text _gameOverReasonText;
        private TMP_Text _gameOverScoreText;
        private Button _gameOverRetryButton;
        private Button _gameOverShopButton;
        private Button _gameOverMenuButton;

        // Shop Components
        private TMP_Text _shopTitleText;
        private TMP_Text _shopTokenText;
        private Button _closeShopButton;
        private Button _buffTabButton;
        private TMP_Text _buffTabText;
        private Button _skinTabButton;
        private TMP_Text _skinTabText;
        private int _currentShopTab = 0; // 0: Nâng cấp & Buff, 1: Skin Móc VIP
        private readonly List<GameObject> _buffCardObjs = new();
        private readonly List<GameObject> _skinCardObjs = new();
        private readonly List<Button> _skinCardBtns = new();
        private readonly List<TMP_Text> _skinCardTexts = new();
        private Button _buyPullPowerBtn;
        private Button _buyCableSpeedBtn;
        private Button _buyLuckyHookBtn;
        private Button _buyDynamiteBtn;
        private Button _buyEnergyDrinkBtn;
        private Button _buyBombShieldBtn;
        private TMP_Text _pullPowerTxt;
        private TMP_Text _cableSpeedTxt;
        private TMP_Text _luckyHookTxt;
        private TMP_Text _dynamiteTxt;
        private TMP_Text _energyDrinkTxt;
        private TMP_Text _bombShieldTxt;

        // Tutorial Modal Component
        private GameObject _tutorialModal;

        // Login Modal Components
        private GameObject _loginModal;
        private Button _loginButton;
        private TMP_Text _loginButtonText;
        private TMP_InputField _usernameInput;
        private TMP_InputField _passwordInput;
        private TMP_Text _loginCurrentAccountText;
        private TMP_Text _loginFeedbackText;
        private Coroutine _loginFeedbackCoroutine;

        // Bộ đệm hiệu năng tối ưu hóa chuỗi (Zero GC String Optimization)
        private float _displayedScore;
        private int _targetScore;
        private int _lastDisplayedScoreInt = -1;
        private int _lastFormattedSeconds = -1;
        private int _lastDynamiteCount = -1;
        private Coroutine _scorePunchCoroutine;

        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("à", "a").Replace("á", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ằ", "a").Replace("ắ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                .Replace("â", "a").Replace("ầ", "a").Replace("ấ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                .Replace("đ", "d").Replace("Đ", "D")
                .Replace("è", "e").Replace("é", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ề", "e").Replace("ế", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                .Replace("ì", "i").Replace("í", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ò", "o").Replace("ó", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ồ", "o").Replace("ố", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                .Replace("ơ", "o").Replace("ờ", "o").Replace("ớ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                .Replace("ù", "u").Replace("ú", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ừ", "u").Replace("ứ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                .Replace("ỳ", "y").Replace("ý", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                .Replace("À", "A").Replace("Á", "A").Replace("Ả", "A").Replace("Ã", "A").Replace("Ạ", "A")
                .Replace("Ă", "A").Replace("Ằ", "A").Replace("Ắ", "A").Replace("Ẳ", "A").Replace("Ẵ", "A").Replace("Ặ", "A")
                .Replace("Â", "A").Replace("Ầ", "A").Replace("Ấ", "A").Replace("Ẩ", "A").Replace("Ẫ", "A").Replace("Ậ", "A")
                .Replace("È", "E").Replace("É", "E").Replace("Ẻ", "E").Replace("Ẽ", "E").Replace("Ẹ", "E")
                .Replace("Ê", "E").Replace("Ề", "E").Replace("Ế", "E").Replace("Ể", "E").Replace("Ễ", "E").Replace("Ệ", "E")
                .Replace("Ì", "I").Replace("Í", "I").Replace("Ỉ", "I").Replace("Ĩ", "I").Replace("Ị", "I")
                .Replace("Ò", "O").Replace("Ó", "O").Replace("Ỏ", "O").Replace("Õ", "O").Replace("Ọ", "O")
                .Replace("Ô", "O").Replace("Ồ", "O").Replace("Ố", "O").Replace("Ổ", "O").Replace("Ỗ", "O").Replace("Ộ", "O")
                .Replace("Ơ", "O").Replace("Ờ", "O").Replace("Ớ", "O").Replace("Ở", "O").Replace("Ỡ", "O").Replace("Ợ", "O")
                .Replace("Ù", "U").Replace("Ú", "U").Replace("Ủ", "U").Replace("Ũ", "U").Replace("Ụ", "U")
                .Replace("Ư", "U").Replace("Ừ", "U").Replace("Ứ", "U").Replace("Ử", "U").Replace("Ữ", "U").Replace("Ự", "U")
                .Replace("Ỳ", "Y").Replace("Ý", "Y").Replace("Ỷ", "Y").Replace("Ỹ", "Y").Replace("Ỵ", "Y");
        }

        private void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();
#else
            if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
#endif

            BuildOrSetupMainMenu();
            BuildOrSetupLevelSelect();
            BuildOrSetupShop();
            BuildOrSetupPlayingHUD();
            BuildOrSetupGameOverAndComplete();
            BuildTutorialModal();
            BuildLoginModal();

            SanitizeAllTexts();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnScoreChanged += HandleScoreChanged;
            }
            if (levelManager != null)
            {
                levelManager.OnTimeChanged += HandleTimeChanged;
            }
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopDataChanged += HandleShopDataChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
            }
            if (levelManager != null)
            {
                levelManager.OnTimeChanged -= HandleTimeChanged;
            }
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopDataChanged -= HandleShopDataChanged;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                if (Mathf.Abs(_displayedScore - _targetScore) > 0.5f)
                {
                    float speed = Mathf.Max(60f, Mathf.Abs(_targetScore - _displayedScore) * 8f);
                    _displayedScore = Mathf.MoveTowards(_displayedScore, _targetScore, speed * Time.deltaTime);

                    int scoreInt = Mathf.RoundToInt(_displayedScore);
                    if (scoreInt != _lastDisplayedScoreInt)
                    {
                        _lastDisplayedScoreInt = scoreInt;
                        if (_scoreText != null)
                        {
                            _scoreText.text = $"DIEM: {_lastDisplayedScoreInt}";
                        }
                    }
                }
            }
        }

        private void SanitizeAllTexts()
        {
            var allTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                if (t != null && !string.IsNullOrEmpty(t.text))
                {
                    t.text = SanitizeText(t.text);
                }
            }
        }

        // =========================================================================
        // 1. MENU CHÍNH HOÀN CHỈNH (MAIN MENU)
        // =========================================================================
        private void BuildOrSetupMainMenu()
        {
            if (menuPanel == null)
            {
                Transform mp = transform.Find("MenuPanel");
                if (mp != null) menuPanel = mp.gameObject;
            }
            if (menuPanel == null) return;

            RectTransform menuRt = menuPanel.GetComponent<RectTransform>();
            if (menuRt != null)
            {
                menuRt.anchorMin = Vector2.zero;
                menuRt.anchorMax = Vector2.one;
                menuRt.sizeDelta = Vector2.zero;
                menuRt.anchoredPosition = Vector2.zero;
            }

            Image bgImg = menuPanel.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            }

            // Tiêu đề Game
            Transform tTrans = menuPanel.transform.Find("TitleText") ?? menuPanel.transform.Find("Title");
            if (tTrans != null)
            {
                _menuTitleText = tTrans.GetComponent<TMP_Text>();
                RectTransform rt = tTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 185f);
                    rt.sizeDelta = new Vector2(650f, 75f);
                }
                if (_menuTitleText != null)
                {
                    _menuTitleText.text = "DAO VANG CLASSIC";
                    _menuTitleText.fontSize = 38f;
                    _menuTitleText.fontStyle = FontStyles.Bold;
                    _menuTitleText.color = new Color(1f, 0.85f, 0.2f);
                    _menuTitleText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Dọn dẹp triệt để Text rác "InstructionText" ("New Text") mặc định của Unity trong MenuPanel
            Transform instrTrans = menuPanel.transform.Find("InstructionText");
            if (instrTrans != null)
            {
                Destroy(instrTrans.gameObject);
            }

            // Thanh hiển thị TOKEN và KỶ LỤC trên Menu
            Transform tokenTrans = menuPanel.transform.Find("MenuTokenText");
            if (tokenTrans == null && _menuTitleText != null)
            {
                GameObject tokObj = Instantiate(_menuTitleText.gameObject, menuPanel.transform);
                tokObj.name = "MenuTokenText";
                tokenTrans = tokObj.transform;
            }
            if (tokenTrans != null)
            {
                _menuTokenText = tokenTrans.GetComponent<TMP_Text>();
                RectTransform rt = tokenTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 130f);
                    rt.sizeDelta = new Vector2(650f, 40f);
                }
                if (_menuTokenText != null)
                {
                    _menuTokenText.fontSize = 20f;
                    _menuTokenText.fontStyle = FontStyles.Bold;
                    _menuTokenText.color = new Color(1f, 0.92f, 0.45f);
                    _menuTokenText.alignment = TextAlignmentOptions.Center;
                }
            }

            // 0. NÚT ĐĂNG NHẬP / TÀI KHOẢN (Login / Account Bar)
            Transform playTrans = menuPanel.transform.Find("PlayButton") ?? menuPanel.transform.Find("Button");
            if (playTrans != null) _playButton = playTrans.GetComponent<Button>();

            _loginButton = SetupOrCloneButton(menuPanel, "LoginButton", _playButton, new Vector2(0f, 82f), new Vector2(400f, 38f),
                new Color(0.18f, 0.25f, 0.35f, 0.98f), "🔑  DANG NHAP / TAI KHOAN", 16f, OnClickOpenLogin);
            if (_loginButton != null) _loginButtonText = _loginButton.GetComponentInChildren<TMP_Text>();

            // 1. NÚT CHƠI NGAY (Play)
            if (_playButton != null)
            {
                RectTransform rt = _playButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 28f);
                    rt.sizeDelta = new Vector2(300f, 52f);
                }
                Image pImg = _playButton.GetComponent<Image>();
                if (pImg != null) pImg.color = new Color(0.12f, 0.65f, 0.28f, 1f);

                TMP_Text txt = _playButton.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = "▶  CHOI NGAY";
                    txt.fontSize = 22f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = Color.white;
                }
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(OnClickPlay);
            }

            // 2. NÚT CỬA HÀNG BUFF (Shop)
            _menuShopButton = SetupOrCloneButton(menuPanel, "ShopButton", _playButton, new Vector2(0f, -28f), new Vector2(300f, 48f),
                new Color(0.88f, 0.55f, 0.1f, 1f), "🛒  CUA HANG BUFF", 19f, OnClickOpenShop);

            // 3. NÚT HƯỚNG DẪN (Tutorial)
            _tutorialButton = SetupOrCloneButton(menuPanel, "TutorialButton", _playButton, new Vector2(0f, -80f), new Vector2(300f, 46f),
                new Color(0.2f, 0.52f, 0.85f, 1f), "📖  HUONG DAN CHOI", 18f, OnClickOpenTutorial);

            // 4. NÚT ÂM THANH (Sound Toggle)
            _soundToggleButton = SetupOrCloneButton(menuPanel, "SoundButton", _playButton, new Vector2(0f, -132f), new Vector2(300f, 44f),
                new Color(0.28f, 0.33f, 0.42f, 1f), "🔊  AM THANH: BAT", 17f, OnClickToggleSound);
            if (_soundToggleButton != null) _soundToggleText = _soundToggleButton.GetComponentInChildren<TMP_Text>();

            // 5. NÚT THOÁT GAME (Quit)
            _quitButton = SetupOrCloneButton(menuPanel, "QuitButton", _playButton, new Vector2(0f, -182f), new Vector2(300f, 42f),
                new Color(0.45f, 0.2f, 0.22f, 1f), "✖  THOAT GAME", 16f, OnClickQuitGame);

            UpdateMainMenuAccountDisplay();

            // Cập nhật trạng thái âm thanh đã lưu
            float savedVol = SaveSystem.LoadSoundVolume();
            AudioListener.volume = savedVol;
            if (_soundToggleText != null)
            {
                _soundToggleText.text = savedVol > 0.1f ? "🔊  AM THANH: BAT" : "🔇  AM THANH: TAT";
            }
        }

        private Button SetupOrCloneButton(GameObject parent, string name, Button template, Vector2 anchoredPos, Vector2 size, Color bgColor, string label, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            Transform existing = parent.transform.Find(name);
            GameObject btnObj = existing != null ? existing.gameObject : (template != null ? Instantiate(template.gameObject, parent.transform) : null);
            if (btnObj == null) return null;

            btnObj.name = name;
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            Image img = btnObj.GetComponent<Image>();
            if (img != null) img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if (onClick != null) btn.onClick.AddListener(onClick);
            }
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = label;
                txt.fontSize = fontSize;
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
            }
            return btn;
        }

        // =========================================================================
        // 2. MÀN CHỌN LEVEL ĐA MÀN (LEVEL SELECT PANEL - 8 LEVELS, 2 PAGES)
        // =========================================================================
        private void BuildOrSetupLevelSelect()
        {
            if (levelSelectPanel == null)
            {
                Transform lp = transform.Find("LevelSelectPanel");
                if (lp != null) levelSelectPanel = lp.gameObject;
            }
            if (levelSelectPanel == null) return;

            RectTransform lsRt = levelSelectPanel.GetComponent<RectTransform>();
            if (lsRt != null)
            {
                lsRt.anchorMin = Vector2.zero;
                lsRt.anchorMax = Vector2.one;
                lsRt.sizeDelta = Vector2.zero;
                lsRt.anchoredPosition = Vector2.zero;
            }

            Image bgImg = levelSelectPanel.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            }

            // Tiêu đề
            Transform tTrans = levelSelectPanel.transform.Find("TitleText") ?? levelSelectPanel.transform.Find("Title");
            if (tTrans != null)
            {
                _levelSelectTitleText = tTrans.GetComponent<TMP_Text>();
                RectTransform rt = tTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 210f);
                    rt.sizeDelta = new Vector2(650f, 50f);
                }
                if (_levelSelectTitleText != null)
                {
                    _levelSelectTitleText.text = "CHON MAN CHOI";
                    _levelSelectTitleText.fontSize = 32f;
                    _levelSelectTitleText.fontStyle = FontStyles.Bold;
                    _levelSelectTitleText.color = new Color(1f, 0.85f, 0.2f);
                    _levelSelectTitleText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Token
            Transform tokTrans = levelSelectPanel.transform.Find("LevelSelectTokenText");
            if (tokTrans == null && _levelSelectTitleText != null)
            {
                GameObject tokObj = Instantiate(_levelSelectTitleText.gameObject, levelSelectPanel.transform);
                tokObj.name = "LevelSelectTokenText";
                tokTrans = tokObj.transform;
            }
            if (tokTrans != null)
            {
                _levelSelectTokenText = tokTrans.GetComponent<TMP_Text>();
                RectTransform rt = tokTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 165f);
                    rt.sizeDelta = new Vector2(650f, 35f);
                }
                if (_levelSelectTokenText != null)
                {
                    _levelSelectTokenText.fontSize = 19f;
                    _levelSelectTokenText.fontStyle = FontStyles.Bold;
                    _levelSelectTokenText.color = new Color(1f, 0.92f, 0.45f);
                    _levelSelectTokenText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Template Button
            Button templateBtn = _playButton;
            for (int i = 1; i <= 3; i++)
            {
                Transform bTrans = levelSelectPanel.transform.Find($"Level{i}Button") ?? levelSelectPanel.transform.Find($"Level{i}");
                if (bTrans != null && templateBtn == null) templateBtn = bTrans.GetComponent<Button>();
            }

            // Xây dựng 4 Card hiển thị trên 1 trang (Horizontal 4 cột)
            float[] xPositions = { -270f, -90f, 90f, 270f };
            _levelCardObjs.Clear();

            for (int slot = 0; slot < LevelsPerPage; slot++)
            {
                int cardIndex = slot;
                string cardName = $"LevelSlotCard_{cardIndex}";
                Transform existing = levelSelectPanel.transform.Find(cardName);
                GameObject cardObj = existing != null ? existing.gameObject : (templateBtn != null ? Instantiate(templateBtn.gameObject, levelSelectPanel.transform) : null);
                if (cardObj == null) continue;

                cardObj.name = cardName;
                RectTransform rt = cardObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(xPositions[slot], 0f);
                    rt.sizeDelta = new Vector2(170f, 240f);
                }

                _levelCardObjs.Add(cardObj);
            }

            // Dọn các button level tĩnh cũ nếu còn
            for (int i = 1; i <= 3; i++)
            {
                Transform old = levelSelectPanel.transform.Find($"Level{i}Button") ?? levelSelectPanel.transform.Find($"Level{i}");
                if (old != null && !_levelCardObjs.Contains(old.gameObject))
                {
                    Destroy(old.gameObject);
                }
            }

            // Nút Chuyển Trang (Pagination Controls)
            _prevPageButton = SetupOrCloneButton(levelSelectPanel, "PrevPageButton", templateBtn, new Vector2(-160f, -155f), new Vector2(150f, 42f),
                new Color(0.22f, 0.35f, 0.5f, 1f), "◀  TRANG TRUOC", 15f, OnClickPrevLevelPage);

            _nextPageButton = SetupOrCloneButton(levelSelectPanel, "NextPageButton", templateBtn, new Vector2(160f, -155f), new Vector2(150f, 42f),
                new Color(0.22f, 0.35f, 0.5f, 1f), "TRANG SAU  ▶", 15f, OnClickNextLevelPage);

            // Text hiển thị trang (TRANG 1/2) ở giữa 2 nút phân trang
            Transform pInd = levelSelectPanel.transform.Find("PageIndicatorText");
            if (pInd == null && _levelSelectTitleText != null)
            {
                GameObject pObj = Instantiate(_levelSelectTitleText.gameObject, levelSelectPanel.transform);
                pObj.name = "PageIndicatorText";
                pInd = pObj.transform;
            }
            if (pInd != null)
            {
                _pageIndicatorText = pInd.GetComponent<TMP_Text>();
                RectTransform prt = pInd.GetComponent<RectTransform>();
                if (prt != null)
                {
                    prt.anchorMin = new Vector2(0.5f, 0.5f);
                    prt.anchorMax = new Vector2(0.5f, 0.5f);
                    prt.anchoredPosition = new Vector2(0f, -155f);
                    prt.sizeDelta = new Vector2(140f, 40f);
                }
                if (_pageIndicatorText != null)
                {
                    _pageIndicatorText.fontSize = 17f;
                    _pageIndicatorText.fontStyle = FontStyles.Bold;
                    _pageIndicatorText.color = new Color(1f, 0.9f, 0.4f);
                    _pageIndicatorText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Nút "◀ QUAY LẠI MENU"
            _backToMenuButton = SetupOrCloneButton(levelSelectPanel, "BackButton", templateBtn, new Vector2(0f, -210f), new Vector2(250f, 44f),
                new Color(0.28f, 0.33f, 0.42f, 1f), "◀  QUAY LAI MENU", 18f, OnClickBackToMenuFromLevelSelect);

            // Popup / Toast thông báo khi bấm vào Màn đang bị khóa
            Transform existingToast = levelSelectPanel.transform.Find("LockedLevelToast");
            if (existingToast != null)
            {
                _lockedLevelToastObj = existingToast.gameObject;
                _lockedToastText = _lockedLevelToastObj.GetComponentInChildren<TMP_Text>();
            }
            else
            {
                _lockedLevelToastObj = new GameObject("LockedLevelToast");
                _lockedLevelToastObj.transform.SetParent(levelSelectPanel.transform, false);

                RectTransform toastRt = _lockedLevelToastObj.AddComponent<RectTransform>();
                toastRt.anchorMin = new Vector2(0.5f, 0.5f);
                toastRt.anchorMax = new Vector2(0.5f, 0.5f);
                toastRt.anchoredPosition = new Vector2(0f, 0f);
                toastRt.sizeDelta = new Vector2(640f, 105f);

                Image toastBg = _lockedLevelToastObj.AddComponent<Image>();
                toastBg.color = new Color(0.35f, 0.06f, 0.08f, 0.98f);

                Button toastBtn = _lockedLevelToastObj.AddComponent<Button>();
                toastBtn.onClick.AddListener(DismissLockedToast);

                GameObject toastTxtObj = new GameObject("ToastText");
                toastTxtObj.transform.SetParent(_lockedLevelToastObj.transform, false);
                RectTransform ttRt = toastTxtObj.AddComponent<RectTransform>();
                ttRt.anchorMin = Vector2.zero;
                ttRt.anchorMax = Vector2.one;
                ttRt.sizeDelta = new Vector2(-24f, -10f);

                _lockedToastText = toastTxtObj.AddComponent<TextMeshProUGUI>();
                _lockedToastText.fontSize = 16f;
                _lockedToastText.fontStyle = FontStyles.Bold;
                _lockedToastText.color = Color.white;
                _lockedToastText.alignment = TextAlignmentOptions.Center;
                _lockedToastText.textWrappingMode = TextWrappingModes.Normal;
            }
            _lockedLevelToastObj.SetActive(false);
        }

        public void OnClickPrevLevelPage()
        {
            DismissLockedToast();
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            _currentLevelPage = Mathf.Max(0, _currentLevelPage - 1);
            RefreshLevelSelectCards();
        }

        public void OnClickNextLevelPage()
        {
            DismissLockedToast();
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            int maxPages = Mathf.CeilToInt((float)TotalLevels / LevelsPerPage) - 1;
            _currentLevelPage = Mathf.Min(maxPages, _currentLevelPage + 1);
            RefreshLevelSelectCards();
        }

        private void RefreshLevelSelectCards()
        {
            int tokens = SaveSystem.LoadTokens();
            int wonCount = GameManager.Instance != null ? GameManager.Instance.TotalLevelsWon : SaveSystem.GetTotalLevelsWon(TotalLevels);

            if (_levelSelectTitleText != null)
            {
                _levelSelectTitleText.text = "TIEN DO VUOT AI (8 MAN CHOI)";
            }
            if (_levelSelectTokenText != null)
            {
                _levelSelectTokenText.text = $"★  TIEN DO: <color=#50FF80><b>{wonCount}/{TotalLevels} MAN DA THANG</b></color>   |   TOKEN: <color=#FFE600><b>{tokens}</b></color>  ★";
            }

            int startIndex = _currentLevelPage * LevelsPerPage;
            int maxPages = Mathf.CeilToInt((float)TotalLevels / LevelsPerPage) - 1;

            if (_prevPageButton != null) _prevPageButton.gameObject.SetActive(_currentLevelPage > 0);
            if (_nextPageButton != null) _nextPageButton.gameObject.SetActive(_currentLevelPage < maxPages);
            if (_pageIndicatorText != null)
            {
                _pageIndicatorText.text = $"TRANG {_currentLevelPage + 1}/{maxPages + 1}";
            }

            int unlockedIdx = GameManager.Instance != null ? GameManager.Instance.UnlockedLevelIndex : SaveSystem.LoadUnlockedLevel();

            for (int slot = 0; slot < LevelsPerPage; slot++)
            {
                if (slot >= _levelCardObjs.Count) continue;
                GameObject cardObj = _levelCardObjs[slot];
                if (cardObj == null) continue;

                int levelIdx = startIndex + slot;
                if (levelIdx >= TotalLevels)
                {
                    cardObj.SetActive(false);
                    continue;
                }
                cardObj.SetActive(true);

                Button btn = cardObj.GetComponent<Button>();
                bool isUnlocked = (levelIdx == 0) || (levelIdx <= unlockedIdx);
                bool isWon = GameManager.Instance != null ? GameManager.Instance.IsLevelWon(levelIdx) : SaveSystem.IsLevelWon(levelIdx);

                if (btn != null)
                {
                    // Luôn để interactable = true để bắt được sự kiện click cả khi màn bị khóa
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    int targetIdx = levelIdx;
                    if (isUnlocked)
                    {
                        btn.onClick.AddListener(() => OnClickSelectLevel(targetIdx));
                    }
                    else
                    {
                        btn.onClick.AddListener(() => OnClickLockedLevel(targetIdx));
                    }
                }

                Image img = cardObj.GetComponent<Image>();
                if (img != null)
                {
                    if (isWon)
                    {
                        // Đã chiến thắng vượt ải: Xanh lá cây thành tựu
                        img.color = new Color(0.12f, 0.44f, 0.22f, 0.98f);
                    }
                    else if (isUnlocked)
                    {
                        // Ải mới nhất đang mở: Xanh dương hoàng gia nổi bật
                        img.color = new Color(0.16f, 0.38f, 0.65f, 0.98f);
                    }
                    else
                    {
                        // Đang bị khóa: Tối trầm viền đỏ cảnh báo
                        img.color = new Color(0.18f, 0.12f, 0.14f, 0.92f);
                    }
                }

                TMP_Text txt = cardObj.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    LevelData lvl = GameManager.Instance != null ? GameManager.Instance.GetLevel(levelIdx) : null;
                    int target = lvl != null ? lvl.targetScore : GetDefaultTarget(levelIdx);
                    int time = lvl != null ? Mathf.RoundToInt(lvl.timeLimit) : GetDefaultTime(levelIdx);
                    int reward = GameManager.Instance != null ? GameManager.Instance.GetLevelReward(levelIdx) : (150 + levelIdx * 150);
                    string bomb = GameManager.Instance != null ? GameManager.Instance.GetLevelBombRate(levelIdx) : "20%";
                    string title = GameManager.Instance != null ? GameManager.Instance.GetLevelTitle(levelIdx) : $"MAN {levelIdx + 1}";

                    string statusBadge;
                    if (isWon)
                    {
                        statusBadge = "<color=#50FF80><b>✔  DA VUOT AI</b></color>\n<size=11><color=#A5FFA5>[BAM DE CHOI LAI]</color></size>";
                    }
                    else if (isUnlocked)
                    {
                        statusBadge = "<color=#FFE600><b>▶  CHOI NGAY (AI MOI)</b></color>\n<size=11><color=#FFF280>[DANG THU THACH]</color></size>";
                    }
                    else
                    {
                        statusBadge = $"<color=#FF5555><b>🔒  DANG KHOA</b></color>\n<size=11><color=#FF9999>[Thang Man {levelIdx} de mo]\n(Bam vao de xem)</color></size>";
                    }

                    string displayTitle = isUnlocked ? title : $"<color=#8890A0>{title} [KHOA]</color>";

                    txt.text = $"{displayTitle}\n\n" +
                               $"Muc tieu: {target}d\n" +
                               $"Thoi gian: {time}s\n" +
                               $"Thuong: +{reward}\n" +
                               $"Bom no: {bomb}\n\n" +
                               $"{statusBadge}";
                    txt.fontSize = 13.5f;
                    txt.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private int GetDefaultTarget(int idx)
        {
            switch (idx)
            {
                case 0: return 300;
                case 1: return 900;
                case 2: return 1200;
                case 3: return 1600;
                case 4: return 1800;
                case 5: return 2200;
                case 6: return 2600;
                case 7: return 3200;
                default: return 300 + idx * 400;
            }
        }

        private int GetDefaultTime(int idx)
        {
            switch (idx)
            {
                case 0: return 60;
                case 1: return 50;
                case 2: return 45;
                case 3: return 45;
                case 4: return 45;
                case 5: return 40;
                case 6: return 40;
                case 7: return 38;
                default: return 40;
            }
        }

        // =========================================================================
        // 3. CỬA HÀNG BUFF & TRANG BỊ (SHOP PANEL)
        // =========================================================================
        private void BuildOrSetupShop()
        {
            if (shopPanel == null)
            {
                Transform sp = transform.Find("ShopPanel");
                if (sp != null) shopPanel = sp.gameObject;
            }

            if (shopPanel == null && levelSelectPanel != null)
            {
                GameObject newShop = Instantiate(levelSelectPanel, transform);
                newShop.name = "ShopPanel";
                shopPanel = newShop;

                for (int i = 0; i < 4; i++)
                {
                    Transform old = shopPanel.transform.Find($"LevelSlotCard_{i}");
                    if (old != null) Destroy(old.gameObject);
                }
            }
            if (shopPanel == null) return;

            RectTransform sRt = shopPanel.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.anchorMin = Vector2.zero;
                sRt.anchorMax = Vector2.one;
                sRt.sizeDelta = Vector2.zero;
                sRt.anchoredPosition = Vector2.zero;
            }

            Image bgImg = shopPanel.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            }

            // 0. Khung nền chính của Cửa Hàng (Shop Modal Frame) to lớn và nổi bật
            Transform frameTrans = shopPanel.transform.Find("ShopModalFrame");
            if (frameTrans == null)
            {
                GameObject frameObj = new GameObject("ShopModalFrame");
                frameObj.transform.SetParent(shopPanel.transform, false);
                frameObj.transform.SetAsFirstSibling();
                RectTransform frt = frameObj.AddComponent<RectTransform>();
                frt.anchorMin = new Vector2(0.5f, 0.5f);
                frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.anchoredPosition = new Vector2(0f, -5f);
                frt.sizeDelta = new Vector2(920f, 550f);

                Image fImg = frameObj.AddComponent<Image>();
                fImg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            }
            else
            {
                frameTrans.SetAsFirstSibling();
                RectTransform frt = frameTrans.GetComponent<RectTransform>();
                if (frt != null)
                {
                    frt.anchorMin = new Vector2(0.5f, 0.5f);
                    frt.anchorMax = new Vector2(0.5f, 0.5f);
                    frt.anchoredPosition = new Vector2(0f, -5f);
                    frt.sizeDelta = new Vector2(920f, 550f);
                }
            }

            // 1. Tiêu đề Shop to rõ ràng
            Transform tTrans = shopPanel.transform.Find("TitleText") ?? shopPanel.transform.Find("Title");
            if (tTrans != null)
            {
                _shopTitleText = tTrans.GetComponent<TMP_Text>();
                RectTransform rt = tTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 222f);
                    rt.sizeDelta = new Vector2(750f, 48f);
                }
                if (_shopTitleText != null)
                {
                    _shopTitleText.text = "🛒  CUA HANG TRANG BI & BUFF";
                    _shopTitleText.fontSize = 32f;
                    _shopTitleText.fontStyle = FontStyles.Bold;
                    _shopTitleText.color = new Color(1f, 0.85f, 0.2f);
                    _shopTitleText.alignment = TextAlignmentOptions.Center;
                }
            }

            // 2. Khung hiển thị TOKEN vàng nổi bật ngay dưới tiêu đề (Dễ thấy nhất màn hình)
            Transform tokVaultTrans = shopPanel.transform.Find("TokenVaultBox");
            if (tokVaultTrans == null)
            {
                Transform oldCoins = shopPanel.transform.Find("CoinsText");
                if (oldCoins != null) Destroy(oldCoins.gameObject);
                Transform oldLvlToken = shopPanel.transform.Find("LevelSelectTokenText");
                if (oldLvlToken != null && oldLvlToken.parent == shopPanel.transform) Destroy(oldLvlToken.gameObject);

                GameObject vaultObj = new GameObject("TokenVaultBox");
                vaultObj.transform.SetParent(shopPanel.transform, false);
                RectTransform vrt = vaultObj.AddComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0.5f, 0.5f);
                vrt.anchorMax = new Vector2(0.5f, 0.5f);
                vrt.anchoredPosition = new Vector2(0f, 168f);
                vrt.sizeDelta = new Vector2(560f, 46f);

                Image vImg = vaultObj.AddComponent<Image>();
                vImg.color = new Color(0.28f, 0.21f, 0.04f, 0.95f);

                GameObject txtObj = new GameObject("TokenText");
                txtObj.transform.SetParent(vaultObj.transform, false);
                RectTransform trt = txtObj.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.sizeDelta = Vector2.zero;
                trt.anchoredPosition = Vector2.zero;

                _shopTokenText = txtObj.AddComponent<TextMeshProUGUI>();
                _shopTokenText.fontSize = 20f;
                _shopTokenText.fontStyle = FontStyles.Bold;
                _shopTokenText.color = new Color(1f, 0.92f, 0.35f);
                _shopTokenText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                RectTransform vrt = tokVaultTrans.GetComponent<RectTransform>();
                if (vrt != null)
                {
                    vrt.anchorMin = new Vector2(0.5f, 0.5f);
                    vrt.anchorMax = new Vector2(0.5f, 0.5f);
                    vrt.anchoredPosition = new Vector2(0f, 168f);
                    vrt.sizeDelta = new Vector2(560f, 46f);
                }
                _shopTokenText = tokVaultTrans.GetComponentInChildren<TMP_Text>();
            }

            // 3. Nút Back về Menu
            Transform backTrans = shopPanel.transform.Find("ShopBackButton") ?? shopPanel.transform.Find("BackButton");
            if (backTrans != null)
            {
                _closeShopButton = backTrans.GetComponent<Button>();
                RectTransform rt = backTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, -244f);
                    rt.sizeDelta = new Vector2(280f, 44f);
                }
                Image bImg = backTrans.GetComponent<Image>();
                if (bImg != null) bImg.color = new Color(0.82f, 0.22f, 0.22f, 1f);

                TMP_Text txt = backTrans.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = "◀  QUAY LAI MENU";
                    txt.fontSize = 19f;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = Color.white;
                }
            }
            if (_closeShopButton != null)
            {
                _closeShopButton.onClick.RemoveAllListeners();
                _closeShopButton.onClick.AddListener(OnClickCloseShop);
            }

            // Ẩn nút phân trang nếu bị duplicate vào shop
            Transform pBtn = shopPanel.transform.Find("PrevPageButton");
            if (pBtn != null) pBtn.gameObject.SetActive(false);
            Transform nBtn = shopPanel.transform.Find("NextPageButton");
            if (nBtn != null) nBtn.gameObject.SetActive(false);

            // 4. Tab Chuyển Mục: [ ⚡ NÂNG CẤP & BUFF ]  |  [ 👑 SKIN MÓC VIP ]
            Button tabTemplate = _closeShopButton ?? _playButton;
            _buffTabButton = SetupOrCloneButton(shopPanel, "ShopBuffTabBtn", tabTemplate, new Vector2(-155f, 122f), new Vector2(280f, 38f),
                new Color(0.18f, 0.45f, 0.78f, 1f), "⚡  NANG CAP & BUFF", 16f, () => OnClickShopTab(0));
            _buffTabText = _buffTabButton != null ? _buffTabButton.GetComponentInChildren<TMP_Text>() : null;

            _skinTabButton = SetupOrCloneButton(shopPanel, "ShopSkinTabBtn", tabTemplate, new Vector2(155f, 122f), new Vector2(280f, 38f),
                new Color(0.15f, 0.18f, 0.25f, 1f), "👑  SKIN MOC VIP (X2 DIEM)", 16f, () => OnClickShopTab(1));
            _skinTabText = _skinTabButton != null ? _skinTabButton.GetComponentInChildren<TMP_Text>() : null;

            // 5. Xây dựng 6 mục Buff trong Cửa Hàng (2 cột x 3 dòng, Kích thước 420x96)
            Button rowTemplate = _closeShopButton ?? _playButton;
            float[] colX = { -220f, 220f };
            float[] rowY = { 50f, -56f, -162f };
            _buffCardObjs.Clear();

            // 1. Tời Trợ Lực (Pull Power)
            GameObject c1 = CreateBuffCard(rowTemplate, "PullPower", colX[0], rowY[0], new Color(0.18f, 0.18f, 0.26f, 0.98f), out _buyPullPowerBtn, out _pullPowerTxt,
                () => { ShopManager.Instance?.BuyPullPower(); UpdateShopUI(); });
            if (c1 != null) _buffCardObjs.Add(c1);

            // 2. Cáp Siêu Tốc (Cable Speed)
            GameObject c2 = CreateBuffCard(rowTemplate, "CableSpeed", colX[1], rowY[0], new Color(0.14f, 0.20f, 0.28f, 0.98f), out _buyCableSpeedBtn, out _cableSpeedTxt,
                () => { ShopManager.Instance?.BuyCableSpeed(); UpdateShopUI(); });
            if (c2 != null) _buffCardObjs.Add(c2);

            // 3. Móc May Mắn (Lucky Hook)
            GameObject c3 = CreateBuffCard(rowTemplate, "LuckyHook", colX[0], rowY[1], new Color(0.14f, 0.23f, 0.22f, 0.98f), out _buyLuckyHookBtn, out _luckyHookTxt,
                () => { ShopManager.Instance?.BuyLuckyHook(); UpdateShopUI(); });
            if (c3 != null) _buffCardObjs.Add(c3);

            // 4. Thuốc Nổ TNT (Dynamite)
            GameObject c4 = CreateBuffCard(rowTemplate, "Dynamite", colX[1], rowY[1], new Color(0.26f, 0.16f, 0.16f, 0.98f), out _buyDynamiteBtn, out _dynamiteTxt,
                () => { ShopManager.Instance?.BuyDynamite(); UpdateShopUI(); });
            if (c4 != null) _buffCardObjs.Add(c4);

            // 5. Nước Tăng Lực (Energy Drink)
            GameObject c5 = CreateBuffCard(rowTemplate, "EnergyDrink", colX[0], rowY[2], new Color(0.14f, 0.24f, 0.20f, 0.98f), out _buyEnergyDrinkBtn, out _energyDrinkTxt,
                () => { ShopManager.Instance?.BuyEnergyDrink(); UpdateShopUI(); });
            if (c5 != null) _buffCardObjs.Add(c5);

            // 6. Khiên Gỡ Bom (Bomb Defuser Shield)
            GameObject c6 = CreateBuffCard(rowTemplate, "BombShield", colX[1], rowY[2], new Color(0.16f, 0.20f, 0.28f, 0.98f), out _buyBombShieldBtn, out _bombShieldTxt,
                () => { ShopManager.Instance?.BuyBombShield(); UpdateShopUI(); });
            if (c6 != null) _buffCardObjs.Add(c6);

            // 6. Xây dựng 4 thẻ Skin Móc VIP (2 cột x 2 dòng, Kích thước 420x130)
            _skinCardObjs.Clear();
            _skinCardBtns.Clear();
            _skinCardTexts.Clear();

            float[] sColX = { -220f, 220f };
            float[] sRowY = { 35f, -115f };
            var allSkins = ShopManager.AllSkins;

            for (int i = 0; i < allSkins.Length; i++)
            {
                var skinData = allSkins[i];
                string sId = skinData.id;
                int col = i % 2;
                int row = i / 2;
                Color sBg;
                switch (sId)
                {
                    case SaveSystem.SkinGoldenId: sBg = new Color(0.28f, 0.22f, 0.05f, 0.98f); break;
                    case SaveSystem.SkinThunderId: sBg = new Color(0.06f, 0.22f, 0.28f, 0.98f); break;
                    case SaveSystem.SkinDragonId: sBg = new Color(0.28f, 0.08f, 0.10f, 0.98f); break;
                    default: sBg = new Color(0.14f, 0.16f, 0.22f, 0.98f); break;
                }

                GameObject sCard = CreateSkinCard(rowTemplate, skinData, sColX[col], sRowY[row], sBg, out Button sBtn, out TMP_Text sTxt,
                    () => OnClickSkinCard(sId));
                if (sCard != null)
                {
                    _skinCardObjs.Add(sCard);
                    _skinCardBtns.Add(sBtn);
                    _skinCardTexts.Add(sTxt);
                }
            }

            UpdateShopTabDisplay();
        }

        private GameObject CreateBuffCard(Button template, string id, float posX, float posY, Color accentBg, out Button buyBtn, out TMP_Text infoTxt, UnityEngine.Events.UnityAction onBuy)
        {
            buyBtn = null;
            infoTxt = null;
            if (shopPanel == null) return null;

            Transform existing = shopPanel.transform.Find($"BuffCard_{id}");
            GameObject cardObj = existing != null ? existing.gameObject : (template != null ? Instantiate(template.gameObject, shopPanel.transform) : null);
            if (cardObj == null) return null;

            cardObj.name = $"BuffCard_{id}";
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.sizeDelta = new Vector2(420f, 96f);
            }
            Image img = cardObj.GetComponent<Image>();
            if (img != null) img.color = accentBg;

            buyBtn = cardObj.GetComponent<Button>();
            if (buyBtn != null)
            {
                var colors = buyBtn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
                buyBtn.colors = colors;

                buyBtn.onClick.RemoveAllListeners();
                if (onBuy != null) buyBtn.onClick.AddListener(onBuy);
            }

            infoTxt = cardObj.GetComponentInChildren<TMP_Text>();
            if (infoTxt != null)
            {
                RectTransform trt = infoTxt.GetComponent<RectTransform>();
                if (trt != null)
                {
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = new Vector2(14f, 8f);
                    trt.offsetMax = new Vector2(-14f, -8f);
                    trt.anchoredPosition = Vector2.zero;
                }
                infoTxt.fontSize = 13f;
                infoTxt.alignment = TextAlignmentOptions.Center;
                infoTxt.textWrappingMode = TextWrappingModes.Normal;
                infoTxt.color = Color.white;
            }

            return cardObj;
        }

        private GameObject CreateSkinCard(Button template, HookSkinData skin, float posX, float posY, Color accentBg, out Button cardBtn, out TMP_Text infoTxt, UnityEngine.Events.UnityAction onClick)
        {
            cardBtn = null;
            infoTxt = null;
            if (shopPanel == null || skin == null) return null;

            Transform existing = shopPanel.transform.Find($"SkinCard_{skin.id}");
            GameObject cardObj = existing != null ? existing.gameObject : (template != null ? Instantiate(template.gameObject, shopPanel.transform) : null);
            if (cardObj == null) return null;

            cardObj.name = $"SkinCard_{skin.id}";
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.sizeDelta = new Vector2(420f, 130f);
            }
            Image img = cardObj.GetComponent<Image>();
            if (img != null) img.color = accentBg;

            cardBtn = cardObj.GetComponent<Button>();
            if (cardBtn != null)
            {
                var colors = cardBtn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.8f);
                cardBtn.colors = colors;

                cardBtn.onClick.RemoveAllListeners();
                if (onClick != null) cardBtn.onClick.AddListener(onClick);
            }

            infoTxt = cardObj.GetComponentInChildren<TMP_Text>();
            if (infoTxt != null)
            {
                RectTransform trt = infoTxt.GetComponent<RectTransform>();
                if (trt != null)
                {
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = new Vector2(14f, 8f);
                    trt.offsetMax = new Vector2(-14f, -8f);
                    trt.anchoredPosition = Vector2.zero;
                }
                infoTxt.fontSize = 12.5f;
                infoTxt.alignment = TextAlignmentOptions.Center;
                infoTxt.textWrappingMode = TextWrappingModes.Normal;
                infoTxt.color = Color.white;
            }

            return cardObj;
        }

        public void OnClickShopTab(int tabIndex)
        {
            _currentShopTab = tabIndex;
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            UpdateShopTabDisplay();
            UpdateShopUI();
        }

        private void UpdateShopTabDisplay()
        {
            bool showBuffs = _currentShopTab == 0;
            bool showSkins = _currentShopTab == 1;

            for (int i = 0; i < _buffCardObjs.Count; i++)
            {
                if (_buffCardObjs[i] != null) _buffCardObjs[i].SetActive(showBuffs);
            }

            for (int i = 0; i < _skinCardObjs.Count; i++)
            {
                if (_skinCardObjs[i] != null) _skinCardObjs[i].SetActive(showSkins);
            }

            if (_buffTabButton != null)
            {
                Image img = _buffTabButton.GetComponent<Image>();
                if (img != null) img.color = showBuffs ? new Color(0.18f, 0.45f, 0.78f, 1f) : new Color(0.15f, 0.18f, 0.25f, 1f);
            }
            if (_buffTabText != null)
            {
                _buffTabText.color = showBuffs ? Color.white : new Color(0.7f, 0.75f, 0.85f, 1f);
            }

            if (_skinTabButton != null)
            {
                Image img = _skinTabButton.GetComponent<Image>();
                if (img != null) img.color = showSkins ? new Color(0.85f, 0.55f, 0.08f, 1f) : new Color(0.15f, 0.18f, 0.25f, 1f);
            }
            if (_skinTabText != null)
            {
                _skinTabText.color = showSkins ? Color.white : new Color(0.7f, 0.75f, 0.85f, 1f);
            }
        }

        public void OnClickSkinCard(string skinId)
        {
            if (ShopManager.Instance == null) return;

            string equipped = SaveSystem.GetEquippedSkin();
            bool isOwned = SaveSystem.IsSkinOwned(skinId);

            if (!isOwned)
            {
                bool bought = ShopManager.Instance.BuySkin(skinId);
                if (bought)
                {
                    DaoVang.Audio.AudioManager.Instance?.PlayWin();
                }
                else
                {
                    DaoVang.Audio.AudioManager.Instance?.PlayBomb();
                }
            }
            else if (equipped != skinId)
            {
                ShopManager.Instance.EquipSkin(skinId);
                DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            }
            UpdateShopUI();
        }

        // =========================================================================
        // 4. HUD KHI CHƠI (PLAYING HUD)
        // =========================================================================
        private void BuildOrSetupPlayingHUD()
        {
            if (playingPanel == null)
            {
                Transform pp = transform.Find("PlayingPanel");
                if (pp != null) playingPanel = pp.gameObject;
            }
            if (playingPanel == null) return;

            // Score Text
            Transform sTrans = playingPanel.transform.Find("Score");
            if (sTrans != null)
            {
                _scoreText = sTrans.GetComponent<TMP_Text>();
                RectTransform rt = sTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(25f, -20f);
                    rt.sizeDelta = new Vector2(280f, 45f);
                }
                if (_scoreText != null)
                {
                    _scoreText.fontSize = 24f;
                    _scoreText.fontStyle = FontStyles.Bold;
                    _scoreText.color = new Color(1f, 0.88f, 0.25f);
                }
            }

            // Target Text
            Transform tgTrans = playingPanel.transform.Find("Target");
            if (tgTrans != null)
            {
                _targetText = tgTrans.GetComponent<TMP_Text>();
                RectTransform rt = tgTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -20f);
                    rt.sizeDelta = new Vector2(300f, 45f);
                }
                if (_targetText != null)
                {
                    _targetText.fontSize = 22f;
                    _targetText.fontStyle = FontStyles.Bold;
                    _targetText.alignment = TextAlignmentOptions.Center;
                    _targetText.color = Color.white;
                }
            }

            // Time Text
            Transform tmTrans = playingPanel.transform.Find("Time");
            if (tmTrans != null)
            {
                _timeText = tmTrans.GetComponent<TMP_Text>();
                RectTransform rt = tmTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-25f, -20f);
                    rt.sizeDelta = new Vector2(220f, 45f);
                }
                if (_timeText != null)
                {
                    _timeText.fontSize = 24f;
                    _timeText.fontStyle = FontStyles.Bold;
                    _timeText.alignment = TextAlignmentOptions.Right;
                    _timeText.color = Color.white;
                }
            }

            // Token Counter trên HUD (Góc dưới bên trái)
            Transform hTokTrans = playingPanel.transform.Find("HudTokenText");
            if (hTokTrans == null && _scoreText != null)
            {
                GameObject tokObj = Instantiate(_scoreText.gameObject, playingPanel.transform);
                tokObj.name = "HudTokenText";
                hTokTrans = tokObj.transform;
            }
            if (hTokTrans != null)
            {
                _hudTokenText = hTokTrans.GetComponent<TMP_Text>();
                RectTransform rt = hTokTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(25f, 25f);
                    rt.sizeDelta = new Vector2(260f, 40f);
                }
                if (_hudTokenText != null)
                {
                    _hudTokenText.fontSize = 20f;
                    _hudTokenText.fontStyle = FontStyles.Bold;
                    _hudTokenText.color = new Color(1f, 0.92f, 0.45f);
                }
            }

            // Buff Status Text trên HUD (Hiển thị +15s hoặc Khiên Bom nếu có)
            Transform buffTrans = playingPanel.transform.Find("HudBuffStatusText");
            if (buffTrans == null && _targetText != null)
            {
                GameObject bObj = Instantiate(_targetText.gameObject, playingPanel.transform);
                bObj.name = "HudBuffStatusText";
                buffTrans = bObj.transform;
            }
            if (buffTrans != null)
            {
                _hudBuffStatusText = buffTrans.GetComponent<TMP_Text>();
                RectTransform rt = buffTrans.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -65f);
                    rt.sizeDelta = new Vector2(400f, 32f);
                }
                if (_hudBuffStatusText != null)
                {
                    _hudBuffStatusText.fontSize = 17f;
                    _hudBuffStatusText.fontStyle = FontStyles.Bold;
                    _hudBuffStatusText.color = new Color(0.35f, 0.95f, 0.5f);
                    _hudBuffStatusText.alignment = TextAlignmentOptions.Center;
                }
            }

            // Nút Thuốc Nổ TNT trên HUD (Góc dưới bên phải)
            Transform dTrans = playingPanel.transform.Find("DynamiteButton");
            if (dTrans != null)
            {
                _dynamiteHudButton = dTrans.GetComponent<Button>();
                _dynamiteHudText = dTrans.GetComponentInChildren<TMP_Text>();
            }
            else
            {
                GameObject dObj = new GameObject("DynamiteButton");
                dObj.transform.SetParent(playingPanel.transform, false);
                RectTransform rt = dObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-25f, 25f);
                rt.sizeDelta = new Vector2(170f, 50f);

                Image img = dObj.AddComponent<Image>();
                img.color = new Color(0.85f, 0.22f, 0.22f, 0.95f);

                _dynamiteHudButton = dObj.AddComponent<Button>();

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(dObj.transform, false);
                RectTransform trt = txtObj.AddComponent<RectTransform>();
                trt.sizeDelta = rt.sizeDelta;
                _dynamiteHudText = txtObj.AddComponent<TextMeshProUGUI>();
                _dynamiteHudText.fontSize = 17f;
                _dynamiteHudText.fontStyle = FontStyles.Bold;
                _dynamiteHudText.alignment = TextAlignmentOptions.Center;
                _dynamiteHudText.color = Color.white;
            }

            if (_dynamiteHudButton != null)
            {
                _dynamiteHudButton.onClick.RemoveAllListeners();
                _dynamiteHudButton.onClick.AddListener(OnClickUseDynamite);
            }

            // 2 Nút HUD Tiện ích khi chơi: "THOÁT" và "QUA MÀN"
            Button hudTemplate = _dynamiteHudButton ?? _playButton;
            Button hudExitBtn = SetupOrCloneButton(playingPanel, "HudExitButton", hudTemplate,
                new Vector2(80f, -72f), new Vector2(110f, 36f),
                new Color(0.65f, 0.22f, 0.22f, 0.95f), "◀  THOAT", 15f, OnClickBackToMenuFromPlaying);
            if (hudExitBtn != null)
            {
                RectTransform rt = hudExitBtn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(80f, -72f);
                }
            }

            Button hudSkipBtn = SetupOrCloneButton(playingPanel, "HudSkipButton", hudTemplate,
                new Vector2(205f, -72f), new Vector2(115f, 36f),
                new Color(0.18f, 0.65f, 0.3f, 0.95f), "▶  QUA MAN", 15f, OnClickNextLevel);
            if (hudSkipBtn != null)
            {
                RectTransform rt = hudSkipBtn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(205f, -72f);
                }
            }
        }

        // =========================================================================
        // 5. GAME OVER & LEVEL COMPLETE
        // =========================================================================
        private void BuildOrSetupGameOverAndComplete()
        {
            // Game Over Panel
            if (gameOverPanel != null)
            {
                SetupDialogPanel(gameOverPanel);

                Transform tTrans = gameOverPanel.transform.Find("TitleText") ?? gameOverPanel.transform.Find("Title");
                if (tTrans != null)
                {
                    _gameOverTitleText = tTrans.GetComponent<TMP_Text>();
                    if (_gameOverTitleText != null)
                    {
                        _gameOverTitleText.text = "THUA CUOC (GAME OVER)";
                        _gameOverTitleText.fontSize = 32f;
                        _gameOverTitleText.fontStyle = FontStyles.Bold;
                        _gameOverTitleText.color = new Color(0.95f, 0.25f, 0.25f);
                        _gameOverTitleText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform rTrans = gameOverPanel.transform.Find("ReasonText");
                if (rTrans != null)
                {
                    _gameOverReasonText = rTrans.GetComponent<TMP_Text>();
                    if (_gameOverReasonText != null)
                    {
                        _gameOverReasonText.fontSize = 21f;
                        _gameOverReasonText.fontStyle = FontStyles.Bold;
                        _gameOverReasonText.color = new Color(1f, 0.65f, 0.25f);
                        _gameOverReasonText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform sTrans = gameOverPanel.transform.Find("GameOverScoreText") ?? gameOverPanel.transform.Find("ScoreText");
                if (sTrans != null)
                {
                    _gameOverScoreText = sTrans.GetComponent<TMP_Text>();
                    if (_gameOverScoreText != null)
                    {
                        _gameOverScoreText.fontSize = 19f;
                        _gameOverScoreText.fontStyle = FontStyles.Bold;
                        _gameOverScoreText.color = Color.white;
                        _gameOverScoreText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform rBtn = gameOverPanel.transform.Find("RestartButton") ?? gameOverPanel.transform.Find("RetryButton");
                if (rBtn != null) _gameOverRetryButton = rBtn.GetComponent<Button>();

                _gameOverRetryButton = SetupOrCloneButton(gameOverPanel, "RestartButton", _gameOverRetryButton ?? _playButton,
                    new Vector2(-155f, -65f), new Vector2(230f, 52f),
                    new Color(0.18f, 0.72f, 0.32f, 1f), "🔄  CHOI LAI", 19f, OnClickRestart);

                _gameOverShopButton = SetupOrCloneButton(gameOverPanel, "ShopButton", _gameOverRetryButton ?? _playButton,
                    new Vector2(155f, -65f), new Vector2(230f, 52f),
                    new Color(0.88f, 0.55f, 0.1f, 1f), "🛒  CUA HANG", 19f, OnClickOpenShop);

                _gameOverMenuButton = SetupOrCloneButton(gameOverPanel, "MenuButton", _gameOverRetryButton ?? _playButton,
                    new Vector2(0f, -140f), new Vector2(260f, 44f),
                    new Color(0.28f, 0.33f, 0.42f, 1f), "◀  THOAT / VE MENU", 17f, OnClickBackToMenuFromGameOver);
            }

            // Level Complete Panel
            if (levelCompletePanel != null)
            {
                SetupDialogPanel(levelCompletePanel);

                Transform tTrans = levelCompletePanel.transform.Find("TitleText") ?? levelCompletePanel.transform.Find("Title");
                if (tTrans != null)
                {
                    _completeTitleText = tTrans.GetComponent<TMP_Text>();
                    if (_completeTitleText != null)
                    {
                        _completeTitleText.text = "CHIEN THANG MAN CHOI!";
                        _completeTitleText.fontSize = 32f;
                        _completeTitleText.fontStyle = FontStyles.Bold;
                        _completeTitleText.color = new Color(1f, 0.85f, 0.2f);
                        _completeTitleText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform sTrans = levelCompletePanel.transform.Find("CompleteScoreText") ?? levelCompletePanel.transform.Find("ScoreText");
                if (sTrans != null)
                {
                    _levelCompleteScoreText = sTrans.GetComponent<TMP_Text>();
                    if (_levelCompleteScoreText != null)
                    {
                        _levelCompleteScoreText.fontSize = 22f;
                        _levelCompleteScoreText.fontStyle = FontStyles.Bold;
                        _levelCompleteScoreText.color = new Color(1f, 0.9f, 0.35f);
                        _levelCompleteScoreText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform rwTrans = levelCompletePanel.transform.Find("RewardText");
                if (rwTrans != null)
                {
                    _rewardText = rwTrans.GetComponent<TMP_Text>();
                    if (_rewardText != null)
                    {
                        _rewardText.fontSize = 19f;
                        _rewardText.fontStyle = FontStyles.Bold;
                        _rewardText.color = new Color(0.35f, 0.95f, 0.6f);
                        _rewardText.alignment = TextAlignmentOptions.Center;
                    }
                }

                Transform nBtn = levelCompletePanel.transform.Find("NextLevelButton");
                if (nBtn != null)
                {
                    _nextLevelButton = nBtn.gameObject;
                }

                Button templateBtn = (_nextLevelButton != null ? _nextLevelButton.GetComponent<Button>() : null) ?? _gameOverRetryButton ?? _playButton;

                Button nextBtnComp = SetupOrCloneButton(levelCompletePanel, "NextLevelButton", templateBtn,
                    new Vector2(-155f, -65f), new Vector2(230f, 52f),
                    new Color(0.18f, 0.72f, 0.32f, 1f), "▶  QUA MAN TIEP", 19f, OnClickNextLevel);
                if (nextBtnComp != null)
                {
                    _nextLevelButton = nextBtnComp.gameObject;
                }

                _completeShopButton = SetupOrCloneButton(levelCompletePanel, "ShopButton", templateBtn,
                    new Vector2(155f, -65f), new Vector2(230f, 52f),
                    new Color(0.88f, 0.55f, 0.1f, 1f), "🛒  CUA HANG", 19f, OnClickOpenShop);

                _completeMenuButton = SetupOrCloneButton(levelCompletePanel, "MenuButton", templateBtn,
                    new Vector2(0f, -140f), new Vector2(260f, 44f),
                    new Color(0.28f, 0.33f, 0.42f, 1f), "◀  THOAT / VE MENU", 17f, OnClickBackToMenuFromComplete);
            }
        }

        private void SetupDialogPanel(GameObject panel)
        {
            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(680f, 440f);
            }
            Image img = panel.GetComponent<Image>();
            if (img == null) img = panel.AddComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.09f, 0.08f, 0.11f, 0.96f);
            }
        }

        // =========================================================================
        // 6. MODAL HƯỚNG DẪN CHI TIẾT (TUTORIAL MODAL)
        // =========================================================================
        private void BuildTutorialModal()
        {
            if (_tutorialModal != null) return;

            Transform existing = transform.Find("TutorialModal");
            if (existing != null)
            {
                _tutorialModal = existing.gameObject;
                return;
            }

            _tutorialModal = new GameObject("TutorialModal");
            _tutorialModal.transform.SetParent(transform, false);

            RectTransform rt = _tutorialModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = _tutorialModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);

            GameObject box = new GameObject("Box");
            box.transform.SetParent(_tutorialModal.transform, false);
            RectTransform boxRt = box.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(700f, 480f);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.14f, 0.19f, 1f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(box.transform, false);
            RectTransform trt = titleObj.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 195f);
            trt.sizeDelta = new Vector2(650f, 45f);
            TMP_Text tTxt = titleObj.AddComponent<TextMeshProUGUI>();
            tTxt.text = "HUONG DAN CHOI DAO VANG";
            tTxt.fontSize = 26f;
            tTxt.fontStyle = FontStyles.Bold;
            tTxt.color = new Color(1f, 0.85f, 0.2f);
            tTxt.alignment = TextAlignmentOptions.Center;

            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(box.transform, false);
            RectTransform drt = descObj.AddComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, 20f);
            drt.sizeDelta = new Vector2(620f, 280f);
            TMP_Text dTxt = descObj.AddComponent<TextMeshProUGUI>();
            dTxt.fontSize = 16f;
            dTxt.lineSpacing = 20f;
            dTxt.color = new Color(0.92f, 0.94f, 0.98f);
            dTxt.text = "1. MUC TIEU:\n" +
                        "  Tha moc cau nham chuan Vang va Kim Cuong de dat du Diem Muc Tieu truoc khi het gio!\n\n" +
                        "2. DIEU KHIEN:\n" +
                        "  Cham man hinh (mobile) hoac Click chuot trai (PC) de phong moc cau.\n" +
                        "  Nhan phim SPACE hoac nut TNT de kich no pha da/rac khi keo nham.\n\n" +
                        "3. CAM BAY BOM:\n" +
                        "  Tuyet doi tranh xa Bom no! Keo trung bom se phat no va THUA NGAY (tru khi da trang bi Khien Go Bom).\n\n" +
                        "4. HE THONG TOKEN & CUA HANG:\n" +
                        "  Tich luy Token tu diem so de mua Toi tro luc, Cap sieu toc, Nuoc tang luc (+15s) va Khien go bom!";

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(box.transform, false);
            RectTransform crt = closeBtnObj.AddComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0f, -195f);
            crt.sizeDelta = new Vector2(220f, 48f);
            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.12f, 0.65f, 0.28f, 1f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(OnClickCloseTutorial);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform ctrt = cTxtObj.AddComponent<RectTransform>();
            ctrt.sizeDelta = crt.sizeDelta;
            TMP_Text cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "DA HIEU";
            cTxt.fontSize = 19f;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.color = Color.white;
            cTxt.alignment = TextAlignmentOptions.Center;

            _tutorialModal.SetActive(false);
        }

        // =========================================================================
        // 7. MODAL DANG NHAP & PHAN QUYEN ADMIN (LOGIN MODAL)
        // =========================================================================
        private void BuildLoginModal()
        {
            if (_loginModal != null) return;

            Transform existing = transform.Find("LoginModal");
            if (existing != null)
            {
                _loginModal = existing.gameObject;
                return;
            }

            _loginModal = new GameObject("LoginModal");
            _loginModal.transform.SetParent(transform, false);

            RectTransform rt = _loginModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = _loginModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.96f);

            GameObject box = new GameObject("Box");
            box.transform.SetParent(_loginModal.transform, false);
            RectTransform boxRt = box.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(720f, 520f);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.11f, 0.13f, 0.18f, 1f);

            // 1. Tiêu đề Modal
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(box.transform, false);
            RectTransform trt = titleObj.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 215f);
            trt.sizeDelta = new Vector2(680f, 44f);
            TMP_Text tTxt = titleObj.AddComponent<TextMeshProUGUI>();
            tTxt.text = "🔑  DANG NHAP & PHAN QUYEN TAI KHOAN";
            tTxt.fontSize = 24f;
            tTxt.fontStyle = FontStyles.Bold;
            tTxt.color = new Color(1f, 0.85f, 0.2f);
            tTxt.alignment = TextAlignmentOptions.Center;

            // 2. Dòng hiển thị trạng thái tài khoản hiện tại
            GameObject curAccObj = new GameObject("CurrentAccount");
            curAccObj.transform.SetParent(box.transform, false);
            RectTransform cart = curAccObj.AddComponent<RectTransform>();
            cart.anchoredPosition = new Vector2(0f, 172f);
            cart.sizeDelta = new Vector2(660f, 32f);
            _loginCurrentAccountText = curAccObj.AddComponent<TextMeshProUGUI>();
            _loginCurrentAccountText.fontSize = 16f;
            _loginCurrentAccountText.fontStyle = FontStyles.Bold;
            _loginCurrentAccountText.alignment = TextAlignmentOptions.Center;

            // 3. NÚT ĐẶC QUYỀN ADMIN (1 CHẠM - TO NỔI BẬT NHẤT)
            Button templateBtn = _playButton ?? _closeShopButton;
            SetupOrCloneButton(box, "QuickAdminBtn", templateBtn, new Vector2(0f, 110f), new Vector2(580f, 54f),
                new Color(0.85f, 0.55f, 0.08f, 1f), "👑  DANG NHAP ADMIN (FULL 1 CHAM - MO KHOA TOAN BO)", 16.5f, OnClickQuickAdminLogin);

            // 4. Ô NHẬP LIỆU USERNAME & PASSWORD (DÀNH CHO NGƯỜI MUỐN GÕ TAY)
            // Label Username
            GameObject uLbl = new GameObject("UserLabel");
            uLbl.transform.SetParent(box.transform, false);
            RectTransform ulrt = uLbl.AddComponent<RectTransform>();
            ulrt.anchoredPosition = new Vector2(-170f, 48f);
            ulrt.sizeDelta = new Vector2(230f, 26f);
            TMP_Text utxt = uLbl.AddComponent<TextMeshProUGUI>();
            utxt.text = "Ten dang nhap (admin / ten ban):";
            utxt.fontSize = 13.5f;
            utxt.color = new Color(0.8f, 0.85f, 0.95f);

            // Input Username
            _usernameInput = CreateInputField(box, "UsernameInput", new Vector2(-170f, 12f), new Vector2(240f, 40f), "Nhap ten dang nhap...", "admin");

            // Label Password
            GameObject pLbl = new GameObject("PassLabel");
            pLbl.transform.SetParent(box.transform, false);
            RectTransform plrt = pLbl.AddComponent<RectTransform>();
            plrt.anchoredPosition = new Vector2(170f, 48f);
            plrt.sizeDelta = new Vector2(230f, 26f);
            TMP_Text ptxt = pLbl.AddComponent<TextMeshProUGUI>();
            ptxt.text = "Mat khau (mac dinh admin):";
            ptxt.fontSize = 13.5f;
            ptxt.color = new Color(0.8f, 0.85f, 0.95f);

            // Input Password
            _passwordInput = CreateInputField(box, "PasswordInput", new Vector2(170f, 12f), new Vector2(240f, 40f), "Nhap mat khau...", "admin");
            if (_passwordInput != null) _passwordInput.contentType = TMP_InputField.ContentType.Password;

            // Nút Xác Nhận Đăng Nhập Form
            SetupOrCloneButton(box, "SubmitFormBtn", templateBtn, new Vector2(0f, -42f), new Vector2(360f, 44f),
                new Color(0.18f, 0.45f, 0.78f, 1f), "🚀  XAC NHAN DANG NHAP FORM", 16.5f, OnClickSubmitLogin);

            // 5. Text phản hồi thông báo sau khi đăng nhập / lỗi
            GameObject feedObj = new GameObject("FeedbackText");
            feedObj.transform.SetParent(box.transform, false);
            RectTransform frt = feedObj.AddComponent<RectTransform>();
            frt.anchoredPosition = new Vector2(0f, -108f);
            frt.sizeDelta = new Vector2(660f, 65f);
            _loginFeedbackText = feedObj.AddComponent<TextMeshProUGUI>();
            _loginFeedbackText.fontSize = 13.5f;
            _loginFeedbackText.fontStyle = FontStyles.Bold;
            _loginFeedbackText.alignment = TextAlignmentOptions.Center;
            _loginFeedbackText.text = "";

            // 6. Nút Đăng Xuất & Nút Đóng
            SetupOrCloneButton(box, "LogoutBtn", templateBtn, new Vector2(-150f, -195f), new Vector2(240f, 46f),
                new Color(0.68f, 0.18f, 0.22f, 1f), "🚪  DANG XUAT TAI KHOAN", 16f, OnClickLogout);

            SetupOrCloneButton(box, "CloseBtn", templateBtn, new Vector2(150f, -195f), new Vector2(240f, 46f),
                new Color(0.28f, 0.33f, 0.42f, 1f), "✖  DONG CUA SO", 16f, OnClickCloseLogin);

            _loginModal.SetActive(false);
        }

        private TMP_InputField CreateInputField(GameObject parent, string name, Vector2 pos, Vector2 size, string placeholderText, string defaultText = "")
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent.transform, false);

            RectTransform rt = inputObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.22f, 0.28f, 1f);

            // Text Component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 2f);
            trt.offsetMax = new Vector2(-10f, -2f);
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 15f;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Left;

            // Placeholder Component
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phrt = phObj.AddComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero;
            phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(10f, 2f);
            phrt.offsetMax = new Vector2(-10f, -2f);
            TextMeshProUGUI phComp = phObj.AddComponent<TextMeshProUGUI>();
            phComp.fontSize = 15f;
            phComp.fontStyle = FontStyles.Italic;
            phComp.color = new Color(0.6f, 0.65f, 0.75f, 0.8f);
            phComp.alignment = TextAlignmentOptions.Left;
            phComp.text = placeholderText;

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = rt;
            inputField.textComponent = textComp;
            inputField.placeholder = phComp;
            if (!string.IsNullOrEmpty(defaultText)) inputField.text = defaultText;

            return inputField;
        }

        private void UpdateMainMenuAccountDisplay()
        {
            bool isAdmin = SaveSystem.IsAdminLoggedIn();
            string username = SaveSystem.GetCurrentUsername();

            if (_loginButton != null)
            {
                Image img = _loginButton.GetComponent<Image>();
                if (_loginButtonText != null)
                {
                    if (isAdmin)
                    {
                        _loginButtonText.text = "👑  TAI KHOAN: <color=#FFE600><b>ADMIN (FULL UNLOCK)</b></color>  [DOI]";
                        if (img != null) img.color = new Color(0.35f, 0.25f, 0.05f, 0.98f);
                    }
                    else
                    {
                        _loginButtonText.text = $"👤  TAI KHOAN: <color=#50FF80><b>{username}</b></color>  [DANG NHAP / DOI]";
                        if (img != null) img.color = new Color(0.18f, 0.25f, 0.35f, 0.98f);
                    }
                }
            }

            if (_loginCurrentAccountText != null)
            {
                if (isAdmin)
                {
                    _loginCurrentAccountText.text = "👑 DANG DANG NHAP: <color=#FFE600><b>ADMIN (DAC QUYEN TOI THUONG - FULL 8 MAN)</b></color>";
                }
                else
                {
                    _loginCurrentAccountText.text = $"👤 DANG DANG NHAP: <color=#50FF80><b>{username} (TAI KHOAN NGUOI CHOI)</b></color>";
                }
            }
        }

        public void OnClickOpenLogin()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            if (_loginModal != null)
            {
                _loginModal.SetActive(true);
                UpdateMainMenuAccountDisplay();
                if (_loginFeedbackText != null) _loginFeedbackText.text = "";
            }
        }

        public void OnClickCloseLogin()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            if (_loginModal != null) _loginModal.SetActive(false);
        }

        public void OnClickQuickAdminLogin()
        {
            if (_usernameInput != null) _usernameInput.text = "admin";
            if (_passwordInput != null) _passwordInput.text = "admin";
            ExecuteAdminLogin();
        }

        public void OnClickSubmitLogin()
        {
            string user = _usernameInput != null ? _usernameInput.text.Trim() : "";
            string pass = _passwordInput != null ? _passwordInput.text.Trim() : "";

            if (user.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                ExecuteAdminLogin();
            }
            else if (!string.IsNullOrEmpty(user))
            {
                ExecutePlayerLogin(user);
            }
            else
            {
                ShowLoginFeedback("<color=#FF7070>Vui long nhap ten dang nhap!</color>");
            }
        }

        private void ExecuteAdminLogin()
        {
            SaveSystem.LoginAsAdmin();
            GameManager.Instance?.RefreshProgressAfterLogin();
            ShopManager.Instance?.ApplyEquippedSkinToHook();
            DaoVang.Audio.AudioManager.Instance?.PlayWin();

            ShowLoginFeedback("<color=#50FF80><b>✔ DANG NHAP ADMIN THANH CONG!</b></color>\n" +
                              "<color=#FFE600>★ Da mo khoa FULL 8 Man choi (Tu do chon man)\n" +
                              "★ Da mo khoa FULL 3 Skin Moc VIP (Tu dong trang bi Moc Hoa Long x2 Diem)\n" +
                              "★ Nang cap toi da moi trang bi & Tang 999,999 Token!</color>");

            UpdateMainMenuAccountDisplay();
            RefreshLevelSelectCards();
            UpdateShopUI();
            ShowMenu();
        }

        private void ExecutePlayerLogin(string user)
        {
            SaveSystem.LoginAsPlayer(user);
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            ShowLoginFeedback($"<color=#50FF80>✔ Dang nhap thanh cong voi tai khoan <b>{user}</b>!</color>");
            UpdateMainMenuAccountDisplay();
            RefreshLevelSelectCards();
            UpdateShopUI();
            ShowMenu();
        }

        public void OnClickLogout()
        {
            SaveSystem.Logout();
            GameManager.Instance?.RefreshProgressAfterLogin();
            ShopManager.Instance?.ApplyEquippedSkinToHook();
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            ShowLoginFeedback("<color=#FFE600>Da dang xuat ve tai khoan Khach.</color>");
            UpdateMainMenuAccountDisplay();
            RefreshLevelSelectCards();
            UpdateShopUI();
            ShowMenu();
        }

        private void ShowLoginFeedback(string message)
        {
            if (_loginFeedbackText != null)
            {
                _loginFeedbackText.text = message;
            }
        }

        // =========================================================================
        // FLOW STATE HANDLER
        // =========================================================================
        private void HandleStateChanged(GameState state)
        {
            if (menuPanel != null) menuPanel.SetActive(state == GameState.Menu);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(state == GameState.LevelSelect);
            if (playingPanel != null) playingPanel.SetActive(state == GameState.Playing);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(state == GameState.LevelComplete);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (shopPanel != null) shopPanel.SetActive(state == GameState.Shop);

            if (_tutorialModal != null) _tutorialModal.SetActive(false);
            if (_loginModal != null) _loginModal.SetActive(false);
            DismissLockedToast();

            switch (state)
            {
                case GameState.Menu: ShowMenu(); break;
                case GameState.LevelSelect: ShowLevelSelect(); break;
                case GameState.Playing: ShowPlayingHUD(); break;
                case GameState.LevelComplete: ShowLevelComplete(); break;
                case GameState.GameOver: ShowGameOver(); break;
                case GameState.Shop: UpdateShopUI(); break;
            }
        }

        private void ShowMenu()
        {
            int tokens = SaveSystem.LoadTokens();
            int highScore = SaveSystem.LoadHighScore();
            int currentUnlocked = GameManager.Instance != null ? GameManager.Instance.UnlockedLevelIndex : SaveSystem.LoadUnlockedLevel();
            int wonCount = GameManager.Instance != null ? GameManager.Instance.TotalLevelsWon : SaveSystem.GetTotalLevelsWon(TotalLevels);

            UpdateMainMenuAccountDisplay();

            if (_menuTokenText != null)
            {
                _menuTokenText.text = $"★  SO DU TOKEN: <size=24><color=#FFE600><b>{tokens} TOKEN</b></color></size>   |   KY LUC: <color=#66FFBB><b>{highScore} DIEM</b></color>  ★";
            }
            if (_menuTitleText != null) _menuTitleText.text = "DAO VANG CLASSIC";

            if (_playButton != null)
            {
                TMP_Text btnTxt = _playButton.GetComponentInChildren<TMP_Text>();
                if (btnTxt != null)
                {
                    btnTxt.text = wonCount >= TotalLevels ? "▶  CHOI LAI CAC MAN" : $"▶  CHOI NGAY (AI {currentUnlocked + 1})";
                }
            }
        }

        private void ShowLevelSelect()
        {
            DismissLockedToast();
            // Tự động mở đúng trang chứa ải mới nhất đang mở khóa (Ải hiện tại)
            int currentUnlocked = GameManager.Instance != null ? GameManager.Instance.UnlockedLevelIndex : SaveSystem.LoadUnlockedLevel();
            int maxPages = Mathf.CeilToInt((float)TotalLevels / LevelsPerPage) - 1;
            _currentLevelPage = Mathf.Clamp(currentUnlocked / LevelsPerPage, 0, maxPages);
            RefreshLevelSelectCards();
        }

        private void ShowPlayingHUD()
        {
            _displayedScore = 0f;
            _targetScore = 0;
            _lastDisplayedScoreInt = -1;
            _lastFormattedSeconds = -1;
            _lastDynamiteCount = -1;

            if (_scoreText != null)
            {
                _scoreText.text = "DIEM: 0";
                _scoreText.transform.localScale = Vector3.one;
            }
            if (_targetText != null)
            {
                _targetText.text = $"MUC TIEU: {(levelManager != null ? levelManager.TargetScore : 0)}";
            }
            if (_timeText != null && levelManager != null)
            {
                int s = Mathf.CeilToInt(levelManager.TimeLeft);
                _lastFormattedSeconds = s;
                _timeText.text = $"{s / 60:00}:{s % 60:00}";
                _timeText.color = Color.white;
                _timeText.transform.localScale = Vector3.one;
            }

            if (_hudTokenText != null)
            {
                _hudTokenText.text = $"🪙 TOKEN: <color=#FFE600><b>{SaveSystem.LoadTokens()}</b></color>";
            }

            if (_hudBuffStatusText != null)
            {
                var skin = ShopManager.Instance != null ? ShopManager.Instance.GetEquippedSkinData() : null;
                string buffInfo = "";
                if (skin != null && skin.id != SaveSystem.DefaultSkinId)
                {
                    buffInfo += $"👑 {skin.name} (X2)  ";
                    if (skin.avoidsBombs)
                    {
                        buffInfo += "★ NE BOM  ";
                    }
                }
                if (levelManager != null && levelManager.HasEnergyDrinkActive)
                {
                    buffInfo += "⚡ +15s  ";
                }
                int shields = ShopManager.Instance != null ? ShopManager.Instance.GetBombShieldCount() : 0;
                if (shields > 0)
                {
                    buffInfo += $"🛡️ KHIEN BOM ({shields})";
                }
                _hudBuffStatusText.text = buffInfo;
            }

            UpdateDynamiteHUD();
        }

        public void UpdateDynamiteHUD()
        {
            int count = SaveSystem.GetDynamiteCount();
            if (count != _lastDynamiteCount)
            {
                _lastDynamiteCount = count;
                if (_dynamiteHudText != null)
                {
                    _dynamiteHudText.text = $"💣 TNT ({_lastDynamiteCount}) [SPACE]";
                }
            }
        }

        private void ShowLevelComplete()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayWin();
            int currentLvl = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0;
            int score = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
            int earnedTokens = GameManager.Instance != null ? GameManager.Instance.LastEarnedTokens : 0;
            int totalTokens = SaveSystem.LoadTokens();
            int highScore = SaveSystem.LoadHighScore();
            bool hasNext = GameManager.Instance != null && GameManager.Instance.HasNextLevel;

            if (_completeTitleText != null)
            {
                _completeTitleText.text = $"VUOT AI MAN {currentLvl + 1} THANH CONG!";
            }

            if (_levelCompleteScoreText != null)
            {
                string nextNotice = hasNext
                    ? $"<color=#50FF80>🎉 DA MO KHOA MAN {currentLvl + 2}!</color>"
                    : "<color=#FFE600>🏆 CHUC MUNG BAN DA PHA DAO TOAN BO 8 MAN CHOI!</color>";
                _levelCompleteScoreText.text = $"DIEM: {score}   |   KY LUC: {highScore}\n{nextNotice}";
            }

            if (_rewardText != null)
            {
                _rewardText.text = $"THUONG QUA MAN: +{earnedTokens} TOKEN!\nTONG TUI TIEN HIEN CO: {totalTokens} TOKEN";
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.SetActive(hasNext);
                TMP_Text nextTxt = _nextLevelButton.GetComponentInChildren<TMP_Text>();
                if (nextTxt != null)
                {
                    nextTxt.text = $"▶  QUA MAN {currentLvl + 2}";
                }
            }
        }

        private void ShowGameOver()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayGameOver();
            int score = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
            int consolation = GameManager.Instance != null ? GameManager.Instance.LastConsolationTokens : 0;
            int totalTokens = SaveSystem.LoadTokens();
            int highScore = SaveSystem.LoadHighScore();

            if (_gameOverTitleText != null) _gameOverTitleText.text = "THUA CUOC (GAME OVER)";

            if (_gameOverReasonText != null && GameManager.Instance != null)
            {
                _gameOverReasonText.text = SanitizeText(GameManager.Instance.LastGameOverReason);
            }

            if (_gameOverScoreText != null)
            {
                _gameOverScoreText.text = $"DIEM: {score}   |   KY LUC: {highScore}\nTHUONG AN UI: +{consolation} TOKEN\nTONG TUI TIEN: {totalTokens} TOKEN";
            }
        }

        public void UpdateShopUI()
        {
            UpdateShopTabDisplay();
            int tokens = SaveSystem.LoadTokens();
            if (_shopTokenText != null)
            {
                _shopTokenText.text = $"★  SO DU TOKEN CUA BAN: <size=25><color=#FFE600><b>{tokens} TOKEN</b></color></size>  ★";
            }

            // 1. Tời Trợ Lực (Pull Power)
            int pullLvl = SaveSystem.GetUpgradeLevel(SaveSystem.KeyPullPower);
            if (_pullPowerTxt != null)
            {
                if (pullLvl >= ShopManager.MaxPullLevel)
                {
                    _pullPowerTxt.text =
                        "<size=16><b><color=#FFE066>⚡ TOI TRO LUC</color>  <color=#00E5FF>[CAP TOI DA - MAX]</color></b></size>\n" +
                        "<size=12><color=#D2DAE8>Tang +75% toc do keo. Keo vang to & da cuc nhanh, tiet kiem thoi gian!</color></size>\n" +
                        "<size=13><b><color=#50FF80>✔ DA DAT CAP TOI DA</color></b></size>";
                }
                else
                {
                    int cost = ShopManager.PullPowerCosts[pullLvl];
                    bool canAfford = tokens >= cost;
                    string actionTag = canAfford
                        ? $"<color=#50FF80>▶ MUA: {cost} TOKEN (Nang len Cap {pullLvl + 1})</color>"
                        : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {cost} TOKEN)</color>";
                    _pullPowerTxt.text =
                        $"<size=16><b><color=#FFE066>⚡ TOI TRO LUC</color>  <color=#A0E0FF>[CAP {pullLvl}/3]</color></b></size>\n" +
                        $"<size=12><color=#D2DAE8>Tang +25% toc do keo moi cap. Keo vang to & da nang cuc nhanh!</color></size>\n" +
                        $"<size=13><b>{actionTag}</b></size>";
                }
            }
            if (_buyPullPowerBtn != null)
            {
                _buyPullPowerBtn.interactable = pullLvl < ShopManager.MaxPullLevel && tokens >= ShopManager.PullPowerCosts[pullLvl];
            }

            // 2. Cáp Siêu Tốc (Cable Speed)
            int cableLvl = SaveSystem.GetUpgradeLevel(SaveSystem.KeyCableSpeed);
            if (_cableSpeedTxt != null)
            {
                if (cableLvl >= ShopManager.MaxCableLevel)
                {
                    _cableSpeedTxt.text =
                        "<size=16><b><color=#FFE066>🚀 CAP SIEU TOC</color>  <color=#00E5FF>[CAP TOI DA - MAX]</color></b></size>\n" +
                        "<size=12><color=#D2DAE8>Tang +60% toc do phong moc. Phong chanh chop bat moi muc tieu o xa!</color></size>\n" +
                        "<size=13><b><color=#50FF80>✔ DA DAT CAP TOI DA</color></b></size>";
                }
                else
                {
                    int cost = ShopManager.CableSpeedCosts[cableLvl];
                    bool canAfford = tokens >= cost;
                    string actionTag = canAfford
                        ? $"<color=#50FF80>▶ MUA: {cost} TOKEN (Nang len Cap {cableLvl + 1})</color>"
                        : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {cost} TOKEN)</color>";
                    _cableSpeedTxt.text =
                        $"<size=16><b><color=#FFE066>🚀 CAP SIEU TOC</color>  <color=#A0E0FF>[CAP {cableLvl}/3]</color></b></size>\n" +
                        $"<size=12><color=#D2DAE8>Tang +20% toc do phong moc moi cap. Bat vat o xa nhanh hon rat nhieu!</color></size>\n" +
                        $"<size=13><b>{actionTag}</b></size>";
                }
            }
            if (_buyCableSpeedBtn != null)
            {
                _buyCableSpeedBtn.interactable = cableLvl < ShopManager.MaxCableLevel && tokens >= ShopManager.CableSpeedCosts[cableLvl];
            }

            // 3. Móc May Mắn (Lucky Hook)
            int luckyLvl = SaveSystem.GetUpgradeLevel(SaveSystem.KeyLuckyHook);
            if (_luckyHookTxt != null)
            {
                if (luckyLvl >= ShopManager.MaxLuckyLevel)
                {
                    _luckyHookTxt.text =
                        "<size=16><b><color=#FFE066>🍀 MOC MAY MAN</color>  <color=#00E5FF>[CAP TOI DA - MAX]</color></b></size>\n" +
                        "<size=12><color=#D2DAE8>Tang +75% diem & Token tu vang/kim cuong. Thuong toi da moi man!</color></size>\n" +
                        "<size=13><b><color=#50FF80>✔ DA DAT CAP TOI DA</color></b></size>";
                }
                else
                {
                    int cost = ShopManager.LuckyHookCosts[luckyLvl];
                    bool canAfford = tokens >= cost;
                    string actionTag = canAfford
                        ? $"<color=#50FF80>▶ MUA: {cost} TOKEN (Nang len Cap {luckyLvl + 1})</color>"
                        : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {cost} TOKEN)</color>";
                    _luckyHookTxt.text =
                        $"<size=16><b><color=#FFE066>🍀 MOC MAY MAN</color>  <color=#A0E0FF>[CAP {luckyLvl}/3]</color></b></size>\n" +
                        $"<size=12><color=#D2DAE8>Tang +25% diem so & Token tu tat ca Vang & Kim Cuong moi cap.</color></size>\n" +
                        $"<size=13><b>{actionTag}</b></size>";
                }
            }
            if (_buyLuckyHookBtn != null)
            {
                _buyLuckyHookBtn.interactable = luckyLvl < ShopManager.MaxLuckyLevel && tokens >= ShopManager.LuckyHookCosts[luckyLvl];
            }

            // 4. Thuốc Nổ TNT (Dynamite)
            int dynCount = SaveSystem.GetDynamiteCount();
            int dynCost = ShopManager.DynamiteCost;
            if (_dynamiteTxt != null)
            {
                bool canAfford = tokens >= dynCost;
                string actionTag = canAfford
                    ? $"<color=#50FF80>▶ MUA THEM (+1): {dynCost} TOKEN</color>"
                    : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {dynCost} TOKEN)</color>";
                _dynamiteTxt.text =
                    $"<size=16><b><color=#FF8866>💣 THUOC NO TNT</color>  <color=#A0E0FF>[DANG CO: {dynCount} QUA]</color></b></size>\n" +
                    $"<size=12><color=#D2DAE8>Pha huy ngay vat dang keo khi gap nham da nang. Bam SPACE hoac nut TNT!</color></size>\n" +
                    $"<size=13><b>{actionTag}</b></size>";
            }
            if (_buyDynamiteBtn != null)
            {
                _buyDynamiteBtn.interactable = tokens >= dynCost;
            }

            // 5. Nước Tăng Lực (Energy Drink)
            int energyCount = SaveSystem.GetEnergyDrinkCount();
            int energyCost = ShopManager.EnergyDrinkCost;
            if (_energyDrinkTxt != null)
            {
                bool canAfford = tokens >= energyCost;
                string actionTag = canAfford
                    ? $"<color=#50FF80>▶ MUA THEM (+1): {energyCost} TOKEN</color>"
                    : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {energyCost} TOKEN)</color>";
                _energyDrinkTxt.text =
                    $"<size=16><b><color=#66FFBB>🥤 NUOC TANG LUC</color>  <color=#A0E0FF>[DANG CO: {energyCount} CHAI]</color></b></size>\n" +
                    $"<size=12><color=#D2DAE8>Tu dong kich hoat dau man, cong them +15 GIAY thoi gian vuot man kho!</color></size>\n" +
                    $"<size=13><b>{actionTag}</b></size>";
            }
            if (_buyEnergyDrinkBtn != null)
            {
                _buyEnergyDrinkBtn.interactable = tokens >= energyCost;
            }

            // 6. Khiên Gỡ Bom (Bomb Defuser Shield)
            int shieldCount = SaveSystem.GetBombShieldCount();
            int shieldCost = ShopManager.BombShieldCost;
            if (_bombShieldTxt != null)
            {
                bool canAfford = tokens >= shieldCost;
                string actionTag = canAfford
                    ? $"<color=#50FF80>▶ MUA THEM (+1): {shieldCost} TOKEN</color>"
                    : $"<color=#FF7070>🔒 CHUA DU TOKEN (CAN: {shieldCost} TOKEN)</color>";
                _bombShieldTxt.text =
                    $"<size=16><b><color=#66D5FF>🛡️ KHIEN GO BOM</color>  <color=#A0E0FF>[DANG CO: {shieldCount} CAI]</color></b></size>\n" +
                    $"<size=12><color=#D2DAE8>BAO HO: Tu dong go va vo hieu hoa 1 qua bom khi keo trung, tranh thua tran!</color></size>\n" +
                    $"<size=13><b>{actionTag}</b></size>";
            }
            if (_buyBombShieldBtn != null)
            {
                _buyBombShieldBtn.interactable = tokens >= shieldCost;
            }

            // 7. Cập nhật trạng thái các thẻ Skin Móc VIP
            string currentEquipped = SaveSystem.GetEquippedSkin();
            var allSkins = ShopManager.AllSkins;
            for (int i = 0; i < allSkins.Length; i++)
            {
                if (i >= _skinCardTexts.Count || i >= _skinCardBtns.Count) continue;
                var skin = allSkins[i];
                var txt = _skinCardTexts[i];
                var btn = _skinCardBtns[i];
                if (txt == null || btn == null) continue;

                bool isOwned = SaveSystem.IsSkinOwned(skin.id);
                bool isEquipped = currentEquipped == skin.id;

                string statusAction;
                if (isEquipped)
                {
                    statusAction = "<size=13><b><color=#50FF80>✔ DANG TRANG BI (ACTIVE)</color></b></size>";
                    btn.interactable = false;
                }
                else if (isOwned)
                {
                    statusAction = "<size=13><b><color=#00E5FF>⚡ BAM DE TRANG BI NGAY</color></b></size>";
                    btn.interactable = true;
                }
                else
                {
                    bool canAfford = tokens >= skin.cost;
                    if (canAfford)
                    {
                        statusAction = $"<size=13><b><color=#FFE600>💰 MUA: {skin.cost:N0} TOKEN (BAM DE MUA)</color></b></size>";
                        btn.interactable = true;
                    }
                    else
                    {
                        statusAction = $"<size=13><b><color=#FF7070>🔒 CHUA DU TOKEN (CAN: {skin.cost:N0} TOKEN)</color></b></size>";
                        btn.interactable = false;
                    }
                }

                string colorHex;
                switch (skin.id)
                {
                    case SaveSystem.SkinGoldenId: colorHex = "#FFD700"; break;
                    case SaveSystem.SkinThunderId: colorHex = "#00E5FF"; break;
                    case SaveSystem.SkinDragonId: colorHex = "#FF3355"; break;
                    default: colorHex = "#E0E8F5"; break;
                }

                txt.text = $"<size=16><b><color={colorHex}>{skin.name}</color>  <color=#A0E0FF>[{skin.subtitle}]</color></b></size>\n" +
                           $"<size=12><color=#D2DAE8>{skin.perkDescription}</color></size>\n" +
                           $"{statusAction}";
            }
        }

        private void HandleShopDataChanged()
        {
            UpdateShopUI();
            if (_hudTokenText != null) _hudTokenText.text = $"🪙 TOKEN: <color=#FFE600><b>{SaveSystem.LoadTokens()}</b></color>";
            if (_menuTokenText != null)
            {
                int tokens = SaveSystem.LoadTokens();
                int highScore = SaveSystem.LoadHighScore();
                _menuTokenText.text = $"★  SO DU TOKEN: <size=24><color=#FFE600><b>{tokens} TOKEN</b></color></size>   |   KY LUC: <color=#66FFBB><b>{highScore} DIEM</b></color>  ★";
            }
            if (_levelSelectTokenText != null)
            {
                _levelSelectTokenText.text = $"★  SO DU TOKEN: <size=24><color=#FFE600><b>{SaveSystem.LoadTokens()} TOKEN</b></color></size>  ★";
            }
        }

        // =========================================================================
        // USER ACTIONS / BUTTON EVENTS
        // =========================================================================
        public void OnClickPlay()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.GoToLevelSelect();
        }

        public void OnClickSelectLevel(int levelIndex)
        {
            int unlockedIdx = GameManager.Instance != null ? GameManager.Instance.UnlockedLevelIndex : SaveSystem.LoadUnlockedLevel();
            bool isUnlocked = (levelIndex == 0) || (levelIndex <= unlockedIdx);
            if (!isUnlocked)
            {
                OnClickLockedLevel(levelIndex);
                return;
            }

            DismissLockedToast();
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.SelectLevel(levelIndex);
        }

        public void OnClickLockedLevel(int levelIndex)
        {
            DaoVang.Audio.AudioManager.Instance?.PlayBomb();
            ShowLockedLevelToast(levelIndex);
        }

        public void DismissLockedToast()
        {
            if (_lockedToastCoroutine != null)
            {
                StopCoroutine(_lockedToastCoroutine);
                _lockedToastCoroutine = null;
            }
            if (_lockedLevelToastObj != null)
            {
                _lockedLevelToastObj.SetActive(false);
            }
        }

        public void ShowLockedLevelToast(int levelIndex)
        {
            if (_lockedLevelToastObj == null) return;

            string message = $"<size=20><color=#FF5555><b>🔒  MAN {levelIndex + 1} DANG BI KHOA!</b></color></size>\n" +
                             $"<size=15><color=#FFFFFF>Ban phai vuot qua </color><color=#FFE600><b>MAN {levelIndex}</b></color><color=#FFFFFF> truoc do moi co the mo khoa man nay!</color></size>\n" +
                             $"<size=11><color=#CCCCCC>(Bam vao thong bao de dong)</color></size>";

            if (_lockedToastText != null)
            {
                _lockedToastText.text = message;
            }

            if (_lockedToastCoroutine != null)
            {
                StopCoroutine(_lockedToastCoroutine);
            }
            _lockedToastCoroutine = StartCoroutine(LockedToastRoutine());
        }

        private IEnumerator LockedToastRoutine()
        {
            if (_lockedLevelToastObj == null) yield break;

            _lockedLevelToastObj.SetActive(true);
            _lockedLevelToastObj.transform.SetAsLastSibling();

            RectTransform rt = _lockedLevelToastObj.GetComponent<RectTransform>();
            Vector3 originalScale = Vector3.one;

            // Hieu ung phong to nay nhe
            float elapsed = 0f;
            float animDuration = 0.18f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animDuration;
                float scale = Mathf.Lerp(0.7f, 1.06f, t);
                if (rt != null) rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (rt != null) rt.localScale = originalScale;

            // Giu hien thi 2.8 giay
            yield return new WaitForSecondsRealtime(2.8f);

            // Thu nho va an
            elapsed = 0f;
            float fadeDuration = 0.15f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                float scale = Mathf.Lerp(1f, 0.8f, t);
                if (rt != null) rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (rt != null) rt.localScale = originalScale;
            _lockedLevelToastObj.SetActive(false);
            _lockedToastCoroutine = null;
        }

        public void OnClickOpenShop()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.OpenShop();
        }

        public void OnClickCloseShop()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.CloseShop();
        }

        public void OnClickOpenTutorial()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            if (_tutorialModal != null) _tutorialModal.SetActive(true);
        }

        public void OnClickCloseTutorial()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            if (_tutorialModal != null) _tutorialModal.SetActive(false);
        }

        public void OnClickToggleSound()
        {
            float currentVol = AudioListener.volume;
            float newVol = currentVol > 0.1f ? 0f : 1f;
            AudioListener.volume = newVol;
            SaveSystem.SaveSoundVolume(newVol);
            if (_soundToggleText != null)
            {
                _soundToggleText.text = newVol > 0.1f ? "🔊  AM THANH: BAT" : "🔇  AM THANH: TAT";
            }
        }

        public void OnClickQuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnClickRestart()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.RestartLevel();
        }

        public void OnClickNextLevel()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.GoToNextLevel();
        }

        public void OnClickBackToMenuFromGameOver()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.ReturnToMenu();
        }

        public void OnClickBackToMenuFromComplete()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.ReturnToMenu();
        }

        public void OnClickBackToMenuFromPlaying()
        {
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.ReturnToMenu();
        }

        public void OnClickBackToMenuFromLevelSelect()
        {
            DismissLockedToast();
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
            GameManager.Instance?.ReturnToMenu();
        }

        public void OnClickUseDynamite()
        {
#if UNITY_2023_1_OR_NEWER
            var hook = FindAnyObjectByType<DaoVang.Hook.HookController>();
#else
            var hook = FindObjectOfType<DaoVang.Hook.HookController>();
#endif
            if (hook != null && hook.UseDynamite())
            {
                UpdateDynamiteHUD();
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            _targetScore = newScore;
            if (_scorePunchCoroutine != null) StopCoroutine(_scorePunchCoroutine);
            _scorePunchCoroutine = StartCoroutine(ScorePunchRoutine());
        }

        private IEnumerator ScorePunchRoutine()
        {
            if (_scoreText == null) yield break;
            Transform t = _scoreText.transform;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.22f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / dur;
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.25f;
                t.localScale = originalScale * scale;
                yield return null;
            }
            t.localScale = originalScale;
        }

        private void HandleTimeChanged(float timeLeft)
        {
            if (_timeText == null) return;

            int s = Mathf.CeilToInt(timeLeft);
            if (s != _lastFormattedSeconds)
            {
                _lastFormattedSeconds = s;
                _timeText.text = $"{s / 60:00}:{s % 60:00}";
            }

            if (timeLeft <= 10f && timeLeft > 0f)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                _timeText.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.25f), pulse);
                _timeText.transform.localScale = Vector3.one * (1f + 0.15f * pulse);
            }
            else
            {
                _timeText.color = Color.white;
                _timeText.transform.localScale = Vector3.one;
            }
        }
    }
}
