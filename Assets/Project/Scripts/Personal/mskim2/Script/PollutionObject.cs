using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using WhaleShark.Core;

[RequireComponent(typeof(Collider2D))]
public class PollutionObject : MonoBehaviour, IPoolable
{
    [Header("Life Settings")] 
    [SerializeField] private int maxHits = 3;

    [Header("Erase Progress")] 
    [SerializeField] private float perHitEraseTime = 0.15f; // 한 번 맞을 때 지워지는 전환 시간
    [SerializeField] private AnimationCurve eraseEase = AnimationCurve.Linear(0,0,1,1);
    [Tooltip("_Cutoff 속성을 가진 머티리얼이 있을 경우 자동 사용 (0=완전 표시, 1=완전 소멸)")] 
    [SerializeField] private bool useCutoffIfAvailable = true;
    [SerializeField] private string cutoffPropertyName = "_Cutoff";

    [Header("Hit Feedback")] 
    [SerializeField] private float flashDuration = 0.06f;
    [SerializeField] private Color flashColor = new Color(1f, 0.6f, 0.6f, 1f);
    [SerializeField] private float jitterScale = 1.05f;
    [SerializeField] private float jitterTime = 0.08f;
    [SerializeField] private Ease jitterEase = Ease.OutQuad;

    [Header("Death Feedback")] 
    [SerializeField] private float finalDissolveTime = 0.25f; // 남은 부분 완전 삭제 시간
    [SerializeField] private float deathShakeStrength = 0.06f; 
    [SerializeField] private int deathShakeVibrato = 10;
    [SerializeField] private float deathMinScale = 0.2f;
    [SerializeField] private Ease deathScaleEase = Ease.InQuad;

    [Header("Pooling")] 
    [SerializeField] private SimplePool pool;

    [Header("References")] 
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Events")] 
    public UnityEvent onHit;                 // Hit 누적 시(Death 전)
    public UnityEvent onDestroyed;           // Death 시(실제 풀 반환 직전)
    public UnityEvent<float> onEraseProgress; // 0~1 진행도 (1 = 사라짐 직전)

    public bool IsAlive => _currentHits < maxHits;

    int _currentHits;
    bool _isDying;
    bool _despawned;
    Collider2D _col;
    Color _originalColor;
    Material _materialInstance; // 인스턴스 머티리얼 (공유 파괴 방지)
    bool _hasCutoff;
    float _currentCutoff; // 0~1

    Tween _flashTween;
    Tween _eraseTween;
    Sequence _deathSeq;
    Tween _jitterScaleTween;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        if (spriteRenderer) _originalColor = spriteRenderer.color;
        if (pool == null) pool = GetComponentInParent<SimplePool>();
        PrepareMaterial();
    }

    void PrepareMaterial()
    {
        if (spriteRenderer == null) return;
        // 머티리얼 인스턴스화 (다른 프리팹과 공유 방지)
        _materialInstance = spriteRenderer.material; // Unity가 자동 인스턴스 생성(렌더러.material 접근)
        if (useCutoffIfAvailable && _materialInstance != null && _materialInstance.HasProperty(cutoffPropertyName))
        {
            _hasCutoff = true;
            _materialInstance.SetFloat(cutoffPropertyName, 0f);
        }
        else
        {
            _hasCutoff = false;
        }
        _currentCutoff = 0f;
    }

    void KillTweens()
    {
        _flashTween?.Kill(); _flashTween = null;
        _eraseTween?.Kill(); _eraseTween = null;
        _deathSeq?.Kill(); _deathSeq = null;
        _jitterScaleTween?.Kill(); _jitterScaleTween = null;
    }

    public void Hit(int hitPower, Vector2 hitPoint, Vector2 hitDirection, UnityEngine.Object attacker)
    {
        if (_isDying) return;
        _currentHits += Mathf.Max(1, hitPower);
        float progress = Mathf.Clamp01((float)_currentHits / maxHits);
        onHit?.Invoke();
        onEraseProgress?.Invoke(progress);
        PlayHitVisual(progress);
        if (_currentHits >= maxHits)
        {
            HandleDeath();
        }
    }

    void PlayHitVisual(float progress)
    {
        if (spriteRenderer == null) return;
        // Flash
        _flashTween?.Kill();
        spriteRenderer.color = flashColor;
        _flashTween = spriteRenderer.DOColor(_originalColor, flashDuration).SetEase(Ease.Linear);

        // Erase 진행
        _eraseTween?.Kill();
        float targetCutoff = progress; // 선형 매핑
        if (_hasCutoff)
        {
            float start = _currentCutoff;
            _eraseTween = DOTween.To(() => start, v => {
                _currentCutoff = v; _materialInstance.SetFloat(cutoffPropertyName, eraseEase.Evaluate(v));
            }, targetCutoff, perHitEraseTime);
        }
        else
        {
            // 머티리얼에 cutoff 없으면 알파 감소 (지워지는 느낌 대체)
            float startA = spriteRenderer.color.a;
            float targetA = 1f - progress; // 점차 사라짐
            _eraseTween = DOTween.To(() => startA, a => {
                var c = spriteRenderer.color; c.a = a; spriteRenderer.color = c;
            }, targetA, perHitEraseTime);
        }

        // Jitter Scale
        _jitterScaleTween?.Kill();
        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale; // 보정
        _jitterScaleTween = DOTween.Sequence()
            .Append(transform.DOScale(baseScale * jitterScale, jitterTime * 0.5f).SetEase(jitterEase))
            .Append(transform.DOScale(baseScale, jitterTime * 0.5f).SetEase(Ease.InOutSine))
            .OnKill(() => _jitterScaleTween = null);
    }

    void HandleDeath()
    {
        if (_isDying) return;
        _isDying = true;
        if (_col) _col.enabled = false;
        onDestroyed?.Invoke();
        KillTweens();

        // 최종 분해/삭제 연출
        _deathSeq = DOTween.Sequence().SetUpdate(false);

        if (_hasCutoff)
        {
            float start = _currentCutoff;
            _deathSeq.Join(DOTween.To(() => start, v => {
                _currentCutoff = v; _materialInstance.SetFloat(cutoffPropertyName, v);
            }, 1f, finalDissolveTime).SetEase(Ease.Linear));
        }
        else if (spriteRenderer)
        {
            float startA = spriteRenderer.color.a;
            _deathSeq.Join(DOTween.To(() => startA, a => { var c = spriteRenderer.color; c.a = a; spriteRenderer.color = c; }, 0f, finalDissolveTime).SetEase(Ease.Linear));
        }

        // Scale down + shake
        Vector3 baseScale = transform.localScale;
        _deathSeq.Join(transform.DOScale(baseScale * deathMinScale, finalDissolveTime).SetEase(deathScaleEase));
        if (deathShakeStrength > 0f && deathShakeVibrato > 0)
        {
            _deathSeq.Join(transform.DOShakePosition(finalDissolveTime, deathShakeStrength, deathShakeVibrato, 90f, false, false));
        }

        _deathSeq.OnComplete(FinalizeDeath).OnKill(() => _deathSeq = null);
    }

    void FinalizeDeath()
    {
        if (_despawned) return;
        _despawned = true;
        if (pool != null) pool.Despawn(gameObject); else gameObject.SetActive(false);
    }

    public void OnSpawned()
    {
        _despawned = false;
        _isDying = false;
        _currentHits = 0;
        KillTweens();
        if (_col) _col.enabled = true;
        if (spriteRenderer)
        {
            if (_materialInstance == null) PrepareMaterial();
            spriteRenderer.color = _originalColor;
            if (_hasCutoff) _materialInstance.SetFloat(cutoffPropertyName, 0f);
            else { var c = spriteRenderer.color; c.a = 1f; spriteRenderer.color = c; }
        }
        _currentCutoff = 0f;
        onEraseProgress?.Invoke(0f);
    }

    public void OnDespawned()
    {
        KillTweens();
    }
}

