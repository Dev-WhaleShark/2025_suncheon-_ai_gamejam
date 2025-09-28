using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 대화 씬 배경을 교차 페이드로 전환하는 레이어.
/// mainImage는 현재 표시중, transitionImage는 새 스프라이트를 담고 페이드 인 후 main으로 승격.
/// </summary>
[DisallowMultipleComponent]
public class DialogueBackgroundLayer : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image mainImage;          // 현재 배경
    [SerializeField] private Image transitionImage;    // 교차 페이드용 임시 레이어

    [Header("Defaults")]
    [SerializeField] private float defaultFade = 0.35f;
    [SerializeField] private bool setNativeSize = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog;

    private Sprite _current;
    private Tween _fadeTween;

    private void Awake()
    {
        EnsureImages();
        ResetTransitionAlpha();
    }

    private void EnsureImages()
    {
        if (mainImage == null)
        {
            // 첫 번째 자식 이미지 탐색
            var images = GetComponentsInChildren<Image>(true);
            if (images.Length > 0) mainImage = images[0];
        }
        if (transitionImage == null)
        {
            // 보조 이미지 생성
            var go = new GameObject("TransitionImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            transitionImage = go.GetComponent<Image>();
            transitionImage.raycastTarget = false;
        }
        if (mainImage == null)
        {
            var go = new GameObject("MainImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            mainImage = go.GetComponent<Image>();
            mainImage.raycastTarget = false;
        }
        // 순서: main(0), transition(1)
        mainImage.transform.SetSiblingIndex(0);
        transitionImage.transform.SetSiblingIndex(1);
    }

    private void ResetTransitionAlpha()
    {
        if (transitionImage != null)
        {
            var c = transitionImage.color;
            c.a = 0f;
            transitionImage.color = c;
        }
    }

    public void SetImmediate(Sprite sprite)
    {
        KillTween();
        _current = sprite;
        if (mainImage != null)
        {
            mainImage.sprite = sprite;
            if (setNativeSize && sprite != null) mainImage.SetNativeSize();
            var c = mainImage.color; c.a = 1f; mainImage.color = c;
        }
        if (transitionImage != null)
        {
            transitionImage.sprite = null;
            var c2 = transitionImage.color; c2.a = 0f; transitionImage.color = c2;
        }
        if (debugLog) Debug.Log($"[DialogueBackgroundLayer] Immediate set -> {(sprite ? sprite.name : "(null)")}", this);
    }

    public void Apply(Sprite sprite, float fadeTime)
    {
        if (sprite == _current)
        {
            if (debugLog) Debug.Log("[DialogueBackgroundLayer] 동일 스프라이트 -> 무시", this);
            return;
        }
        if (fadeTime <= 0f) fadeTime = defaultFade;
        if (_current == null)
        {
            // 첫 적용은 즉시 세팅
            SetImmediate(sprite);
            return;
        }
        CrossFade(sprite, fadeTime);
    }

    private void CrossFade(Sprite next, float duration)
    {
        KillTween();
        if (transitionImage == null || mainImage == null)
        {
            SetImmediate(next);
            return;
        }
        transitionImage.sprite = next;
        if (setNativeSize && next != null) transitionImage.SetNativeSize();
        // 초기 알파 0
        var tc = transitionImage.color; tc.a = 0f; transitionImage.color = tc;
        var mc = mainImage.color; mc.a = 1f; mainImage.color = mc;

        _fadeTween = DOTween.To(() => 0f, v =>
        {
            if (transitionImage)
            {
                var c = transitionImage.color; c.a = v; transitionImage.color = c;
            }
            if (mainImage)
            {
                var c2 = mainImage.color; c2.a = 1f - v; mainImage.color = c2;
            }
        }, 1f, duration).SetUpdate(true).OnComplete(() =>
        {
            if (mainImage)
            {
                mainImage.sprite = next;
                var c3 = mainImage.color; c3.a = 1f; mainImage.color = c3;
            }
            if (transitionImage)
            {
                transitionImage.sprite = null;
                var c4 = transitionImage.color; c4.a = 0f; transitionImage.color = c4;
            }
            _current = next;
            if (debugLog) Debug.Log($"[DialogueBackgroundLayer] CrossFade complete -> {(next ? next.name : "(null)")}", this);
        });
    }

    private void KillTween()
    {
        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
        _fadeTween = null;
    }
}

