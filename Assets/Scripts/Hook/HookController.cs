using UnityEngine;
using DaoVang.Core;
using DaoVang.Items;

namespace DaoVang.Hook
{
    /// <summary>
    /// Điều khiển toàn bộ vòng đời của móc câu:
    ///   Swinging (đu qua lại) --chạm màn hình--> Extending (duỗi xuống)
    ///   Extending --chạm vật phẩm--> RetractingWithItem (kéo item về)
    ///   Extending --chạm giới hạn màn chơi--> Retracting (thu về tay không)
    ///   Retracting/RetractingWithItem --về tới gốc--> Swinging
    ///
    /// Setup trong Scene:
    ///   - GameObject "HookPivot" đặt tại vị trí nhân vật, HookController gắn ở đây.
    ///   - Con của HookPivot là "HookVisual" (sprite dây + móc), được kéo dài bằng
    ///     cách scale theo trục Y hoặc dùng LineRenderer nối pivot -> hookHead.
    ///   - "HookHead" là GameObject con có Collider2D (isTrigger = true) và
    ///     Rigidbody2D (Body Type = Kinematic) để bắt sự kiện OnTriggerEnter2D
    ///     mà không bị vật lý World tác động.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class HookController : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField] private Transform hookHead;      // đầu móc, nơi đặt Collider2D trigger
        [SerializeField] private Transform hookOrigin;    // vị trí gốc (vị trí nhân vật đứng)
        [SerializeField] private LineRenderer ropeRenderer; // dây câu nối origin -> hookHead (tuỳ chọn)

        [Header("Thông số đu qua lại")]
        [SerializeField] private float swingAngleRange = 60f; // biên độ góc đu (độ), tính từ trục thẳng đứng
        [SerializeField] private float swingSpeed = 1.2f;      // tốc độ đu

        [Header("Thông số duỗi/thu")]
        [SerializeField] private float extendSpeed = 9f;
        [SerializeField] private float baseRetractSpeed = 9f;   // tốc độ thu khi tay không hoặc item nhẹ nhất
        [SerializeField] private float maxRopeLength = 12f;      // giới hạn màn chơi theo chiều dọc

        [Header("Layer vật phẩm")]
        [SerializeField] private LayerMask itemLayerMask;

        [Header("Player animator & character")]
        [SerializeField] private Animator playerAnimator; // để trigger animation "Pull" khi kéo vật nặng
        [SerializeField] private DaoVang.Player.PlayerCharacter playerCharacter;

        [Header("Hình ảnh móc & dây")]
        [SerializeField] private Sprite hookHeadSprite;
        [SerializeField] private Sprite explosionSprite;
        [SerializeField] private Sprite winchMachineSprite;
        [SerializeField] private Color ropeColor = new Color(1f, 0.85f, 0.2f, 1f); // Dây màu vàng kim nổi bật
        [SerializeField] private float ropeWidth = 0.09f;

        private HookState _state = HookState.Swinging;
        private float _swingTime;
        private ItemBase _caughtItem;
        private AudioSource _audioSource;
        private GameObject _winchObj;

        public HookState State => _state;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (playerCharacter == null)
            {
#if UNITY_2023_1_OR_NEWER
                playerCharacter = GetComponentInParent<DaoVang.Player.PlayerCharacter>() ?? FindAnyObjectByType<DaoVang.Player.PlayerCharacter>();
#else
                playerCharacter = GetComponentInParent<DaoVang.Player.PlayerCharacter>() ?? FindObjectOfType<DaoVang.Player.PlayerCharacter>();
#endif
            }

            if (itemLayerMask.value == 0)
            {
                itemLayerMask = 1 << 3; // Fallback an toàn vào Layer 3 (Items)
            }
            SetupRopeAndHookVisuals();
        }

        private void SetupRopeAndHookVisuals()
        {
            // 1. Tự động kiểm tra và cấu hình LineRenderer cho dây câu
            if (ropeRenderer == null)
            {
                ropeRenderer = GetComponent<LineRenderer>();
                if (ropeRenderer == null)
                {
                    ropeRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (ropeRenderer != null)
            {
                ropeRenderer.useWorldSpace = true;
                ropeRenderer.positionCount = 2;
                ropeRenderer.startWidth = ropeWidth;
                ropeRenderer.endWidth = ropeWidth;
                ropeRenderer.sortingOrder = 10; // Nằm trên Background

                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipelines/2D/Sprite-Lit-Default") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    ropeRenderer.material = new Material(shader);
                }
                ropeRenderer.startColor = ropeColor;
                ropeRenderer.endColor = ropeColor;
            }

            // 2. Tự động kiểm tra và cấu hình SpriteRenderer cho đầu móc (HookHead)
            if (hookHead != null)
            {
                var headSr = hookHead.GetComponent<SpriteRenderer>();
                if (headSr == null)
                {
                    headSr = hookHead.gameObject.AddComponent<SpriteRenderer>();
                }

                if (headSr != null)
                {
                    if (headSr.sprite == null && hookHeadSprite != null)
                    {
                        headSr.sprite = hookHeadSprite;
                    }

                    headSr.sortingOrder = 12; // Nằm trên cả dây câu và background
                    headSr.color = Color.white;

                    Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipelines/2D/Sprite-Lit-Default");
                    if (shader != null && headSr.sharedMaterial == null)
                    {
                        headSr.material = new Material(shader);
                    }
                }

                // Căn chỉnh scale đầu móc to rõ, dễ nhìn
                hookHead.localScale = new Vector3(1.35f, 1.35f, 1f);
            }

            // 3. Tự động kiểm tra và cấu hình Máy Kéo (Winch Machine)
            if (_winchObj == null)
            {
                Transform existingWinch = transform.Find("WinchMachine") ?? (playerCharacter != null ? playerCharacter.transform.Find("WinchMachine") : null);
                if (existingWinch != null)
                {
                    _winchObj = existingWinch.gameObject;
                }
                else
                {
                    _winchObj = new GameObject("WinchMachine");
                    Transform parentTransform = playerCharacter != null ? playerCharacter.transform : transform.parent;
                    _winchObj.transform.SetParent(parentTransform, false);
                    _winchObj.transform.localPosition = transform.localPosition + new Vector3(0f, 0.18f, 0f);
                    _winchObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                    var sr = _winchObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 9; // Nằm ngay sau đầu móc và dây, nổi bật trên nền
                    if (winchMachineSprite != null)
                    {
                        sr.sprite = winchMachineSprite;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
            ApplyEquippedSkin();
            ResetHook();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                ResetHook();
            }
            else
            {
                _state = HookState.Swinging;
                _caughtItem = null;
                if (hookHead != null && hookOrigin != null)
                {
                    hookHead.position = hookOrigin.position;
                    hookHead.rotation = Quaternion.identity;
                }
                if (hookOrigin != null) hookOrigin.rotation = Quaternion.identity;
                DrawRope();
            }
        }

        /// <summary>
        /// Khởi tạo và reset lại móc câu về vị trí ban đầu (dùng khi bắt đầu ván mới hoặc đổi trạng thái)
        /// </summary>
        public void ResetHook()
        {
            _state = HookState.Swinging;
            _swingTime = 0f;

            if (_caughtItem != null)
            {
                _caughtItem.OnCollected();
                _caughtItem = null;
            }

            if (hookHead != null)
            {
                for (int i = hookHead.childCount - 1; i >= 0; i--)
                {
                    Transform child = hookHead.GetChild(i);
                    child.SetParent(null);
                    child.gameObject.SetActive(false);
                }

                if (hookOrigin != null)
                {
                    hookHead.position = hookOrigin.position;
                    hookHead.rotation = hookOrigin.rotation;
                }
            }

            if (hookOrigin != null)
            {
                hookOrigin.rotation = Quaternion.identity;
            }

            DrawRope();
            if (playerCharacter != null) playerCharacter.PlayIdle();
            else if (playerAnimator != null) playerAnimator.SetTrigger("Idle");
        }

        public void ApplyEquippedSkin()
        {
            var skin = DaoVang.Shop.ShopManager.Instance != null 
                ? DaoVang.Shop.ShopManager.Instance.GetEquippedSkinData() 
                : null;
            if (skin == null) return;

            // Đổi màu dây câu theo Skin
            if (ropeRenderer != null)
            {
                ropeRenderer.startColor = skin.ropeColor;
                ropeRenderer.endColor = skin.ropeColor;
            }

            // Đổi màu sắc đầu móc câu theo Skin
            if (hookHead != null)
            {
                var headSr = hookHead.GetComponent<SpriteRenderer>();
                if (headSr != null)
                {
                    headSr.color = skin.headColor;
                }
            }
        }

        private void Update()
        {
            // Chỉ hoạt động khi game đang trong trạng thái chơi (Playing)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (hookHead != null && hookOrigin != null)
                {
                    hookHead.position = hookOrigin.position;
                    hookOrigin.rotation = Quaternion.identity;
                }
                DrawRope();
                return;
            }

            switch (_state)
            {
                case HookState.Swinging:
                    UpdateSwing();
                    CheckTouchInput();
                    break;
                case HookState.Extending:
                    UpdateExtend();
                    break;
                case HookState.Retracting:
                case HookState.RetractingWithItem:
                    UpdateRetract();
                    break;
            }

            DrawRope();
        }

        // ------------------------------------------------------------------
        // 1. SWINGING - móc tự động đu qua lại trái/phải
        // ------------------------------------------------------------------
        private void UpdateSwing()
        {
            _swingTime += Time.deltaTime * swingSpeed;
            // Dùng Sin để đu qua lại mượt, biên độ +-swingAngleRange quanh trục thẳng đứng (0 độ = thẳng xuống).
            float angle = Mathf.Sin(_swingTime) * swingAngleRange;
            hookOrigin.rotation = Quaternion.Euler(0f, 0f, angle);
            hookHead.position = hookOrigin.position; // khi đang đu, đầu móc luôn sát pivot
            hookHead.rotation = hookOrigin.rotation; // Đầu móc xoay theo hướng dây
            if (_winchObj != null) _winchObj.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Người chơi chạm màn hình để thả móc.
        /// Hỗ trợ cả Touch (mobile) và chuột trái (test trong Editor).
        /// </summary>
        private void CheckTouchInput()
        {
            // Không nhận input thả móc nếu người chơi vừa bấm trúng nút bấm UI
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            }

            bool tapped = false;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                tapped = true;
            }
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
            }
#endif
            if (tapped)
            {
                FireHook();
            }
        }

        private void FireHook()
        {
            _state = HookState.Extending;
            DaoVang.Audio.AudioManager.Instance?.PlayShoot();
        }

        // ------------------------------------------------------------------
        // 2. EXTENDING - móc duỗi xuống theo góc hiện tại cho đến khi chạm vật/giới hạn
        // ------------------------------------------------------------------
        private void UpdateExtend()
        {
            // Hướng duỗi = hướng "up" cục bộ của hookOrigin (đã bị xoay theo góc đu lúc thả).
            Vector3 direction = hookOrigin.up * -1f; // up của pivot trỏ lên trên nên lấy chiều ngược lại để đi xuống
            float cableMod = DaoVang.Shop.ShopManager.Instance != null ? DaoVang.Shop.ShopManager.Instance.GetExtendSpeedMultiplier() : 1f;
            float moveDist = (extendSpeed * cableMod) * Time.deltaTime;
            Vector3 prevPos = hookHead.position;
            Vector3 targetPos = prevPos + direction * moveDist;

            // Swept continuous collision detection - loại bỏ hoàn toàn lỗi tunneling xuyên thấu item ở mọi tốc độ
            float castRadius = 0.28f;
            RaycastHit2D[] hits = Physics2D.CircleCastAll(prevPos, castRadius, direction, moveDist, itemLayerMask);

            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            bool itemCaught = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                ItemBase item = hit.collider.GetComponent<ItemBase>();
                if (item == null || item.IsCaught || !item.gameObject.activeInHierarchy) continue;

                if (item.Type == ItemType.Bomb && DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.EquippedSkinAvoidsBombs())
                {
                    // Móc Hỏa Long (VIP Mythic): Né bom hoàn toàn, vô hiệu hoá bom mà không dừng cú phóng móc!
                    EvadeBomb(item);
                    continue;
                }

                // Gắp trúng vật phẩm hợp lệ đầu tiên trên đường phóng
                hookHead.position = prevPos + direction * Mathf.Max(0f, hit.distance);
                CatchItem(item);
                itemCaught = true;
                break;
            }

            if (!itemCaught)
            {
                hookHead.position = targetPos;
                hookHead.rotation = hookOrigin.rotation;

                // Kiểm tra va chạm bổ sung tại vị trí mới phòng trường hợp collider nằm ngay điểm xuất phát
                Collider2D overlap = Physics2D.OverlapCircle(hookHead.position, castRadius, itemLayerMask);
                if (overlap != null)
                {
                    ItemBase item = overlap.GetComponent<ItemBase>();
                    if (item != null && !item.IsCaught && item.gameObject.activeInHierarchy)
                    {
                        if (item.Type == ItemType.Bomb && DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.EquippedSkinAvoidsBombs())
                        {
                            EvadeBomb(item);
                        }
                        else
                        {
                            CatchItem(item);
                            itemCaught = true;
                        }
                    }
                }
            }

            if (_winchObj != null) _winchObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 50f) * 2.5f);

            float distanceFromOrigin = Vector3.Distance(hookHead.position, hookOrigin.position);
            float effectiveMaxLength = Mathf.Max(maxRopeLength, 20f);
            if (!itemCaught && distanceFromOrigin >= effectiveMaxLength)
            {
                _state = HookState.Retracting;
            }
        }

        private void CatchItem(ItemBase item)
        {
            if (item.Type == ItemType.Bomb)
            {
                // Móc cuối (Móc Hỏa Long VIP Mythic) né bom hoàn toàn
                if (DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.EquippedSkinAvoidsBombs())
                {
                    EvadeBomb(item);
                    _state = HookState.Retracting;
                    return;
                }

                // Kiểm tra Khiên Gỡ Bom từ Shop: nếu có thì vô hiệu hóa an toàn, không nổ
                if (DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.ConsumeBombShield())
                {
                    DefuseBomb(item);
                    return;
                }

                ExplodeBomb(item);
                return;
            }

            item.OnCaught(hookHead);
            _caughtItem = item;
            _state = HookState.RetractingWithItem;

            if (playerCharacter != null)
            {
                playerCharacter.PlayPull(item.Mass);
            }
            else if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Pull");
            }
            DaoVang.Audio.AudioManager.Instance?.PlayCatch();
        }

        private void EvadeBomb(ItemBase bomb)
        {
            if (bomb != null)
            {
                Vector3 pos = bomb.transform.position;
                bomb.gameObject.SetActive(false);
                DaoVang.UI.ScoreEffectManager.Instance?.ShowCustomPopup(pos, "★ NE BOM!", new Color(0.2f, 1f, 0.45f, 1f));
            }
            DaoVang.Audio.AudioManager.Instance?.PlayCatch();
        }

        private void DefuseBomb(ItemBase bomb)
        {
            if (bomb != null)
            {
                Vector3 pos = bomb.transform.position;
                bomb.gameObject.SetActive(false);
                DaoVang.UI.ScoreEffectManager.Instance?.ShowScorePopup(pos, 0, new Color(0.2f, 0.95f, 0.45f, 1f));
            }
            DaoVang.Audio.AudioManager.Instance?.PlayCollect();
            _state = HookState.Retracting;
            if (playerCharacter != null) playerCharacter.PlayCheer();
        }

        private void ExplodeBomb(ItemBase bomb)
        {
            StartCoroutine(BombExplosionRoutine(bomb));
        }

        private System.Collections.IEnumerator BombExplosionRoutine(ItemBase bomb)
        {
            Vector3 explosionPos = bomb != null ? bomb.transform.position : hookHead.position;

            // Ngay khi móc chạm vào bom: bom biến mất và phát nổ
            if (bomb != null)
            {
                bomb.gameObject.SetActive(false);
            }

            // Âm thanh bom nổ uy lực
            DaoVang.Audio.AudioManager.Instance?.PlayBomb();
            if (playerCharacter != null) playerCharacter.PlayIdle();

            // Tạo hiệu ứng vụ nổ đẹp mắt
            CreateExplosionVisual(explosionPos);

            // Rung lắc camera tạo cảm giác nổ mạnh
            Camera cam = Camera.main;
            if (cam != null)
            {
                StartCoroutine(CameraShakeRoutine(cam.transform, 0.45f, 0.35f));
            }

            // Thu móc về vị trí gốc ngay
            hookHead.position = hookOrigin.position;
            hookOrigin.rotation = Quaternion.identity;
            hookHead.rotation = Quaternion.identity;
            _swingTime = 0f;
            _state = HookState.Swinging;

            // Chờ 0.45 giây để người chơi nhìn rõ vụ nổ trước khi hiện bảng Game Over
            yield return new WaitForSeconds(0.45f);

            GameManager.Instance.TriggerGameOver("BAN DA KEO PHAI BOM! NO TUNG!");
        }

        private void CreateExplosionVisual(Vector3 position)
        {
            GameObject explosionObj = new GameObject("ExplosionEffect");
            explosionObj.transform.position = position;
            SpriteRenderer sr = explosionObj.AddComponent<SpriteRenderer>();

            if (explosionSprite != null)
            {
                sr.sprite = explosionSprite;
            }
            else
            {
                // Fallback nếu chưa gắn sprite: tạo hình tròn màu cam vàng nổ
                Texture2D tex = new Texture2D(32, 32);
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                        tex.SetPixel(x, y, dist < 15 ? new Color(1f, 0.4f, 0f, 1f) : Color.clear);
                    }
                }
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            }

            sr.sortingOrder = 25;
            StartCoroutine(AnimateExplosion(explosionObj));
        }

        private System.Collections.IEnumerator AnimateExplosion(GameObject explosionObj)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.6f;
            Vector3 endScale = Vector3.one * 3.5f;
            SpriteRenderer sr = explosionObj != null ? explosionObj.GetComponent<SpriteRenderer>() : null;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (explosionObj != null)
                {
                    explosionObj.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI * 0.5f));
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = Mathf.Clamp01(1f - (t * t));
                        sr.color = c;
                    }
                }
                yield return null;
            }

            if (explosionObj != null)
            {
                Destroy(explosionObj);
            }
        }

        private System.Collections.IEnumerator CameraShakeRoutine(Transform camTransform, float duration, float magnitude)
        {
            Vector3 originalPos = camTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = (Random.value * 2f - 1f) * magnitude;
                float y = (Random.value * 2f - 1f) * magnitude;
                camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                yield return null;
            }

            camTransform.localPosition = originalPos;
        }

        // ------------------------------------------------------------------
        // 3. RETRACTING - móc thu về vị trí gốc, có hoặc không mang vật phẩm
        // ------------------------------------------------------------------
        private void UpdateRetract()
        {
            hookHead.rotation = hookOrigin.rotation;
            if (_winchObj != null) _winchObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 50f) * 3f);

            // Kiểm tra an toàn: Nếu đang ở trạng thái kéo item nhưng item đã bị mất/hủy, chuyển về Retracting
            if (_state == HookState.RetractingWithItem && (_caughtItem == null || !_caughtItem.gameObject.activeInHierarchy))
            {
                _state = HookState.Retracting;
                _caughtItem = null;
            }

            // Kiểm tra phím Space để kích nổ thuốc nổ phá đá/vật nặng đang kéo
            if (_state == HookState.RetractingWithItem && _caughtItem != null)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    UseDynamite();
                }
            }

            // Vật càng nặng (mass càng lớn) thì kéo càng chậm.
            // Áp dụng hệ số nâng cấp sức kéo từ Cửa Hàng (PullPower) & Skin Móc VIP
            float pullMod = DaoVang.Shop.ShopManager.Instance != null ? DaoVang.Shop.ShopManager.Instance.GetPullSpeedMultiplier() : 1f;
            float speed = baseRetractSpeed * pullMod;
            if (_state == HookState.RetractingWithItem && _caughtItem != null)
            {
                float mass = Mathf.Max(0.1f, _caughtItem.Mass);
                // Với Skin VIP có pullMod cao (>2.0), giảm bớt ảnh hưởng khối lượng để kéo cực nhanh và mượt mà
                if (pullMod > 2.0f) mass = Mathf.Sqrt(mass);
                speed = (baseRetractSpeed * pullMod) / mass * _caughtItem.RetractSpeedModifier;
            }

            float step = speed * Time.deltaTime;
            float dist = Vector3.Distance(hookHead.position, hookOrigin.position);

            if (dist <= step || dist < 0.08f)
            {
                hookHead.position = hookOrigin.position;
                CompleteRetract();
            }
            else
            {
                hookHead.position = Vector3.MoveTowards(hookHead.position, hookOrigin.position, step);
            }
        }

        /// <summary>
        /// Kích nổ 1 quả thuốc nổ để phá hủy ngay lập tức vật phẩm đang kéo
        /// </summary>
        public bool UseDynamite()
        {
            if (_state != HookState.RetractingWithItem || _caughtItem == null) return false;

            if (DaoVang.Shop.ShopManager.Instance != null && DaoVang.Shop.ShopManager.Instance.UseDynamite())
            {
                Vector3 blastPos = _caughtItem.transform.position;
                _caughtItem.OnCollected();
                _caughtItem = null;

                // Hiệu ứng nổ và âm thanh
                CreateExplosionVisual(blastPos);
                DaoVang.Audio.AudioManager.Instance?.PlayBomb();

                // Chuyển sang thu móc rỗng với tốc độ cao nhất
                _state = HookState.Retracting;
                if (playerCharacter != null) playerCharacter.PlayIdle();
                return true;
            }
            return false;
        }

        private void CompleteRetract()
        {
            if (_state == HookState.RetractingWithItem && _caughtItem != null)
            {
                int rawScore = _caughtItem.ScoreValue;
                ItemType type = _caughtItem.Type;

                // Áp dụng hệ số Móc May Mắn (Lucky Hook) từ Cửa Hàng
                float luckyMod = DaoVang.Shop.ShopManager.Instance != null ? DaoVang.Shop.ShopManager.Instance.GetScoreMultiplier() : 1f;
                int score = Mathf.RoundToInt(rawScore * luckyMod);

                GameManager.Instance.AddScore(score);
                _caughtItem.OnCollected();
                _caughtItem = null;

                // Âm thanh thu nhận vật phẩm (coin chime)
                DaoVang.Audio.AudioManager.Instance?.PlayCollect();

                // Hoạt ảnh ăn mừng nhảy nhót của thợ mỏ
                if (playerCharacter != null)
                {
                    playerCharacter.PlayCheer();
                }
                else if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("Idle");
                }

                // Hiệu ứng chữ số điểm bay lên và tia sáng lấp lánh
                Color popupColor = (type == ItemType.Diamond) ? new Color(0.35f, 0.92f, 1f, 1f) :
                                   (type == ItemType.Rock) ? new Color(0.85f, 0.85f, 0.85f, 1f) :
                                   new Color(1f, 0.85f, 0.15f, 1f);
                DaoVang.UI.ScoreEffectManager.Instance?.ShowScorePopup(hookOrigin.position, score, popupColor);
            }
            else
            {
                if (playerCharacter != null)
                {
                    playerCharacter.PlayIdle();
                }
                else if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("Idle");
                }
            }

            // Dọn dẹp triệt để bất kỳ item hay con nào còn bám trên đầu móc
            if (_caughtItem != null)
            {
                _caughtItem.OnCollected();
                _caughtItem = null;
            }

            if (hookHead != null)
            {
                for (int i = hookHead.childCount - 1; i >= 0; i--)
                {
                    Transform child = hookHead.GetChild(i);
                    child.SetParent(null);
                    child.gameObject.SetActive(false);
                }
                hookHead.position = hookOrigin.position;
                hookHead.rotation = Quaternion.identity;
            }

            hookOrigin.rotation = Quaternion.identity;
            _swingTime = 0f;
            _state = HookState.Swinging;
        }

        private void DrawRope()
        {
            if (ropeRenderer == null) return;
            ropeRenderer.SetPosition(0, hookOrigin.position);
            ropeRenderer.SetPosition(1, hookHead.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (hookHead != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hookHead.position, 0.15f);
            }
        }
    }
}
