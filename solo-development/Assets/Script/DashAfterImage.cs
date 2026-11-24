using System.Collections.Generic;
using UnityEngine;

public class DashAfterImage : MonoBehaviour
{
    [Header("잔상 설정")]
    [SerializeField] private float spawnInterval = 0.03f;  // 잔상 생성 간격
    [SerializeField] private float fadeDuration = 0.3f;    // 사라지는 시간
    [SerializeField] private Color startColor = new Color(0.4f, 0.8f, 1f, 0.7f);

    [Header("정렬")]
    [SerializeField] private int sortingOrderOffset = -1;  // 본체보다 뒤에 그림

    private SpriteRenderer sourceSprite;
    private bool isActive;
    private float spawnTimer;

    // 잔상 오브젝트 재사용 (매번 Instantiate/Destroy 하지 않도록)
    private readonly List<AfterImage> pool = new List<AfterImage>();

    private class AfterImage
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float timeLeft;
        public bool inUse;
    }

    void Awake()
    {
        sourceSprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // 생성
        if (isActive && sourceSprite != null)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                Spawn();
                spawnTimer = spawnInterval;
            }
        }

        // 페이드 아웃
        for (int i = 0; i < pool.Count; i++)
        {
            var img = pool[i];
            if (!img.inUse) continue;

            img.timeLeft -= Time.deltaTime;
            if (img.timeLeft <= 0f)
            {
                img.obj.SetActive(false);
                img.inUse = false;
                continue;
            }

            float t = img.timeLeft / fadeDuration; // 1 → 0
            Color c = startColor;
            c.a = startColor.a * t;
            img.sr.color = c;
        }
    }

    public void StartEffect()
    {
        isActive = true;
        spawnTimer = 0f; // 시작하자마자 첫 잔상 생성
    }

    public void StopEffect()
    {
        isActive = false;
    }

    private void Spawn()
    {
        AfterImage img = GetFromPool();

        // 현재 스프라이트 모양/위치/방향을 그대로 복사
        Transform src = sourceSprite.transform;
        img.obj.transform.SetPositionAndRotation(src.position, src.rotation);
        img.obj.transform.localScale = src.lossyScale;

        img.sr.sprite = sourceSprite.sprite;
        img.sr.flipX = sourceSprite.flipX;
        img.sr.flipY = sourceSprite.flipY;
        img.sr.sortingLayerID = sourceSprite.sortingLayerID;
        img.sr.sortingOrder = sourceSprite.sortingOrder + sortingOrderOffset;
        img.sr.color = startColor;

        img.timeLeft = fadeDuration;
        img.inUse = true;
        img.obj.SetActive(true);
    }

    private AfterImage GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].inUse) return pool[i];
        }

        var go = new GameObject("AfterImage");
        var sr = go.AddComponent<SpriteRenderer>();
        var img = new AfterImage { obj = go, sr = sr };
        pool.Add(img);
        return img;
    }

    void OnDestroy()
    {
        foreach (var img in pool)
        {
            if (img.obj != null) Destroy(img.obj);
        }
    }
}