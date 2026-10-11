using System;
using UnityEngine;
using DaoVang.Data;

namespace DaoVang.Shop
{
    /// <summary>
    /// Quản lý dữ liệu, bảng giá và chỉ số nâng cấp trang bị móc câu và Buff trong Cửa Hàng (Shop) bằng TOKEN.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public event Action OnShopDataChanged;

        // Bảng giá nâng cấp & buff (bằng TOKEN)
        public static readonly int[] PullPowerCosts = { 200, 450, 900 };
        public static readonly int[] CableSpeedCosts = { 150, 350, 750 };
        public static readonly int[] LuckyHookCosts = { 250, 550, 1100 };
        public const int DynamiteCost = 100;
        public const int EnergyDrinkCost = 150;
        public const int BombShieldCost = 200;

        public const int MaxPullLevel = 3;
        public const int MaxCableLevel = 3;
        public const int MaxLuckyLevel = 3;

        // Danh mục Skin Móc VIP
        public static readonly HookSkinData[] AllSkins = new HookSkinData[]
        {
            new HookSkinData
            {
                id = SaveSystem.DefaultSkinId,
                name = "MOC TRUYEN THONG",
                subtitle = "CO DIEN",
                cost = 0,
                scoreMultiplier = 1.0f,
                extendSpeedBonus = 0f,
                pullSpeedBonus = 0f,
                avoidsBombs = false,
                headColor = Color.white,
                ropeColor = new Color(1f, 0.85f, 0.2f, 1f),
                perkDescription = "Moc tieu chuan. Diem so va toc do theo cap nang cap co ban."
            },
            new HookSkinData
            {
                id = SaveSystem.SkinGoldenId,
                name = "MOC HOANG GIA (GOLDEN)",
                subtitle = "VIP HOANG GIA",
                cost = 20000,
                scoreMultiplier = 2.0f,
                extendSpeedBonus = 0.50f,
                pullSpeedBonus = 0.80f,
                avoidsBombs = false,
                headColor = new Color(1f, 0.84f, 0.1f, 1f),
                ropeColor = new Color(1f, 0.92f, 0.35f, 1f),
                perkDescription = "★ X2.0 DIEM SO & TOKEN tu moi vat pham\n★ +50% Toc do phong & +80% Toc do keo ve"
            },
            new HookSkinData
            {
                id = SaveSystem.SkinThunderId,
                name = "MOC LOI THAN (THUNDER)",
                subtitle = "VIP SAM SET",
                cost = 35000,
                scoreMultiplier = 2.0f,
                extendSpeedBonus = 0.75f,
                pullSpeedBonus = 1.50f,
                avoidsBombs = false,
                headColor = new Color(0.1f, 0.95f, 1f, 1f),
                ropeColor = new Color(0.2f, 0.85f, 1f, 1f),
                perkDescription = "★ X2.0 DIEM SO & TOKEN tu moi vat pham\n★ +75% Toc do phong & +150% Toc do keo sieu toc"
            },
            new HookSkinData
            {
                id = SaveSystem.SkinDragonId,
                name = "MOC HOA LONG (MYTHIC)",
                subtitle = "VIP THAN THOAI",
                cost = 50000,
                scoreMultiplier = 2.0f,
                extendSpeedBonus = 1.00f,
                pullSpeedBonus = 2.50f,
                avoidsBombs = true,
                headColor = new Color(1f, 0.22f, 0.25f, 1f),
                ropeColor = new Color(1f, 0.45f, 0.1f, 1f),
                perkDescription = "★ NE HOAN TOAN BOM NO (KHONG BI NO TUNG)\n★ X2.0 DIEM SO & +250% SUC KEO CUONG PHONG"
            }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // -------------------------------------------------------------
        // Thuộc tính lấy chỉ số trực tiếp cho HookController & Gameplay
        // -------------------------------------------------------------

        public HookSkinData GetSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) skinId = SaveSystem.DefaultSkinId;
            for (int i = 0; i < AllSkins.Length; i++)
            {
                if (AllSkins[i].id == skinId) return AllSkins[i];
            }
            return AllSkins[0];
        }

        public HookSkinData GetEquippedSkinData()
        {
            return GetSkin(SaveSystem.GetEquippedSkin());
        }

        public bool BuySkin(string skinId)
        {
            HookSkinData skin = GetSkin(skinId);
            if (skin == null || SaveSystem.IsSkinOwned(skinId)) return false;

            int tokens = SaveSystem.LoadTokens();
            if (tokens >= skin.cost)
            {
                SaveSystem.AddTokens(-skin.cost);
                SaveSystem.UnlockSkin(skinId);
                SaveSystem.SetEquippedSkin(skinId);
                ApplyEquippedSkinToHook();
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool EquipSkin(string skinId)
        {
            if (!SaveSystem.IsSkinOwned(skinId)) return false;
            SaveSystem.SetEquippedSkin(skinId);
            ApplyEquippedSkinToHook();
            OnShopDataChanged?.Invoke();
            return true;
        }

        public void ApplyEquippedSkinToHook()
        {
#if UNITY_2023_1_OR_NEWER
            var hook = FindAnyObjectByType<DaoVang.Hook.HookController>();
#else
            var hook = FindObjectOfType<DaoVang.Hook.HookController>();
#endif
            if (hook != null)
            {
                hook.ApplyEquippedSkin();
            }
        }

        /// <summary>Hệ số tăng tốc độ thu móc: nâng cấp cơ bản (+25%/cấp) kết hợp với Skin Móc đang trang bị</summary>
        public float GetPullSpeedMultiplier()
        {
            int level = SaveSystem.GetUpgradeLevel(SaveSystem.KeyPullPower);
            float baseMultiplier = 1f + (level * 0.25f);
            HookSkinData skin = GetEquippedSkinData();
            float skinBonus = skin != null ? skin.pullSpeedBonus : 0f;
            return baseMultiplier * (1f + skinBonus);
        }

        /// <summary>Hệ số tăng tốc độ phóng móc: nâng cấp cơ bản (+20%/cấp) kết hợp với Skin Móc đang trang bị</summary>
        public float GetExtendSpeedMultiplier()
        {
            int level = SaveSystem.GetUpgradeLevel(SaveSystem.KeyCableSpeed);
            float baseMultiplier = 1f + (level * 0.20f);
            HookSkinData skin = GetEquippedSkinData();
            float skinBonus = skin != null ? skin.extendSpeedBonus : 0f;
            return baseMultiplier * (1f + skinBonus);
        }

        /// <summary>Hệ số tăng điểm số: Móc May Mắn (+25%/cấp) kết hợp với x2.0 từ Skin Móc VIP</summary>
        public float GetScoreMultiplier()
        {
            int level = SaveSystem.GetUpgradeLevel(SaveSystem.KeyLuckyHook);
            float baseMultiplier = 1f + (level * 0.25f);
            HookSkinData skin = GetEquippedSkinData();
            float skinScoreMultiplier = skin != null ? skin.scoreMultiplier : 1.0f;
            return baseMultiplier * skinScoreMultiplier;
        }

        public int GetDynamiteCount() => SaveSystem.GetDynamiteCount();

        public bool UseDynamite()
        {
            int current = GetDynamiteCount();
            if (current > 0)
            {
                SaveSystem.SetDynamiteCount(current - 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetEnergyDrinkCount() => SaveSystem.GetEnergyDrinkCount();

        /// <summary>Tiêu thụ 1 Nước Tăng Lực khi bắt đầu màn chơi để nhận thêm +15s</summary>
        public bool ConsumeEnergyDrink()
        {
            int current = GetEnergyDrinkCount();
            if (current > 0)
            {
                SaveSystem.SetEnergyDrinkCount(current - 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetBombShieldCount() => SaveSystem.GetBombShieldCount();

        /// <summary>Tiêu thụ 1 Khiên Gỡ Bom khi lỡ kéo phải bom để vô hiệu hóa bom an toàn</summary>
        public bool ConsumeBombShield()
        {
            int current = GetBombShieldCount();
            if (current > 0)
            {
                SaveSystem.SetBombShieldCount(current - 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        // -------------------------------------------------------------
        // Các hàm giao dịch Cửa hàng bằng TOKEN
        // -------------------------------------------------------------

        public bool BuyPullPower()
        {
            int currentLevel = SaveSystem.GetUpgradeLevel(SaveSystem.KeyPullPower);
            if (currentLevel >= MaxPullLevel) return false;

            int cost = PullPowerCosts[currentLevel];
            if (SaveSystem.LoadTokens() >= cost)
            {
                SaveSystem.AddTokens(-cost);
                SaveSystem.SetUpgradeLevel(SaveSystem.KeyPullPower, currentLevel + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool BuyCableSpeed()
        {
            int currentLevel = SaveSystem.GetUpgradeLevel(SaveSystem.KeyCableSpeed);
            if (currentLevel >= MaxCableLevel) return false;

            int cost = CableSpeedCosts[currentLevel];
            if (SaveSystem.LoadTokens() >= cost)
            {
                SaveSystem.AddTokens(-cost);
                SaveSystem.SetUpgradeLevel(SaveSystem.KeyCableSpeed, currentLevel + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool BuyLuckyHook()
        {
            int currentLevel = SaveSystem.GetUpgradeLevel(SaveSystem.KeyLuckyHook);
            if (currentLevel >= MaxLuckyLevel) return false;

            int cost = LuckyHookCosts[currentLevel];
            if (SaveSystem.LoadTokens() >= cost)
            {
                SaveSystem.AddTokens(-cost);
                SaveSystem.SetUpgradeLevel(SaveSystem.KeyLuckyHook, currentLevel + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool BuyDynamite()
        {
            if (SaveSystem.LoadTokens() >= DynamiteCost)
            {
                SaveSystem.AddTokens(-DynamiteCost);
                SaveSystem.SetDynamiteCount(GetDynamiteCount() + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool BuyEnergyDrink()
        {
            if (SaveSystem.LoadTokens() >= EnergyDrinkCost)
            {
                SaveSystem.AddTokens(-EnergyDrinkCost);
                SaveSystem.SetEnergyDrinkCount(GetEnergyDrinkCount() + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool BuyBombShield()
        {
            if (SaveSystem.LoadTokens() >= BombShieldCost)
            {
                SaveSystem.AddTokens(-BombShieldCost);
                SaveSystem.SetBombShieldCount(GetBombShieldCount() + 1);
                OnShopDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Kiểm tra xem Skin Móc hiện tại có né bom (miễn nhiễm bom) hay không</summary>
        public bool EquippedSkinAvoidsBombs()
        {
            HookSkinData skin = GetEquippedSkinData();
            return skin != null && skin.avoidsBombs;
        }
    }

    /// <summary>
    /// Cấu hình dữ liệu của một loại Skin Móc VIP
    /// </summary>
    [System.Serializable]
    public class HookSkinData
    {
        public string id;
        public string name;
        public string subtitle;
        public int cost;
        public float scoreMultiplier = 1.0f; // x1.0, x2.0
        public float extendSpeedBonus = 0f;  // +0%, +50%, +75%, +100%
        public float pullSpeedBonus = 0f;    // +0%, +50%, +75%, +100%
        public bool avoidsBombs = false;     // True nếu né/miễn nhiễm bom (Móc Hỏa Long)
        public Color headColor = Color.white;
        public Color ropeColor = Color.yellow;
        public string perkDescription;
    }
}
