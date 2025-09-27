using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RewardUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private RectTransform cardsRoot;
    [SerializeField] private RewardCard cardPrefab;
    [SerializeField] private int maxCards = 3;

    [Header("Entry Animation")]
    [SerializeField] private float entryYOffset = 400f;
    [SerializeField] private float entryMoveDuration = 0.55f;
    [SerializeField] private Ease entryMoveEase = Ease.OutCubic;
    [SerializeField] private float entryScaleFrom = 0.85f;
    [SerializeField] private float entryStagger = 0.15f;
    [SerializeField] private float entryFadeDuration = 0.35f;

    [Header("Select Animation")]
    [SerializeField] private float selectScale = 1.15f;
    [SerializeField] private float selectScaleTime = 0.25f;
    [SerializeField] private Ease selectScaleEase = Ease.OutBack;
    [SerializeField] private float unselectedFadeTime = 0.25f;

    [Header("Exit Animation (after selection)")]
    [SerializeField] private float exitDelayAfterSelect = 0.65f;
    [SerializeField] private float exitFadeTime = 0.35f;

    [Header("Input")]
    [SerializeField] private bool blockRaycastWhileAnimating = true;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Events")]
    public UnityEvent onShowStart;
    public UnityEvent onShowComplete;
    public UnityEvent onHideComplete;
    public UnityEvent<RewardData> onRewardChosen;

    [Header("Debug")]
    [SerializeField] private bool debugLogSelection = false;

    private readonly List<RewardCard> _activeCards = new();
    private readonly List<Vector2> _finalPositions = new();
    private bool _isAnimating;
    private bool _isShown;
    private bool _selectionLocked;
    private Sequence _showSequence;
    private Sequence _exitSequence;

    public bool IsShown => _isShown;

    private void Awake()
    {
        if (cardsRoot == null)
        {
            cardsRoot = (RectTransform)transform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public void ShowRewards(List<RewardData> rewards)
    {
        if (rewards == null)
        {
            if (debugLogSelection)
            {
                Debug.LogWarning("[RewardUI] ShowRewards 호출 - rewards == null", this);
            }
            return;
        }

        int count = Mathf.Min(maxCards, rewards.Count);

        if (count <= 0)
        {
            if (debugLogSelection)
            {
                Debug.LogWarning("[RewardUI] ShowRewards 호출 - 빈 리스트 또는 maxCards=0", this);
            }
            return;
        }

        ClearCards();
        _selectionLocked = false;
        _isShown = true;

        if (_showSequence != null)
        {
            _showSequence.Kill();
            _showSequence = null;
        }

        onShowStart?.Invoke();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // Spawn cards and record final positions
        for (int i = 0; i < count; i++)
        {
            RewardCard card = Instantiate(cardPrefab, cardsRoot);
            card.gameObject.SetActive(true);
            card.Initialize(rewards[i], this);

            var rt = (RectTransform)card.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Horizontal align (even spacing). We'll distribute across a width based on card width & gap heuristic.
            float gap = 40f; // simple gap
            float cardWidth = rt.sizeDelta.x;
            float totalWidth = cardWidth * count + gap * (count - 1);
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;
            float targetX = startX + i * (cardWidth + gap);

            Vector2 finalPos = new Vector2(targetX, 0f);
            _finalPositions.Add(finalPos);

            // Set start state
            rt.anchoredPosition = finalPos + new Vector2(0f, -entryYOffset);
            rt.localScale = Vector3.one * entryScaleFrom;
            card.SetAlpha(0f);

            _activeCards.Add(card);
        }

        // Build show sequence
        _showSequence = DOTween.Sequence();
        _isAnimating = true;

        for (int i = 0; i < _activeCards.Count; i++)
        {
            var card = _activeCards[i];
            var rt = (RectTransform)card.transform;
            float delay = i * entryStagger;

            _showSequence.Insert(delay, rt.DOAnchorPos(_finalPositions[i], entryMoveDuration).SetEase(entryMoveEase));
            _showSequence.Insert(delay, rt.DOScale(1f, entryMoveDuration).SetEase(Ease.OutBack, 1.2f));
            _showSequence.Insert(delay, DOTween.To(card.GetAlpha, card.SetAlpha, 1f, entryFadeDuration));
        }

        _showSequence.OnComplete(() =>
        {
            _isAnimating = false;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            onShowComplete?.Invoke();
        });
    }

    public void HideUIImmediate()
    {
        if (!_isShown)
        {
            return;
        }

        if (_showSequence != null)
        {
            _showSequence.Kill();
            _showSequence = null;
        }

        if (_exitSequence != null)
        {
            _exitSequence.Kill();
            _exitSequence = null;
        }

        ClearCards();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        _isShown = false;
        onHideComplete?.Invoke();
    }

    private void ClearCards()
    {
        for (int i = 0; i < _activeCards.Count; i++)
        {
            if (_activeCards[i] != null)
            {
                Destroy(_activeCards[i].gameObject);
            }
        }

        _activeCards.Clear();
        _finalPositions.Clear();
    }

    internal void HandleCardClicked(RewardCard card)
    {
        if (_selectionLocked)
        {
            return;
        }

        _selectionLocked = true;
        var data = card.Data;

        if (debugLogSelection)
        {
            Debug.Log(data != null
                ? $"[RewardUI] Reward 선택: {data.id} ({data.displayName})"
                : "[RewardUI] Reward 선택: 데이터 null", this);
        }

        onRewardChosen?.Invoke(data);

        if (canvasGroup != null && blockRaycastWhileAnimating)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // Animate selection
        for (int i = 0; i < _activeCards.Count; i++)
        {
            var c = _activeCards[i];
            var rt = (RectTransform)c.transform;

            if (c == card)
            {
                rt.DOKill();
                rt.DOScale(selectScale, selectScaleTime).SetEase(selectScaleEase);
            }
            else
            {
                c.FadeAndDisable(unselectedFadeTime);
            }
        }

        // Exit after delay
        if (_exitSequence != null)
        {
            _exitSequence.Kill();
            _exitSequence = null;
        }

        _exitSequence = DOTween.Sequence();
        _exitSequence.AppendInterval(exitDelayAfterSelect);
        if (canvasGroup != null)
        {
            _exitSequence.Append(canvasGroup.DOFade(0f, exitFadeTime));
        }
        _exitSequence.OnComplete(() =>
        {
            HideUIImmediate();
        });
    }

    // ===== Test Helper (Editor button will call) =====
    public void TestShowDummy()
    {
        var dummy = new List<RewardData>();

        for (int i = 0; i < maxCards; i++)
        {
            dummy.Add(new RewardData
            {
                id = $"reward_{i}",
                displayName = $"Reward {i + 1}",
                description = "테스트 보상 설명",
                icon = null
            });
        }

        ShowRewards(dummy);
    }
}
