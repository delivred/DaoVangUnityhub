using System.Collections;
using UnityEngine;

namespace DaoVang.Player
{
    /// <summary>
    /// Điều khiển hoạt ảnh nhân vật thợ mỏ:
    /// - Hoạt ảnh thở nhịp nhàng khi Idle
    /// - Nghiêng người gồng mình khi kéo vật nặng (Pull)
    /// - Nhảy cẫng ăn mừng khi kéo được vàng/kim cương (Cheer)
    /// Hỗ trợ cả Animator Controller truyền thống lẫn chuyển động Procedural mượt mà.
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        private Animator _animator;
        private Vector3 _baseScale;
        private Vector3 _baseLocalPos;
        private bool _isPulling;
        private float _pullStrain;
        private Coroutine _cheerCoroutine;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _baseScale = transform.localScale;
            _baseLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (_cheerCoroutine != null) return;

            if (_isPulling)
            {
                // Khi đang kéo vật phẩm: nghiêng người ra sau gồng mình
                float strainJitter = Mathf.Sin(Time.time * 25f) * (_pullStrain * 1.5f);
                transform.localRotation = Quaternion.Euler(0f, 0f, -8f + strainJitter);
                transform.localScale = new Vector3(_baseScale.x * 0.96f, _baseScale.y * 1.04f, _baseScale.z);
            }
            else
            {
                // Khi rảnh (Idle): thở nhịp nhàng lên xuống nhẹ nhàng
                float breath = Mathf.Sin(Time.time * 2.5f) * 0.03f;
                transform.localScale = new Vector3(_baseScale.x * (1f - breath * 0.5f), _baseScale.y * (1f + breath), _baseScale.z);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        public void PlayIdle()
        {
            _isPulling = false;
            _pullStrain = 0f;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetTrigger("Idle");
            }
        }

        public void PlayPull(float mass = 1.0f)
        {
            _isPulling = true;
            _pullStrain = Mathf.Clamp01((mass - 1.0f) / 2.0f);
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetTrigger("Pull");
            }
        }

        /// <summary>
        /// Hoạt ảnh ăn mừng nhảy nhẹ khi vừa kéo vàng/kim cương thành công về gốc
        /// </summary>
        public void PlayCheer()
        {
            if (_cheerCoroutine != null) StopCoroutine(_cheerCoroutine);
            _cheerCoroutine = StartCoroutine(CheerRoutine());
        }

        private IEnumerator CheerRoutine()
        {
            _isPulling = false;
            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startPos = transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float jumpY = Mathf.Sin(t * Mathf.PI) * 0.25f;
                transform.localPosition = new Vector3(startPos.x, startPos.y + jumpY, startPos.z);
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 5f);
                yield return null;
            }

            transform.localPosition = startPos;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseScale;
            _cheerCoroutine = null;
        }
    }
}
