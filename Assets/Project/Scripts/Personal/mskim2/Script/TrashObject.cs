using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using WhaleShark.Core; // DOTween 사용

[RequireComponent(typeof(Collider2D))]
public class TrashObject : MonoBehaviour, IPoolable
{
    [Header("Life Settings")] 
    [SerializeField] private int maxHits = 3;

    [Header("Pooling")] 
    [SerializeField] private SimplePool pool;

    [Header("Hit Feedback")]
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float pushDistance = 0.25f;
    [SerializeField] private float pushOutTime = 0.07f;
    [SerializeField] private float pushReturnTime = 0.12f;
    [SerializeField] private Ease pushOutEase = Ease.OutQuad;
    [SerializeField] private Ease pushReturnEase = Ease.InQuad;
    [SerializeField] private bool restartTweenOnNewHit = true;

    [Header("Death Feedback")]
    [SerializeField] private float deathScaleTime = 0.25f;
    [SerializeField] private float deathFadeTime = 0.20f;
    [SerializeField] private float deathRotateJitter = 15f;
    [SerializeField] private int deathRotateJitterLoops = 4;
    [SerializeField] private Ease deathScaleEase = Ease.InBack;

    [Header("References")] 
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Events")] 
    public UnityEvent onHit;
    public UnityEvent onDestroyed;
    public UnityEvent<float> onHitProgress; // 0~1 누적 비율

    public bool IsAlive => currentHits < maxHits;

    int currentHits;
    Color originalColor;
    Collider2D col;

    Sequence hitSequence;
    Tween flashTween;
    Sequence deathSequence;
    Vector3 baseLocalPos;
    bool isDying;
    bool _despawned;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (spriteRenderer)
            originalColor = spriteRenderer.color;
        if (pool == null)
            pool = GetComponentInParent<SimplePool>();
    }
    
    void KillTweens()
    {
        hitSequence?.Kill(); hitSequence = null;
        flashTween?.Kill(); flashTween = null;
        deathSequence?.Kill(); deathSequence = null;
    }

    /// <summary>
    /// 외부 공격 시스템에서 호출. hitDirection은 (가해자->이 객체) 방향 권장.
    /// </summary>
    public void Hit(int hitPower, Vector2 hitPoint, Vector2 hitDirection, UnityEngine.Object attacker)
    {
        if (isDying) return;

        currentHits += Mathf.Max(1, hitPower);
        onHit?.Invoke();
        onHitProgress?.Invoke(Mathf.Clamp01((float)currentHits / maxHits));

        PlayHitFeedback(hitDirection, hitPoint);

        if (currentHits >= maxHits)
        {
            HandleDestroy();
        }
    }

    void PlayHitFeedback(Vector2 hitDirection, Vector2 hitPoint)
    {
        if (!spriteRenderer) return;

        // 방향 보정: 0 벡터면 오브젝트 중심 기준
        if (hitDirection == Vector2.zero)
        {
            Vector2 center = transform.position;
            hitDirection = (center - hitPoint).normalized;
            if (hitDirection == Vector2.zero) hitDirection = Vector2.up; // 완전 동일 위치 fallback
        }

        hitDirection.Normalize();

        // Flash: 기존 진행 중이면 재시작
        flashTween?.Kill();
        spriteRenderer.color = flashColor;
        flashTween = spriteRenderer.DOColor(originalColor, flashDuration).SetEase(Ease.Linear);

        // Push
        if (hitSequence != null)
        {
            if (restartTweenOnNewHit)
            {
                hitSequence.Kill();
                transform.localPosition = baseLocalPos; // 원위치 강제
            }
            else return; // 새 히트 무시
        }

        baseLocalPos = transform.localPosition; // 현재 위치 기준 (연속 히트 자연스러운 반응)
        Vector3 target = baseLocalPos + (Vector3)hitDirection * pushDistance;
        hitSequence = DOTween.Sequence();
        hitSequence.SetUpdate(false)
                   .Append(transform.DOLocalMove(target, pushOutTime).SetEase(pushOutEase))
                   .Append(transform.DOLocalMove(baseLocalPos, pushReturnTime).SetEase(pushReturnEase))
                   .OnKill(() => hitSequence = null);
    }

    void HandleDestroy()
    {
        if (isDying) return;
        isDying = true;
        if (col) col.enabled = false;
        onDestroyed?.Invoke();

        KillTweens(); // 히트 트윈 중단 후 사망 연출 시작

        if (!spriteRenderer)
        {
            FinalizeDeath();
            return;
        }

        deathSequence = DOTween.Sequence().SetUpdate(false);

        // 회전 흔들 (Z축 기준 2D)
        if (deathRotateJitter > 0f && deathRotateJitterLoops > 0)
        {
            float endRot = UnityEngine.Random.Range(-deathRotateJitter, deathRotateJitter);
            deathSequence.Join(transform.DOLocalRotate(new Vector3(0,0,endRot), deathScaleTime * 0.5f)
                                          .SetLoops(deathRotateJitterLoops, LoopType.Yoyo)
                                          .SetEase(Ease.InOutSine));
        }

        // 스케일 축소 + 페이드
        Vector3 initScale = transform.localScale;
        deathSequence.Join(transform.DOScale(initScale * 0.1f, deathScaleTime).SetEase(deathScaleEase));
        if (spriteRenderer)
            deathSequence.Join(spriteRenderer.DOFade(0f, deathFadeTime).SetEase(Ease.Linear));

        deathSequence.OnComplete(FinalizeDeath)
                     .OnKill(() => deathSequence = null);
    }

    void FinalizeDeath()
    {
        if (_despawned) return; // 중복 방지
        _despawned = true;
        // 풀 존재하면 풀로 반환, 없으면 비활성 (Destroy 사용 안 함)
        if (pool != null)
        {
            pool.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawned()
    {
        _despawned = false;
        currentHits = 0;
        isDying = false;
        KillTweens();
        baseLocalPos = transform.localPosition;
        if (col) col.enabled = true;
        if (spriteRenderer)
        {
            spriteRenderer.color = originalColor;
            var c = spriteRenderer.color; c.a = 1f; spriteRenderer.color = c; // 페이드 복구
        }
        onHitProgress?.Invoke(0f);
    }

    public void OnDespawned()
    {
        KillTweens();
    }
}
