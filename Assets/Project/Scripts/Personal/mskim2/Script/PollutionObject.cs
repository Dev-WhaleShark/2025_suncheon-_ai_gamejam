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
    [SerializeField] private AnimationCurve eraseEase = AnimationCurve.Linear(0, 0, 1, 1);
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

    public bool IsAlive => _currentHits < maxHits && !_isDying;

    private int _currentHits;
    private bool _isDying;
    private bool _despawned;
    private Collider2D _col;
    private Color _originalColor;
    private Material _materialInstance; // 인스턴스 머티리얼 (공유 파괴 방지)
    private bool _hasCutoff;
    private float _currentCutoff; // 0~1
    private Vector3 _initialScale; // 최초 스케일 저장

    private Tween _flashTween;
    private Tween _eraseTween;
    private Sequence _deathSeq;
    private Tween _jitterScaleTween;

    private void Awake()
    {
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        _col = GetComponent<Collider2D>();

        if (spriteRenderer)
        {
            _originalColor = spriteRenderer.color;
        }

        if (pool == null)
        {
            pool = GetComponentInParent<SimplePool>();
        }

        _initialScale = transform.localScale; // 초기 스케일 캐싱
        PrepareMaterial();
    }

    private void PrepareMaterial()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        _materialInstance = spriteRenderer.material; // material 접근 시 자동 인스턴스화

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

    private void KillTweens()
    {
        if (_flashTween != null)
        {
            _flashTween.Kill();
            _flashTween = null;
        }

        if (_eraseTween != null)
        {
            _eraseTween.Kill();
            _eraseTween = null;
        }

        if (_deathSeq != null)
        {
            _deathSeq.Kill();
            _deathSeq = null;
        }

        if (_jitterScaleTween != null)
        {
            _jitterScaleTween.Kill();
            _jitterScaleTween = null;
        }
    }

    public void Hit(int hitPower, Vector2 hitPoint, Vector2 hitDirection, UnityEngine.Object attacker)
    {
        if (_isDying)
        {
            return;
        }

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

    private void PlayHitVisual(float progress)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Flash
        if (_flashTween != null)
        {
            _flashTween.Kill();
        }

        spriteRenderer.color = flashColor;
        _flashTween = spriteRenderer
            .DOColor(_originalColor, flashDuration)
            .SetEase(Ease.Linear);

        // Erase
        if (_eraseTween != null)
        {
            _eraseTween.Kill();
        }

        float targetCutoff = progress;

        if (_hasCutoff)
        {
            float start = _currentCutoff;
            _eraseTween = DOTween.To(
                () => start,
                v =>
                {
                    _currentCutoff = v;
                    _materialInstance.SetFloat(cutoffPropertyName, eraseEase.Evaluate(v));
                },
                targetCutoff,
                perHitEraseTime
            );
        }
        else
        {
            float startA = spriteRenderer.color.a;
            float targetA = 1f - progress;
            _eraseTween = DOTween.To(
                () => startA,
                a =>
                {
                    var c = spriteRenderer.color;
                    c.a = a;
                    spriteRenderer.color = c;
                },
                targetA,
                perHitEraseTime
            );
        }

        // Jitter Scale
        if (_jitterScaleTween != null)
        {
            _jitterScaleTween.Kill();
        }

        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale;
        _jitterScaleTween = DOTween.Sequence()
            .Append(transform.DOScale(baseScale * jitterScale, jitterTime * 0.5f).SetEase(jitterEase))
            .Append(transform.DOScale(baseScale, jitterTime * 0.5f).SetEase(Ease.InOutSine))
            .OnKill(() => _jitterScaleTween = null);
    }

    private void HandleDeath()
    {
        if (_isDying)
        {
            return;
        }

        _isDying = true;

        if (_col)
        {
            _col.enabled = false;
        }

        onDestroyed?.Invoke();
        KillTweens();

        _deathSeq = DOTween.Sequence().SetUpdate(false);

        if (_hasCutoff)
        {
            float start = _currentCutoff;
            _deathSeq.Join(
                DOTween.To(
                    () => start,
                    v =>
                    {
                        _currentCutoff = v;
                        _materialInstance.SetFloat(cutoffPropertyName, v);
                    },
                    1f,
                    finalDissolveTime
                ).SetEase(Ease.Linear)
            );
        }
        else if (spriteRenderer)
        {
            float startA = spriteRenderer.color.a;
            _deathSeq.Join(
                DOTween.To(
                    () => startA,
                    a =>
                    {
                        var c = spriteRenderer.color;
                        c.a = a;
                        spriteRenderer.color = c;
                    },
                    0f,
                    finalDissolveTime
                ).SetEase(Ease.Linear)
            );
        }

        Vector3 baseScale = transform.localScale;
        _deathSeq.Join(
            transform
                .DOScale(baseScale * deathMinScale, finalDissolveTime)
                .SetEase(deathScaleEase)
        );

        if (deathShakeStrength > 0f && deathShakeVibrato > 0)
        {
            _deathSeq.Join(
                transform.DOShakePosition(
                    finalDissolveTime,
                    deathShakeStrength,
                    deathShakeVibrato,
                    90f,
                    false,
                    false
                )
            );
        }

        _deathSeq.OnComplete(FinalizeDeath).OnKill(() => _deathSeq = null);
    }

    private void FinalizeDeath()
    {
        if (_despawned)
        {
            return;
        }

        _despawned = true;

        if (pool != null)
        {
            pool.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void ForceKill()
    {
        if (!IsAlive)
        {
            return;
        }

        _currentHits = maxHits;
        HandleDeath();
    }

    public void TestHit()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PollutionObject] Play 모드에서만 TestHit 동작");
            return;
        }

        Hit(1, transform.position, Vector2.up, null);
    }

    public void TestKill()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PollutionObject] Play 모드에서만 TestKill 동작");
            return;
        }

        ForceKill();
    }

    public void OnSpawned()
    {
        _despawned = false;
        _isDying = false;
        _currentHits = 0;
        KillTweens();

        // 스케일 복원 (사망 연출로 축소된 상태에서 재사용되는 문제 해결)
        transform.localScale = _initialScale;

        if (_col)
        {
            _col.enabled = true;
        }

        if (spriteRenderer)
        {
            if (_materialInstance == null)
            {
                PrepareMaterial();
            }

            spriteRenderer.color = _originalColor;

            if (_hasCutoff)
            {
                _materialInstance.SetFloat(cutoffPropertyName, 0f);
            }
            else
            {
                var c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
        }

        _currentCutoff = 0f;
        onEraseProgress?.Invoke(0f);
    }

    public void OnDespawned()
    {
        KillTweens();
    }
}
