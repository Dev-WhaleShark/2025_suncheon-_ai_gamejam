using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

[DisallowMultipleComponent]
public class RewardCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Events")]
    public UnityEvent onClicked;

    public RewardData Data { get; private set; }
    private RewardUI _owner;

    private bool _initialized;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(HandleClickInternal);
        }
    }

    public void Initialize(RewardData data, RewardUI owner)
    {
        Data = data;
        _owner = owner;
        _initialized = true;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        if (titleText != null)
        {
            titleText.text = data.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.description;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    private void HandleClickInternal()
    {
        if (!_initialized)
        {
            return;
        }

        onClicked?.Invoke();

        if (_owner != null)
        {
            _owner.HandleCardClicked(this);
        }
    }

    public float GetAlpha()
    {
        if (canvasGroup == null)
        {
            return 1f;
        }

        return canvasGroup.alpha;
    }

    public void SetAlpha(float a)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = a;
    }

    public void FadeAndDisable(float duration)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.DOKill();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOFade(0f, duration).SetEase(Ease.Linear);
    }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = interactable;
            canvasGroup.interactable = interactable;
        }

        if (selectButton != null)
        {
            selectButton.interactable = interactable;
        }
    }
}

