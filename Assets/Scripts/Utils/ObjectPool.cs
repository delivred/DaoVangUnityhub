using System.Collections.Generic;
using UnityEngine;

namespace DaoVang.Utils
{
    /// <summary>
    /// ObjectPool tối ưu hóa theo prefab:
    /// - Quản lý hàng đợi object tái sử dụng giữa các ván chơi.
    /// - Gom toàn bộ object nhàn rỗi vào thư mục con _poolRoot để giữ Scene Hierarchy ngăn nắp.
    /// - Cung cấp WarmUp để tạo trước đối tượng, loại bỏ hoàn toàn hiện tượng giật lag khi vào màn chơi.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
        private Transform _poolRoot;

        private void Awake()
        {
            EnsurePoolRoot();
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot == null)
            {
                Transform existing = transform.Find("_PoolContainer");
                if (existing != null)
                {
                    _poolRoot = existing;
                }
                else
                {
                    GameObject rootObj = new GameObject("_PoolContainer");
                    rootObj.transform.SetParent(transform, false);
                    _poolRoot = rootObj.transform;
                }
            }
        }

        /// <summary>Lấy 1 object từ pool (tạo mới nếu pool rỗng).</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            EnsurePoolRoot();

            if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }

            GameObject instance = null;

            // Lấy từ queue và loại bỏ bất kỳ instance nào đã bị destroy ngoài ý muốn
            while (queue.Count > 0)
            {
                instance = queue.Dequeue();
                if (instance != null) break;
            }

            if (instance != null)
            {
                instance.transform.SetParent(parent != null ? parent : transform);
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab, position, rotation, parent != null ? parent : transform);
                _instanceToPrefab[instance] = prefab;
            }

            return instance;
        }

        /// <summary>Trả object về pool thay vì Destroy, tự động đưa vào _poolRoot để giấu khỏi cảnh.</summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            EnsurePoolRoot();

            instance.SetActive(false);
            if (_poolRoot != null)
            {
                instance.transform.SetParent(_poolRoot);
            }

            if (_instanceToPrefab.TryGetValue(instance, out GameObject prefab) && prefab != null)
            {
                if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
                {
                    queue = new Queue<GameObject>();
                    _pools[prefab] = queue;
                }
                queue.Enqueue(instance);
            }
            else
            {
                // Object không được tạo qua pool này — fallback Destroy an toàn
                Destroy(instance);
            }
        }

        /// <summary>Khởi tạo sẵn một số lượng object để tránh giật khung hình lần đầu chơi.</summary>
        public void WarmUp(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            EnsurePoolRoot();

            for (int i = 0; i < count; i++)
            {
                GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, _poolRoot);
                instance.SetActive(false);
                _instanceToPrefab[instance] = prefab;

                if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
                {
                    queue = new Queue<GameObject>();
                    _pools[prefab] = queue;
                }
                queue.Enqueue(instance);
            }
        }

        /// <summary>Dọn toàn bộ pool, dùng khi chuyển level/scene.</summary>
        public void Clear()
        {
            foreach (var queue in _pools.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _pools.Clear();
            _instanceToPrefab.Clear();
        }
    }
}
