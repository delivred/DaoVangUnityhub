using UnityEngine;

namespace DaoVang.Data
{
    /// <summary>
    /// Lưu trữ dữ liệu game tối ưu bằng PlayerPrefs kết hợp Bộ nhớ đệm (In-Memory Cache):
    /// - Điểm cao (HighScore), màn chơi đã mở khóa (UnlockedLevel)
    /// - Tiền tệ TOKEN (Xu Thợ Mỏ) với 500 Token ban đầu
    /// - Các cấp độ nâng cấp trang bị móc câu (Shop Upgrades) và 6 loại Buff độc đáo
    /// - Trạng thái âm thanh (Sound Volume)
    /// </summary>
    public static class SaveSystem
    {
        private const string HighScoreKey = "DaoVang_HighScore";
        private const string UnlockedLevelKey = "DaoVang_UnlockedLevel";
        private const string TokensKey = "DaoVang_Tokens";
        private const string LegacyCoinsKey = "DaoVang_Coins";
        private const string SoundVolumeKey = "DaoVang_SoundVolume";

        // Upgrade & Buff Keys
        public const string KeyPullPower = "Upgrade_PullPower";
        public const string KeyCableSpeed = "Upgrade_CableSpeed";
        public const string KeyLuckyHook = "Upgrade_LuckyHook";
        public const string KeyDynamite = "Upgrade_Dynamite";
        public const string KeyEnergyDrink = "Upgrade_EnergyDrink";
        public const string KeyBombShield = "Upgrade_BombShield";

        // In-Memory Cache để tối ưu hiệu năng, tránh đọc PlayerPrefs I/O liên tục mỗi frame
        private static int? _cachedHighScore;
        private static int? _cachedUnlockedLevel;
        private static int? _cachedTokens;
        private static float? _cachedSoundVolume;

        public static void SaveHighScore(int score)
        {
            int current = LoadHighScore();
            if (score > current)
            {
                _cachedHighScore = score;
                PlayerPrefs.SetInt(HighScoreKey, score);
                PlayerPrefs.Save();
            }
        }

        public static int LoadHighScore()
        {
            if (_cachedHighScore.HasValue) return _cachedHighScore.Value;
            _cachedHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            return _cachedHighScore.Value;
        }

        public static void SaveUnlockedLevel(int levelIndex)
        {
            int current = LoadUnlockedLevel();
            if (levelIndex > current)
            {
                _cachedUnlockedLevel = levelIndex;
                PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex);
                PlayerPrefs.Save();
            }
        }

        public static int LoadUnlockedLevel()
        {
            if (_cachedUnlockedLevel.HasValue) return _cachedUnlockedLevel.Value;
            _cachedUnlockedLevel = PlayerPrefs.GetInt(UnlockedLevelKey, 0);
            return _cachedUnlockedLevel.Value;
        }

        private const string LevelWonPrefix = "DaoVang_Won_Level_";

        /// <summary>
        /// Đánh dấu màn chơi đã chiến thắng (vượt ải thành công) và mở khóa màn tiếp theo vĩnh viễn
        /// </summary>
        public static void MarkLevelWon(int levelIndex)
        {
            PlayerPrefs.SetInt(LevelWonPrefix + levelIndex, 1);
            SaveUnlockedLevel(levelIndex + 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Kiểm tra xem màn chơi đã từng chiến thắng trước đây chưa
        /// </summary>
        public static bool IsLevelWon(int levelIndex)
        {
            if (PlayerPrefs.GetInt(LevelWonPrefix + levelIndex, 0) == 1) return true;
            // Tương thích ngược: Nếu màn cao hơn đã được mở khóa, các màn trước đó mặc định đã win
            return levelIndex < LoadUnlockedLevel();
        }

        /// <summary>
        /// Tổng số màn chơi người chơi đã hoàn thành chiến thắng
        /// </summary>
        public static int GetTotalLevelsWon(int totalLevels = 8)
        {
            int count = 0;
            for (int i = 0; i < totalLevels; i++)
            {
                if (IsLevelWon(i)) count++;
            }
            return count;
        }

        /// <summary>
        /// Đặt lại tiến trình vượt ải về Màn 1 (dùng khi muốn chơi lại từ đầu)
        /// </summary>
        public static void ResetCampaignProgress()
        {
            _cachedUnlockedLevel = 0;
            PlayerPrefs.SetInt(UnlockedLevelKey, 0);
            for (int i = 0; i < 16; i++)
            {
                PlayerPrefs.DeleteKey(LevelWonPrefix + i);
            }
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // Tiền tệ TOKEN (Xu Thợ Mỏ)
        // -------------------------------------------------------------
        /// <summary>
        /// Tải số lượng Token hiện có từ cache RAM. Mặc định tặng sẵn 500 Token ban đầu để mua buff trải nghiệm.
        /// </summary>
        public static int LoadTokens()
        {
            if (_cachedTokens.HasValue) return _cachedTokens.Value;

            if (PlayerPrefs.HasKey(TokensKey))
            {
                _cachedTokens = PlayerPrefs.GetInt(TokensKey);
                return _cachedTokens.Value;
            }
            if (PlayerPrefs.HasKey(LegacyCoinsKey))
            {
                int legacy = PlayerPrefs.GetInt(LegacyCoinsKey);
                _cachedTokens = Mathf.Max(500, legacy);
                SaveTokens(_cachedTokens.Value);
                return _cachedTokens.Value;
            }

            _cachedTokens = 500;
            return _cachedTokens.Value;
        }

        public static void SaveTokens(int amount)
        {
            int val = Mathf.Max(0, amount);
            _cachedTokens = val;
            PlayerPrefs.SetInt(TokensKey, val);
            PlayerPrefs.SetInt(LegacyCoinsKey, val);
            PlayerPrefs.Save();
        }

        public static void AddTokens(int amount)
        {
            int current = LoadTokens();
            SaveTokens(current + amount);
        }

        // Tương thích ngược với các module gọi Coins
        public static int LoadCoins() => LoadTokens();
        public static void SaveCoins(int amount) => SaveTokens(amount);
        public static void AddCoins(int amount) => AddTokens(amount);

        // -------------------------------------------------------------
        // Nâng cấp & Vật phẩm Buff Cửa Hàng
        // -------------------------------------------------------------
        public static int GetUpgradeLevel(string key) => PlayerPrefs.GetInt(key, 0);

        public static void SetUpgradeLevel(string key, int level)
        {
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
        }

        public static int GetDynamiteCount() => PlayerPrefs.GetInt(KeyDynamite, 1); // Tặng sẵn 1 quả TNT

        public static void SetDynamiteCount(int count)
        {
            PlayerPrefs.SetInt(KeyDynamite, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }

        public static int GetEnergyDrinkCount() => PlayerPrefs.GetInt(KeyEnergyDrink, 0);

        public static void SetEnergyDrinkCount(int count)
        {
            PlayerPrefs.SetInt(KeyEnergyDrink, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }

        public static int GetBombShieldCount() => PlayerPrefs.GetInt(KeyBombShield, 0);

        public static void SetBombShieldCount(int count)
        {
            PlayerPrefs.SetInt(KeyBombShield, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // Cài đặt Âm thanh
        // -------------------------------------------------------------
        public static float LoadSoundVolume()
        {
            if (_cachedSoundVolume.HasValue) return _cachedSoundVolume.Value;
            _cachedSoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
            return _cachedSoundVolume.Value;
        }

        public static void SaveSoundVolume(float volume)
        {
            _cachedSoundVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SoundVolumeKey, _cachedSoundVolume.Value);
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // Hệ thống Skin Móc VIP
        // -------------------------------------------------------------
        public const string DefaultSkinId = "default";
        public const string SkinGoldenId = "golden";
        public const string SkinThunderId = "thunder";
        public const string SkinDragonId = "dragon";

        private const string EquippedSkinKey = "DaoVang_EquippedSkin";
        private const string SkinOwnedPrefix = "DaoVang_SkinOwned_";
        private static string _cachedEquippedSkin;

        public static string GetEquippedSkin()
        {
            if (!string.IsNullOrEmpty(_cachedEquippedSkin)) return _cachedEquippedSkin;
            _cachedEquippedSkin = PlayerPrefs.GetString(EquippedSkinKey, DefaultSkinId);
            return _cachedEquippedSkin;
        }

        public static void SetEquippedSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) skinId = DefaultSkinId;
            _cachedEquippedSkin = skinId;
            PlayerPrefs.SetString(EquippedSkinKey, skinId);
            PlayerPrefs.Save();
        }

        public static bool IsSkinOwned(string skinId)
        {
            if (string.IsNullOrEmpty(skinId) || skinId == DefaultSkinId) return true;
            return PlayerPrefs.GetInt(SkinOwnedPrefix + skinId, 0) == 1;
        }

        public static void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            PlayerPrefs.SetInt(SkinOwnedPrefix + skinId, 1);
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // Hệ thống Đăng Nhập & Phân Quyền Admin
        // -------------------------------------------------------------
        private const string CurrentUsernameKey = "DaoVang_CurrentUsername";
        private const string IsAdminKey = "DaoVang_IsAdmin";
        private static string _cachedUsername;
        private static bool? _cachedIsAdmin;

        public static string GetCurrentUsername()
        {
            if (!string.IsNullOrEmpty(_cachedUsername)) return _cachedUsername;
            _cachedUsername = PlayerPrefs.GetString(CurrentUsernameKey, "Khach");
            return _cachedUsername;
        }

        public static bool IsAdminLoggedIn()
        {
            if (_cachedIsAdmin.HasValue) return _cachedIsAdmin.Value;
            _cachedIsAdmin = PlayerPrefs.GetInt(IsAdminKey, 0) == 1;
            return _cachedIsAdmin.Value;
        }

        public static void LoginAsAdmin()
        {
            _cachedUsername = "admin";
            _cachedIsAdmin = true;
            PlayerPrefs.SetString(CurrentUsernameKey, "admin");
            PlayerPrefs.SetInt(IsAdminKey, 1);

            // Mở khóa toàn bộ 8 Màn chơi
            for (int i = 0; i < 8; i++)
            {
                PlayerPrefs.SetInt(LevelWonPrefix + i, 1);
            }
            SaveUnlockedLevel(7);

            // Mở khóa toàn bộ Skin Móc VIP và tự trang bị Móc Hỏa Long x2 điểm
            UnlockSkin(SkinGoldenId);
            UnlockSkin(SkinThunderId);
            UnlockSkin(SkinDragonId);
            SetEquippedSkin(SkinDragonId);

            // Tối đa hóa mọi chỉ số nâng cấp
            SetUpgradeLevel(KeyPullPower, 3);
            SetUpgradeLevel(KeyCableSpeed, 3);
            SetUpgradeLevel(KeyLuckyHook, 3);

            // Tặng 99 mỗi loại vật phẩm tiêu hao
            SetDynamiteCount(Mathf.Max(99, GetDynamiteCount()));
            SetEnergyDrinkCount(Mathf.Max(99, GetEnergyDrinkCount()));
            SetBombShieldCount(Mathf.Max(99, GetBombShieldCount()));

            // Tặng 999,999 Tokens cho Admin
            if (LoadTokens() < 999999)
            {
                AddTokens(999999 - LoadTokens());
            }

            PlayerPrefs.Save();
        }

        public static void LoginAsPlayer(string username)
        {
            if (string.IsNullOrEmpty(username)) username = "Player";
            _cachedUsername = username;
            _cachedIsAdmin = false;
            PlayerPrefs.SetString(CurrentUsernameKey, username);
            PlayerPrefs.SetInt(IsAdminKey, 0);
            PlayerPrefs.Save();
        }

        public static void Logout()
        {
            _cachedUsername = "Khach";
            _cachedIsAdmin = false;
            PlayerPrefs.SetString(CurrentUsernameKey, "Khach");
            PlayerPrefs.SetInt(IsAdminKey, 0);
            PlayerPrefs.Save();
        }
    }
}
