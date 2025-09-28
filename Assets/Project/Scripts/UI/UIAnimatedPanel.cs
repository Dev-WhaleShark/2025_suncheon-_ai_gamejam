using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIAnimatedPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float scaleDuration = 0.40f;

    [Range(0.5f,1f)]
    [SerializeField] private float openStartScale = 0.9f;
    [Range(0.5f,1f)]
    [SerializeField] private float closeEndScale = 0.9f;

    [SerializeField] private Ease openScaleEase = Ease.OutBack;
    [SerializeField] private Ease closeScaleEase = Ease.InCubic;
    [SerializeField] private Ease openFadeEase = Ease.OutQuad;
    [SerializeField] private Ease closeFadeEase = Ease.InQuad;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Events")]
    public UnityEvent onShowStart;
    public UnityEvent onShowComplete;
    public UnityEvent onShowCompleted; // alias
    public UnityEvent onHideComplete;
    public UnityEvent onHideCompleted; // alias

    private Sequence seq;
    private bool shown;
    private Vector3 originalScale = Vector3.one;

    public bool IsShown => shown;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (scaleTarget == null && canvasGroup != null)
            scaleTarget = canvasGroup.GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (scaleTarget != null)
            originalScale = scaleTarget.localScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    public void Show()
    {
        if (shown) return;
        KillSequence();
        canvasGroup.gameObject.SetActive(true);

        if (scaleTarget != null)
            scaleTarget.localScale = originalScale * openStartScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        seq = DOTween.Sequence().SetUpdate(useUnscaledTime);

        if (scaleTarget != null)
        {
            seq.Join(scaleTarget.DOScale(originalScale, scaleDuration).SetEase(openScaleEase));
        }

        seq.Join(canvasGroup.DOFade(1f, fadeDuration).SetEase(openFadeEase));

        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            shown = true;
            seq = null;
            onShowComplete?.Invoke();
            onShowCompleted?.Invoke();
        });
    }

    public void Hide()
    {
        if (!shown) return;
        KillSequence();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        seq = DOTween.Sequence().SetUpdate(useUnscaledTime);
        if (scaleTarget != null)
        {
            seq.Join(scaleTarget.DOScale(originalScale * closeEndScale, scaleDuration).SetEase(closeScaleEase));
        }
        seq.Join(canvasGroup.DOFade(0f, fadeDuration).SetEase(closeFadeEase));
        seq.OnComplete(() =>
        {
            canvasGroup.gameObject.SetActive(false);

            seq = null;
            onHideComplete?.Invoke();
            onHideCompleted?.Invoke();
        });
    }

    public void HideImmediate()
    {
        KillSequence();
        if (scaleTarget != null) scaleTarget.localScale = originalScale;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        shown = false;
        if (canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(false);
        onHideComplete?.Invoke();
        onHideCompleted?.Invoke();
    }

    public void ShowImmediate()
    {
        KillSequence();
        if (scaleTarget != null) scaleTarget.localScale = originalScale;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        shown = true;
        if (!canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(true);
        onShowComplete?.Invoke();
        onShowCompleted?.Invoke();
    }

    private void KillSequence()
    {
        if (seq != null && seq.IsActive())
            seq.Kill();

        seq = null;
    }
}
