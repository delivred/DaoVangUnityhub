using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DaoVang.UI
{
    /// <summary>
    /// Hiệu ứng điểm bay (Floating Score) và tia sáng lấp lánh (Sparkle)
    /// khi người chơi kéo thành công vàng, kim cương về tới thợ mỏ.
    /// </summary>
    public class ScoreEffectManager : MonoBehaviour
    {
        public static ScoreEffectManager Instance { get; private set; }

        [SerializeField] private Sprite sparkleSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Hiển thị chữ số điểm bay lên và chùm tia sáng lấp lánh tại vị trí thu vật phẩm
        /// </summary>
        public void ShowScorePopup(Vector3 worldPos, int score, Color textColor)
        {
            StartCoroutine(FloatingScoreRoutine(worldPos, score, textColor));
            StartCoroutine(SpawnSparklesRoutine(worldPos));
        }

        /// <summary>
        /// Hiển thị văn bản thông báo đặc biệt bay lên (ví dụ: NE BOM!) và chùm tia sáng lấp lánh
        /// </summary>
        public void ShowCustomPopup(Vector3 worldPos, string text, Color textColor)
        {
            StartCoroutine(FloatingCustomRoutine(worldPos, text, textColor));
            StartCoroutine(SpawnSparklesRoutine(worldPos));
        }

        private IEnumerator FloatingCustomRoutine(Vector3 startPos, string text, Color textColor)
        {
            GameObject textObj = new GameObject("CustomPopup");
            textObj.transform.position = startPos + new Vector3(0f, 0.4f, 0f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 6.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.sortingOrder = 30;

            float duration = 0.85f;
            float elapsed = 0f;
            Vector3 origin = textObj.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (textObj != null)
                {
                    textObj.transform.position = origin + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 1.3f, 0f);
                    float scale = (t < 0.2f) ? Mathf.Lerp(0.6f, 1.25f, t / 0.2f) : Mathf.Lerp(1.25f, 1.0f, (t - 0.2f) / 0.8f);
                    textObj.transform.localScale = Vector3.one * scale;

                    if (t > 0.5f)
                    {
                        Color c = tmp.color;
                        c.a = 1f - ((t - 0.5f) / 0.5f);
                        tmp.color = c;
                    }
                }
                yield return null;
            }

            if (textObj != null) Destroy(textObj);
        }

        private IEnumerator FloatingScoreRoutine(Vector3 startPos, int score, Color textColor)
        {
            GameObject textObj = new GameObject("ScorePopup");
            textObj.transform.position = startPos + new Vector3(0f, 0.4f, 0f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"+{score}";
            tmp.fontSize = 6.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.sortingOrder = 30;

            float duration = 0.85f;
            float elapsed = 0f;
            Vector3 origin = textObj.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (textObj != null)
                {
                    // Bay từ từ lên trên
                    textObj.transform.position = origin + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 1.3f, 0f);

                    // Hiệu ứng scale pop
                    float scale = (t < 0.2f) ? Mathf.Lerp(0.6f, 1.25f, t / 0.2f) : Mathf.Lerp(1.25f, 1.0f, (t - 0.2f) / 0.8f);
                    textObj.transform.localScale = Vector3.one * scale;

                    // Fade out
                    if (t > 0.5f)
                    {
                        Color c = tmp.color;
                        c.a = 1f - ((t - 0.5f) / 0.5f);
                        tmp.color = c;
                    }
                }
                yield return null;
            }

            if (textObj != null) Destroy(textObj);
        }

        private IEnumerator SpawnSparklesRoutine(Vector3 center)
        {
            if (sparkleSprite == null) yield break;

            int count = 5;
            List<GameObject> sparkles = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject sp = new GameObject("Sparkle");
                sp.transform.position = center;
                sp.transform.localScale = Vector3.one * Random.Range(0.4f, 0.8f);
                SpriteRenderer sr = sp.AddComponent<SpriteRenderer>();
                sr.sprite = sparkleSprite;
                sr.sortingOrder = 28;
                sparkles.Add(sp);
            }

            float duration = 0.55f;
            float elapsed = 0f;
            List<Vector3> velocities = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(1.2f, 2.5f);
                velocities.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < sparkles.Count; i++)
                {
                    if (sparkles[i] != null)
                    {
                        sparkles[i].transform.position += velocities[i] * Time.deltaTime;
                        SpriteRenderer sr = sparkles[i].GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = 1f - t;
                            sr.color = c;
                        }
                    }
                }
                yield return null;
            }

            foreach (var sp in sparkles)
            {
                if (sp != null) Destroy(sp);
            }
        }
    }
}
