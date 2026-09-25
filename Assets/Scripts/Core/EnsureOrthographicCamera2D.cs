using UnityEngine;

namespace DaoVang.Core
{
    /// <summary>
    /// Gắn script này lên Main Camera. Nó tự động ép Camera về chế độ Orthographic
    /// (đúng chuẩn game 2D) ngay khi chạy, phòng trường hợp project bị tạo/nhầm
    /// sang Template 3D khiến Camera mặc định là Perspective — nguyên nhân phổ biến
    /// nhất gây "vật phẩm lệch vị trí", "nhìn méo", "chạm không trúng vật".
    ///
    /// Không thay thế được việc setup đúng project 2D từ đầu (xem README mục 1),
    /// nhưng đảm bảo camera luôn đúng hướng nhìn dù ai đó lỡ đổi nhầm trong Inspector.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class EnsureOrthographicCamera2D : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 6f;

        private void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;

            if (cam.orthographicSize <= 0f)
            {
                Debug.LogWarning("[EnsureOrthographicCamera2D] orthographicSize <= 0, đã tự chỉnh về 6. " +
                                  "Kiểm tra lại giá trị trong Inspector nếu khung hình vẫn sai.");
                cam.orthographicSize = 6f;
            }

            if (GetComponent<GameViewAdapter>() == null)
            {
                gameObject.AddComponent<GameViewAdapter>();
            }
        }
    }
}
