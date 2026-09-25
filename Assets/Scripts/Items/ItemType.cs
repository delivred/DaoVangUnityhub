using System;
using UnityEngine;

namespace DaoVang.Items
{
    public enum ItemType
    {
        SmallGold,
        BigGold,
        Rock,
        Diamond,
        Bomb
    }

    /// <summary>
    /// Cấu hình thông số cho 1 loại vật phẩm. Dùng trong LevelData để định nghĩa
    /// "level này spawn loại nào, tỉ lệ bao nhiêu" mà không cần sửa code.
    /// </summary>
    [Serializable]
    public struct ItemConfig
    {
        public ItemType type;
        public GameObject prefab;

        [Tooltip("Điểm số khi kéo được vật phẩm này về")]
        public int scoreValue;

        [Tooltip("Khối lượng — càng nặng kéo càng chậm")]
        public float mass;

        [Tooltip("Hệ số điều chỉnh riêng tốc độ kéo, mặc định 1. Vd: kim cương trơn nên >1 dù nặng")]
        public float retractSpeedModifier;

        [Tooltip("Trọng số xuất hiện khi random spawn — số càng lớn càng dễ ra")]
        [Range(0f, 100f)]
        public float spawnWeight;
    }

    /// <summary>Bảng thông số mặc định tham khảo cho các loại vật phẩm theo đề bài.</summary>
    public static class ItemDefaults
    {
        // scoreValue, mass, retractSpeedModifier gợi ý — chỉnh trong Inspector theo ItemConfig ở trên.
        public static readonly (int score, float mass, float speedMod) SmallGold = (50, 1.0f, 1.0f);
        public static readonly (int score, float mass, float speedMod) BigGold = (150, 2.2f, 1.0f);
        public static readonly (int score, float mass, float speedMod) Rock = (10, 3.0f, 0.8f);
        public static readonly (int score, float mass, float speedMod) Diamond = (300, 1.5f, 1.4f);
        public static readonly (int score, float mass, float speedMod) Bomb = (0, 1.0f, 1.0f);
    }
}
