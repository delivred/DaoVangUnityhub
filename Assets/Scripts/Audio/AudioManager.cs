using UnityEngine;

namespace DaoVang.Audio
{
    /// <summary>
    /// Singleton quản lý toàn bộ âm thanh hiệu ứng (SFX) trong game:
    /// phóng móc, bắt trúng vật phẩm, thu vàng về, bom nổ, chiến thắng và thua cuộc.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip catchClip;
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip bombClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip gameOverClip;

        [Header("Cài đặt")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }

        public void PlayShoot() => PlayClip(shootClip, 0.75f);
        public void PlayCatch() => PlayClip(catchClip, 0.85f);
        public void PlayCollect() => PlayClip(collectClip, 1.0f);
        public void PlayBomb() => PlayClip(bombClip, 1.0f);
        public void PlayWin() => PlayClip(winClip, 0.9f);
        public void PlayGameOver() => PlayClip(gameOverClip, 0.9f);

        private void PlayClip(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, sfxVolume * volumeScale);
            }
        }
    }
}
