using UnityEngine;
using UnityEngine.UI;

namespace DaoVang.Core
{
    /// <summary>
    /// Tự động điều chỉnh Camera, Background, và Nhân vật (Player)
    /// để game hiển thị tối ưu, sắc nét ở cả màn hình DỌC (Portrait) và NGANG (Landscape).
    /// </summary>
    [ExecuteAlways]
    public class GameViewAdapter : MonoBehaviour
    {
        [Header("Camera & Tầm nhìn")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Vị trí Y của Camera ở màn hình ngang (landscape) để bao quát trọn vẹn màn chơi")]
        [SerializeField] private float landscapeCenterY = -1.0f;
        [Tooltip("Vị trí Y của Camera ở màn hình dọc (portrait)")]
        [SerializeField] private float portraitCenterY = -0.6f;
        [Tooltip("Chiều rộng tối thiểu cần nhìn thấy trong gameplay (để thấy trọn khu vực đào vàng)")]
        [SerializeField] private float targetVisibleWidth = 9.2f;
        [Tooltip("Kích thước camera chuẩn ở màn hình ngang")]
        [SerializeField] private float landscapeOrthoSize = 5.8f;

        [Header("Tham chiếu đối tượng")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("Căn chỉnh vị trí nhân vật")]
        [SerializeField] private float playerTopPadding = 1.35f;

        private float _lastScreenWidth;
        private float _lastScreenHeight;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            AutoFindReferences();
            ApplyAdaptation();
        }

        private void Start()
        {
            AutoFindReferences();
            ApplyAdaptation();
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                ApplyAdaptation();
            }
        }

        private void AutoFindReferences()
        {
            if (targetCamera == null) targetCamera = Camera.main ?? GetComponent<Camera>();

            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            if (backgroundRenderer == null)
            {
                GameObject bgObj = GameObject.Find("Background");
                if (bgObj != null) backgroundRenderer = bgObj.GetComponent<SpriteRenderer>();
            }

            if (canvasScaler == null)
            {
#if UNITY_2023_1_OR_NEWER
                canvasScaler = Object.FindAnyObjectByType<CanvasScaler>();
#else
                canvasScaler = Object.FindObjectOfType<CanvasScaler>();
#endif
            }
        }

        /// <summary>
        /// Thực hiện căn chỉnh Camera, Background, Player và Canvas theo tỷ lệ màn hình hiện tại.
        /// </summary>
        public void ApplyAdaptation()
        {
            if (targetCamera == null) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            targetCamera.orthographic = true;

            float aspect = targetCamera.aspect;
            if (aspect <= 0f) aspect = 9f / 16f;

            bool isPortrait = aspect < 1.0f;

            // Đặt vị trí trung tâm Camera để nhìn trọn vẹn khu vực đào vàng
            float centerY = isPortrait ? portraitCenterY : landscapeCenterY;
            targetCamera.transform.position = new Vector3(0f, centerY, -10f);

            // 1. Tự động tính Orthographic Size để luôn nhìn thấy trọn vẹn khu vực chơi
            if (isPortrait)
            {
                // Ở màn hình dọc: tăng chiều cao camera để đảm bảo chiều ngang >= targetVisibleWidth
                float neededOrtho = (targetVisibleWidth / 2f) / aspect;
                targetCamera.orthographicSize = Mathf.Max(landscapeOrthoSize, neededOrtho);
            }
            else
            {
                // Ở màn hình ngang: dùng kích thước chuẩn
                targetCamera.orthographicSize = landscapeOrthoSize;
            }

            float orthoSize = targetCamera.orthographicSize;
            float camHeight = orthoSize * 2f;
            float camWidth = camHeight * aspect;

            // 2. Co giãn Background theo chế độ Cover (Aspect Fill) không bị viền đen
            if (backgroundRenderer != null && backgroundRenderer.sprite != null)
            {
                backgroundRenderer.transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 0f);
                backgroundRenderer.transform.rotation = Quaternion.identity;

                Vector2 spriteSize = backgroundRenderer.sprite.rect.size / backgroundRenderer.sprite.pixelsPerUnit;
                if (spriteSize.x > 0f && spriteSize.y > 0f)
                {
                    float scaleX = camWidth / spriteSize.x;
                    float scaleY = camHeight / spriteSize.y;
                    // Lấy tỷ lệ lớn hơn để luôn phủ kín khung nhìn ở cả dọc lẫn ngang
                    float coverScale = Mathf.Max(scaleX, scaleY) * 1.02f;
                    backgroundRenderer.transform.localScale = new Vector3(coverScale, coverScale, 1f);
                }
            }

            // 3. Đặt nhân vật (Player) đứng trực tiếp trên mặt đất / sàn giàn gỗ
            if (playerTransform != null)
            {
                if (backgroundRenderer != null && backgroundRenderer.sprite != null)
                {
                    // Mặt sàn gỗ nằm ở 68.06% từ đáy ảnh lên (345px từ đỉnh xuống trên ảnh 1080p)
                    float bgBottom = backgroundRenderer.bounds.min.y;
                    float bgHeight = backgroundRenderer.bounds.size.y;
                    float groundPlatformY = bgBottom + bgHeight * 0.6806f;

                    float halfPlayerHeight = 1.15f;
                    var playerSr = playerTransform.GetComponent<SpriteRenderer>();
                    if (playerSr != null && playerSr.sprite != null)
                    {
                        halfPlayerHeight = playerSr.bounds.extents.y;
                    }

                    playerTransform.position = new Vector3(0f, groundPlatformY + halfPlayerHeight, 0f);
                }
                else
                {
                    float playerY = targetCamera.transform.position.y + orthoSize - playerTopPadding;
                    playerTransform.position = new Vector3(0f, playerY, 0f);
                }
                playerTransform.rotation = Quaternion.identity; // Giữ góc thẳng đứng chuẩn 2D
            }

            // 4. Tự động chuyển CanvasScaler theo chiều màn hình
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = isPortrait ? new Vector2(720, 1280) : new Vector2(1280, 720);
                canvasScaler.matchWidthOrHeight = isPortrait ? 0f : 1f;
            }
        }
    }
}
