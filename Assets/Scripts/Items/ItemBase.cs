using UnityEngine;

namespace DaoVang.Items
{
    /// <summary>
    /// Gắn trên mọi prefab vật phẩm (vàng nhỏ, vàng lớn, đá, kim cương).
    /// Yêu cầu prefab có: Collider2D (isTrigger = true, layer "Item"),
    /// Rigidbody2D (Body Type = Kinematic — vật phẩm không rơi tự do, chỉ đứng yên
    /// chờ móc, việc di chuyển hoàn toàn do HookController điều khiển).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemBase : MonoBehaviour
    {
        [SerializeField] private ItemType type;
        [SerializeField] private int scoreValue;
        [SerializeField] private float mass = 1f;
        [SerializeField] private float retractSpeedModifier = 1f;

        public ItemType Type => type;
        public int ScoreValue => scoreValue;
        public float Mass => mass;
        public float RetractSpeedModifier => retractSpeedModifier;
        public bool IsCaught { get; private set; }

        /// <summary>Gọi từ ItemSpawner ngay sau Instantiate để nạp thông số theo ItemConfig.</summary>
        public void Setup(ItemConfig config)
        {
            type = config.type;
            scoreValue = config.scoreValue;
            mass = config.mass;
            retractSpeedModifier = config.retractSpeedModifier;
        }

        /// <summary>HookController gọi khi móc chạm trúng vật phẩm này.</summary>
        public void OnCaught(Transform hookHead)
        {
            IsCaught = true;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Parent vào đầu móc để vật phẩm "di chuyển theo móc" tự động theo Transform cha.
            transform.SetParent(hookHead);
            transform.localPosition = Vector3.zero;
        }

        /// <summary>HookController gọi khi vật phẩm đã về tới đích và được cộng điểm.</summary>
        public void OnCollected()
        {
            // Trả về pool thay vì Destroy để tối ưu hiệu năng (xem Utils/ObjectPool.cs).
            transform.SetParent(null);
            gameObject.SetActive(false);
        }

        /// <summary>Reset trạng thái khi lấy lại từ pool để spawn ở vị trí mới.</summary>
        public void ResetForReuse()
        {
            IsCaught = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
            transform.SetParent(null);
        }
    }
}
