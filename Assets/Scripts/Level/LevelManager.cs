using System;
using UnityEngine;
using DaoVang.Core;
using DaoVang.Data;
using DaoVang.Items;

namespace DaoVang.Level
{
    /// <summary>
    /// Quản lý vòng đời 1 màn chơi: đếm ngược thời gian, theo dõi điểm mục tiêu,
    /// gọi ItemSpawner sinh vật phẩm, và báo cho GameManager khi thắng/thua.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private ItemSpawner itemSpawner;

        public float TimeLeft { get; private set; }
        public int TargetScore { get; private set; }
        public LevelData CurrentLevel { get; private set; }
        public bool HasEnergyDrinkActive { get; private set; }

        private bool _isRunning;

        /// <summary>Bắn mỗi frame khi đang chạy, PlayingUI subscribe để cập nhật đồng hồ đếm ngược.</summary>
        public event Action<float> OnTimeChanged;

        private void Update()
        {
            if (!_isRunning) return;

            TimeLeft -= Time.deltaTime;
            OnTimeChanged?.Invoke(Mathf.Max(0f, TimeLeft));

            if (TimeLeft <= 0f)
            {
                _isRunning = false;
                GameManager.Instance.TriggerGameOver("HET THOI GIAN!");
            }
        }

        /// <summary>Gọi từ GameManager mỗi khi bắt đầu 1 level mới.</summary>
        public void LoadLevel(LevelData level)
        {
            CurrentLevel = level;
            TargetScore = level.targetScore;

            // Kích hoạt Nước Tăng Lực nếu người chơi đã mua trong Shop (+15s)
            HasEnergyDrinkActive = DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.ConsumeEnergyDrink();
            TimeLeft = level.timeLimit + (HasEnergyDrinkActive ? 15f : 0f);

            _isRunning = true;

            itemSpawner.SpawnForLevel(level);

#if UNITY_2023_1_OR_NEWER
            var hook = FindAnyObjectByType<DaoVang.Hook.HookController>();
#else
            var hook = FindObjectOfType<DaoVang.Hook.HookController>();
#endif
            if (hook != null) hook.ResetHook();

            OnTimeChanged?.Invoke(TimeLeft);
        }

        public bool IsTargetReached(int currentScore) => currentScore >= TargetScore;

        /// <summary>Dừng đếm giờ — gọi khi level kết thúc (thắng hoặc thua) để tránh trừ giờ thừa.</summary>
        public void StopLevel()
        {
            _isRunning = false;
            HasEnergyDrinkActive = false;
        }
    }
}
