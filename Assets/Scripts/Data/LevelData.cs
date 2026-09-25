using UnityEngine;
using DaoVang.Items;

namespace DaoVang.Data
{
    /// <summary>
    /// Mỗi màn chơi là 1 asset LevelData riêng (chuột phải trong Project ->
    /// Create -> DaoVang -> Level Data). Thiết kế tối thiểu 3 asset: Level1, Level2, Level3,
    /// độ khó tăng dần bằng cách chỉnh các field bên dưới — không cần sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "DaoVang/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Mục tiêu & thời gian")]
        [Tooltip("Thời gian chơi (giây). Giảm dần qua các level để tăng độ khó.")]
        public float timeLimit = 60f;

        [Tooltip("Điểm cần đạt để qua màn. Tăng dần qua các level.")]
        public int targetScore = 500;

        [Header("Cấu hình vật phẩm")]
        [Tooltip("Danh sách loại vật phẩm + trọng số random cho level này. " +
                 "Level dễ: trọng số vàng lớn/kim cương cao. Level khó: tăng trọng số Rock, giảm giá trị.")]
        public ItemConfig[] itemConfigs;

        [Tooltip("Số lượng vật phẩm sinh ra trên màn chơi")]
        public int itemCount = 20;

        [Header("Bố trí không gian")]
        [Tooltip("Vùng X (trái-phải) để random vị trí vật phẩm")]
        public Vector2 spawnAreaX = new Vector2(-4f, 4f);

        [Tooltip("Vùng Y (nông-sâu) để random vị trí vật phẩm, Y càng âm càng sâu")]
        public Vector2 spawnAreaY = new Vector2(-2f, -10f);

        [Tooltip("Khoảng cách tối thiểu giữa 2 vật phẩm để tránh chồng lấn — tăng lên ở level khó " +
                 "để buộc người chơi nhắm chính xác hơn / tốn thời gian di chuyển móc hơn")]
        public float minItemSpacing = 1f;

        /// <summary>Random chọn 1 ItemConfig theo trọng số spawnWeight (roulette wheel selection).</summary>
        public ItemConfig GetRandomItemConfig()
        {
            float totalWeight = 0f;
            foreach (var cfg in itemConfigs) totalWeight += cfg.spawnWeight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var cfg in itemConfigs)
            {
                cumulative += cfg.spawnWeight;
                if (roll <= cumulative) return cfg;
            }
            return itemConfigs[itemConfigs.Length - 1]; // fallback an toàn
        }
    }
}
