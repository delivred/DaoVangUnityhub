using System;
using UnityEngine;
using DaoVang.Level;
using DaoVang.Data;

namespace DaoVang.Core
{
    /// <summary>
    /// GameManager - Singleton điều phối toàn bộ flow game:
    /// Luồng: Menu -> (bấm CHƠI) -> LevelSelect (8 Màn chơi thử thách) -> Playing -> Win/Lose -> Shop/Menu.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Cấu hình level")]
        [SerializeField] private LevelData[] levels;
        [Tooltip("Nếu bật, bỏ qua luật tuần tự để test mọi màn. Mặc định TẮT để người chơi phải vượt ải tuần tự: Win màn 1 mới mở màn 2.")]
        [SerializeField] private bool unlockAllLevels = false;

        [Header("Tham chiếu module")]
        [SerializeField] private LevelManager levelManager;

        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int CurrentLevelIndex { get; private set; } = 0;
        public int CurrentScore { get; private set; } = 0;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnScoreChanged;

        public string LastGameOverReason { get; private set; }
        public int LastEarnedTokens { get; private set; }
        public int LastConsolationTokens { get; private set; }

        /// <summary>Level cao nhất người chơi đã mở khóa (0 = Level1, 7 = Level8)</summary>
        public int UnlockedLevelIndex { get; private set; }
        public bool UnlockAllLevels => unlockAllLevels;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (levelManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                levelManager = FindAnyObjectByType<LevelManager>();
#else
                levelManager = FindObjectOfType<LevelManager>();
#endif
            }

            int count = LevelCount;
            UnlockedLevelIndex = Mathf.Clamp(SaveSystem.LoadUnlockedLevel(), 0, Mathf.Max(0, count - 1));
            CurrentLevelIndex = UnlockedLevelIndex;
        }

        private void Start()
        {
            SetState(GameState.Menu);
        }

        // ------------------------------------------------------------------
        // API công khai gọi từ UI
        // ------------------------------------------------------------------

        public void RefreshProgressAfterLogin()
        {
            int count = LevelCount;
            UnlockedLevelIndex = Mathf.Clamp(SaveSystem.LoadUnlockedLevel(), 0, Mathf.Max(0, count - 1));
            CurrentLevelIndex = UnlockedLevelIndex;
        }

        public void GoToLevelSelect()
        {
            SetState(GameState.LevelSelect);
        }

        public void SelectLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length) return;
            if (!IsLevelUnlocked(index))
            {
                Debug.LogWarning($"[GameManager] Man {index + 1} dang bi KHOA! Can vuot qua Man {index} truoc do.");
                return;
            }

            CurrentLevelIndex = index;
            CurrentScore = 0;
            SetState(GameState.Playing);
            if (levelManager != null)
            {
                levelManager.LoadLevel(levels[CurrentLevelIndex]);
            }
        }

        public void RestartLevel()
        {
            CurrentScore = 0;
            SetState(GameState.Playing);
            if (levelManager != null && levels != null && CurrentLevelIndex < levels.Length)
            {
                levelManager.LoadLevel(levels[CurrentLevelIndex]);
            }
        }

        public void GoToNextLevel()
        {
            int maxIdx = LevelCount - 1;
            CurrentLevelIndex = Mathf.Min(CurrentLevelIndex + 1, maxIdx);
            SaveSystem.SaveUnlockedLevel(CurrentLevelIndex);
            UnlockedLevelIndex = Mathf.Max(UnlockedLevelIndex, CurrentLevelIndex);
            CurrentScore = 0;
            SetState(GameState.Playing);
            if (levelManager != null && levels != null && CurrentLevelIndex < levels.Length)
            {
                levelManager.LoadLevel(levels[CurrentLevelIndex]);
            }
        }

        public void ReturnToMenu()
        {
            SetState(GameState.Menu);
        }

        public void ReturnToLevelSelect()
        {
            SetState(GameState.LevelSelect);
        }

        // ------------------------------------------------------------------
        // Quản lý Điểm Số & Cửa Hàng
        // ------------------------------------------------------------------

        public void AddScore(int amount)
        {
            CurrentScore += amount;
            OnScoreChanged?.Invoke(CurrentScore);

            if (levelManager != null && levelManager.IsTargetReached(CurrentScore))
            {
                CompleteLevel();
            }
        }

        private GameState _previousStateBeforeShop = GameState.Menu;

        public void OpenShop()
        {
            _previousStateBeforeShop = CurrentState;
            SetState(GameState.Shop);
        }

        public void CloseShop()
        {
            if (_previousStateBeforeShop == GameState.Playing || _previousStateBeforeShop == GameState.Shop)
            {
                SetState(GameState.Menu);
            }
            else
            {
                SetState(_previousStateBeforeShop);
            }
        }

        public void CompleteLevel()
        {
            if (CurrentState != GameState.Playing) return;
            if (levelManager != null) levelManager.StopLevel();

            // Đánh dấu đã chiến thắng vượt ải màn hiện tại và lưu mở khóa màn kế tiếp
            SaveSystem.MarkLevelWon(CurrentLevelIndex);

            bool hasNextLevel = CurrentLevelIndex < LevelCount - 1;
            if (hasNextLevel)
            {
                SaveSystem.SaveUnlockedLevel(CurrentLevelIndex + 1);
                UnlockedLevelIndex = Mathf.Max(UnlockedLevelIndex, CurrentLevelIndex + 1);
            }

            // Tích lũy TOKEN từ điểm số, tiền thưởng vượt chỉ tiêu và thưởng hoàn thành màn
            int target = levelManager != null ? levelManager.TargetScore : 0;
            int bonus = Mathf.Max(0, CurrentScore - target);
            int levelClearReward = GetLevelReward(CurrentLevelIndex);
            LastEarnedTokens = CurrentScore + bonus + levelClearReward;
            SaveSystem.AddTokens(LastEarnedTokens);

            SaveSystem.SaveHighScore(CurrentScore);
            SetState(GameState.LevelComplete);
        }

        public void TriggerGameOver(string reason)
        {
            if (CurrentState != GameState.Playing) return;
            if (levelManager != null) levelManager.StopLevel();
            LastGameOverReason = reason;

            // Thưởng an ủi TOKEN cho người chơi để nâng cấp buff
            LastConsolationTokens = Mathf.Max(40, CurrentScore / 2);
            SaveSystem.AddTokens(LastConsolationTokens);

            SaveSystem.SaveHighScore(CurrentScore);
            SetState(GameState.GameOver);
        }

        public bool HasNextLevel => CurrentLevelIndex < LevelCount - 1;

        public bool IsLevelUnlocked(int index)
        {
            if (index <= 0) return true; // Màn 1 luôn luôn mở khóa
            return index <= UnlockedLevelIndex;
        }

        public bool IsLevelWon(int index) => SaveSystem.IsLevelWon(index);
        public int TotalLevelsWon => SaveSystem.GetTotalLevelsWon(LevelCount);

        public int LevelCount => levels != null ? levels.Length : 0;
        public LevelData GetLevel(int index) => (levels != null && index >= 0 && index < levels.Length) ? levels[index] : null;

        public void SetLevels(LevelData[] newLevels)
        {
            levels = newLevels;
        }

        public string GetLevelTitle(int index)
        {
            switch (index)
            {
                case 0: return "MAN 1: KHOI DONG";
                case 1: return "MAN 2: MO VANG SAU";
                case 2: return "MAN 3: BAI MIN CO DAI";
                case 3: return "MAN 4: HANG KIM CUONG";
                case 4: return "MAN 5: THUNG LUNG DA";
                case 5: return "MAN 6: KHO BAU HOANG GIA";
                case 6: return "MAN 7: SA MAC TU THAN";
                case 7: return "MAN 8: DINH CAO HUYEN THOAI";
                default: return $"MAN {index + 1}";
            }
        }

        public int GetLevelReward(int index)
        {
            switch (index)
            {
                case 0: return 150;
                case 1: return 250;
                case 2: return 400;
                case 3: return 550;
                case 4: return 700;
                case 5: return 900;
                case 6: return 1100;
                case 7: return 1500;
                default: return 200 + index * 150;
            }
        }

        public string GetLevelBombRate(int index)
        {
            switch (index)
            {
                case 0: return "15%";
                case 1: return "15%";
                case 2: return "25%";
                case 3: return "20%";
                case 4: return "15%";
                case 5: return "20%";
                case 6: return "30%";
                case 7: return "25%";
                default: return "20%";
            }
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
