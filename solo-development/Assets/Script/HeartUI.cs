using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [Header("스프라이트")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    [Header("하트 설정")]
    [SerializeField] private Vector2 heartSize = new Vector2(48f, 48f);
    [SerializeField] private float spacing = 8f;

    [Header("연출")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeAmount = 6f;

    private readonly List<Image> hearts = new List<Image>();
    private int lastHp = -1;

    // PlayerHealth.onHpChanged에 연결하는 함수
    public void UpdateHearts(int currentHp, int maxHp)
    {
        // 하트 개수가 부족하면 추가, 많으면 숨김
        while (hearts.Count < maxHp)
        {
            hearts.Add(CreateHeart());
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            bool visible = i < maxHp;
            hearts[i].gameObject.SetActive(visible);
            if (!visible) continue;

            hearts[i].sprite = (i < currentHp) ? fullHeart : emptyHeart;
        }

        // HP가 줄었을 때만 흔들림 연출
        if (lastHp >= 0 && currentHp < lastHp)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine());
        }

        lastHp = currentHp;
    }

    private Image CreateHeart()
    {
        var go = new GameObject("Heart", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = heartSize;

        // HeartUI 오브젝트의 왼쪽 위를 기준으로 오른쪽으로 나열
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(hearts.Count * (heartSize.x + spacing), 0f);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        var rt = (RectTransform)transform;
        Vector2 origin = rt.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float falloff = 1f - (elapsed / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeAmount * falloff;
            rt.anchoredPosition = origin + offset;
            yield return null;
        }

        rt.anchoredPosition = origin;
    }
}