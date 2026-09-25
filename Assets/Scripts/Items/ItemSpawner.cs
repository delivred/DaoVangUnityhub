using System.Collections.Generic;
using UnityEngine;
using DaoVang.Data;
using DaoVang.Utils;

namespace DaoVang.Items
{
    /// <summary>
    /// Sinh toàn bộ vật phẩm cho 1 màn chơi dựa trên LevelData:
    /// - Tái sử dụng GameObject qua ObjectPool để loại bỏ Garbage Collection lag.
    /// - Tái sử dụng bộ nhớ đệm _placedPositions để tránh cấp phát bộ nhớ động mỗi ván.
    /// - Tự động tính toán tầm nhìn Main Camera để rải vật phẩm đều khắp màn hình.
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private ObjectPool pool;

        private readonly List<GameObject> _activeItems = new();
        private readonly List<Vector2> _placedPositions = new(64);

        private void Awake()
        {
            if (pool == null)
            {
#if UNITY_2023_1_OR_NEWER
                pool = GetComponent<ObjectPool>() ?? FindAnyObjectByType<ObjectPool>() ?? gameObject.AddComponent<ObjectPool>();
#else
                pool = GetComponent<ObjectPool>() ?? FindObjectOfType<ObjectPool>() ?? gameObject.AddComponent<ObjectPool>();
#endif
            }
        }

        /// <summary>Dọn hết vật phẩm cũ và sinh mới theo cấu hình LevelData.</summary>
        public void SpawnForLevel(LevelData level)
        {
            if (level == null) return;
            if (pool == null) Awake();

            ClearAll();
            _placedPositions.Clear();

            int maxAttemptsPerItem = 25;
            GetSpawnBounds(level, out float minX, out float maxX, out float minY, out float maxY);

            int count = level.itemCount;
            for (int i = 0; i < count; i++)
            {
                ItemConfig config = level.GetRandomItemConfig();
                if (config.prefab == null) continue;

                Vector2 position = FindValidPosition(i, count, minX, maxX, minY, maxY, _placedPositions, level.minItemSpacing, maxAttemptsPerItem);
                _placedPositions.Add(position);

                GameObject instance = pool.Get(config.prefab, position, Quaternion.identity, transform);
                if (instance != null)
                {
                    ItemBase item = instance.GetComponent<ItemBase>();
                    if (item != null)
                    {
                        item.ResetForReuse();
                        item.Setup(config);
                    }
                    _activeItems.Add(instance);
                }
            }
        }

        /// <summary>
        /// Tính toán biên độ vùng spawn dựa trên LevelData kết hợp với khung nhìn Main Camera.
        /// </summary>
        private void GetSpawnBounds(LevelData level, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = level.spawnAreaX.x;
            maxX = level.spawnAreaX.y;
            minY = Mathf.Min(level.spawnAreaY.x, level.spawnAreaY.y);
            maxY = Mathf.Max(level.spawnAreaY.x, level.spawnAreaY.y);

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                float camHalfHeight = cam.orthographicSize;
                float camHalfWidth = camHalfHeight * cam.aspect;
                float camCenterY = cam.transform.position.y;

                // Rải vật phẩm ra khắp chiều ngang camera (chừa lề an toàn 1.4 unit)
                float camMinX = -camHalfWidth + 1.4f;
                float camMaxX = camHalfWidth - 1.4f;

                // Chiều sâu bao quát từ đáy camera (+ 1.0 unit) lên dưới sàn giàn khoan (+0.5 unit)
                float camMinY = camCenterY - camHalfHeight + 1.0f;
                float camMaxY = 0.5f;

                minX = Mathf.Min(minX, camMinX);
                maxX = Mathf.Max(maxX, camMaxX);
                minY = Mathf.Min(minY, camMinY);
                maxY = Mathf.Max(maxY, camMaxY);
            }
        }

        /// <summary>
        /// Tìm vị trí hợp lệ phân bổ đều trên toàn bộ bề ngang và chiều sâu của Camera.
        /// </summary>
        private Vector2 FindValidPosition(int itemIndex, int totalItems, float minX, float maxX, float minY, float maxY, List<Vector2> placed, float minSpacing, int maxAttempts)
        {
            float totalWidth = maxX - minX;
            int sectors = 5;
            int sectorIdx = itemIndex % sectors;
            float sectorWidth = totalWidth / sectors;
            float secMinX = minX + sectorIdx * sectorWidth;
            float secMaxX = secMinX + sectorWidth;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float candX = attempt < 12 ? Random.Range(secMinX, secMaxX) : Random.Range(minX, maxX);
                float candY = Random.Range(minY, maxY);
                Vector2 candidate = new Vector2(candX, candY);

                bool tooClose = false;
                for (int p = 0; p < placed.Count; p++)
                {
                    if (Vector2.Distance(placed[p], candidate) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return candidate;
            }

            return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        }

        public void ClearAll()
        {
            if (pool != null)
            {
                for (int i = 0; i < _activeItems.Count; i++)
                {
                    if (_activeItems[i] != null) pool.Release(_activeItems[i]);
                }
            }
            _activeItems.Clear();
        }
    }
}
