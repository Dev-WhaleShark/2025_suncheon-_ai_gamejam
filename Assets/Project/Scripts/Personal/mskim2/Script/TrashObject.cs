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

    public bool IsAlive => _currentHits < maxHits;

    private int _currentHits;
    private Color _originalColor;
    private Collider2D _col;

    private Sequence _hitSequence;
    private Tween _flashTween;
    private Sequence _deathSequence;

    private Vector3 _baseLocalPos;
    private bool _isDying;
    private bool _despawned;

    private Vector3 _initialScale; // 최초 스케일 저장 (재스폰 시 복원)

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

        _initialScale = transform.localScale; // 초기 스케일 기록
    }

    private void KillTweens()
    {
        if (_hitSequence != null)
        {
            _hitSequence.Kill();
            _hitSequence = null;
        }

        if (_flashTween != null)
        {
            _flashTween.Kill();
            _flashTween = null;
        }

        if (_deathSequence != null)
        {
            _deathSequence.Kill();
            _deathSequence = null;
        }
    }

    /// <summary>
    /// 외부 공격 시스템에서 호출. hitDirection은 (가해자->이 객체) 방향 권장.
    /// </summary>
    public void Hit(int hitPower, Vector2 hitPoint, Vector2 hitDirection, UnityEngine.Object attacker)
    {
        if (_isDying)
        {
            return;
        }

        _currentHits += Mathf.Max(1, hitPower);
        onHit?.Invoke();
        onHitProgress?.Invoke(Mathf.Clamp01((float)_currentHits / maxHits));

        PlayHitFeedback(hitDirection, hitPoint);

        if (_currentHits >= maxHits)
        {
            HandleDestroy();
        }
    }

    private void PlayHitFeedback(Vector2 hitDirection, Vector2 hitPoint)
    {
        if (!spriteRenderer)
        {
            return;
        }

        if (hitDirection == Vector2.zero)
        {
            Vector2 center = transform.position;
            hitDirection = (center - hitPoint).normalized;
            if (hitDirection == Vector2.zero)
            {
                hitDirection = Vector2.up;
            }
        }

        hitDirection.Normalize();

        // Flash
        if (_flashTween != null)
        {
            _flashTween.Kill();
        }
        spriteRenderer.color = flashColor;
        _flashTween = spriteRenderer.DOColor(_originalColor, flashDuration).SetEase(Ease.Linear);

        // Push
        if (_hitSequence != null)
        {
            if (restartTweenOnNewHit)
            {
                _hitSequence.Kill();
                transform.localPosition = _baseLocalPos;
            }
            else
            {
                return; // 새 히트 무시
            }
        }

        _baseLocalPos = transform.localPosition;
        Vector3 target = _baseLocalPos + (Vector3)hitDirection * pushDistance;
        _hitSequence = DOTween.Sequence();
        _hitSequence.SetUpdate(false)
            .Append(transform.DOLocalMove(target, pushOutTime).SetEase(pushOutEase))
            .Append(transform.DOLocalMove(_baseLocalPos, pushReturnTime).SetEase(pushReturnEase))
            .OnKill(() => _hitSequence = null);
    }

    private void HandleDestroy()
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

        if (!spriteRenderer)
        {
            FinalizeDeath();
            return;
        }

        _deathSequence = DOTween.Sequence().SetUpdate(false);

        if (deathRotateJitter > 0f && deathRotateJitterLoops > 0)
        {
            float endRot = UnityEngine.Random.Range(-deathRotateJitter, deathRotateJitter);
            _deathSequence.Join(
                transform
                    .DOLocalRotate(new Vector3(0, 0, endRot), deathScaleTime * 0.5f)
                    .SetLoops(deathRotateJitterLoops, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
            );
        }

        Vector3 initScale = transform.localScale;
        _deathSequence.Join(
            transform
                .DOScale(initScale * 0.1f, deathScaleTime)
                .SetEase(deathScaleEase)
        );

        if (spriteRenderer)
        {
            _deathSequence.Join(
                spriteRenderer
                    .DOFade(0f, deathFadeTime)
                    .SetEase(Ease.Linear)
            );
        }

        _deathSequence.OnComplete(FinalizeDeath)
            .OnKill(() => _deathSequence = null);
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

    public void OnSpawned()
    {
        _despawned = false;
        _currentHits = 0;
        _isDying = false;
        KillTweens();

        // 사망 시 축소된 스케일 복원
        transform.localScale = _initialScale;

        _baseLocalPos = transform.localPosition;

        if (_col)
        {
            _col.enabled = true;
        }

        if (spriteRenderer)
        {
            spriteRenderer.color = _originalColor;
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        onHitProgress?.Invoke(0f);
    }

    public void OnDespawned()
    {
        KillTweens();
    }
}
