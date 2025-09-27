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
    [SerializeField] private RewardDatabase rewardDatabase; // ScriptableObject 데이터베이스

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

    [Header("External Transition")] public bool externalTransitionMode = false; // StageManager가 선택 후 전환 연출을 가져갈 때 true
    public UnityEvent<RewardCard> onRewardCardChosenVisual; // 선택된 카드(비주얼) 전달

    private readonly List<RewardCard> _activeCards = new();
    private readonly List<Vector2> _finalPositions = new();
    private bool _isAnimating;
    private bool _isShown;
    private bool _selectionLocked;

    const float ROOT_OPEN_START_SCALE = 0.9f;
    const float ROOT_CLOSE_END_SCALE = 0.9f;
    const float ROOT_SCALE_TIME = 0.30f;          // 패널 전체 스케일 시간
    const float ROOT_FADE_TIME = 0.22f;           // 패널 페이드 시간 (카드보다 살짝 빠르게 시작 가능)
    const Ease ROOT_OPEN_SCALE_EASE = Ease.OutBack;
    const Ease ROOT_CLOSE_SCALE_EASE = Ease.InCubic;
    const Ease ROOT_OPEN_FADE_EASE = Ease.OutQuad;
    const Ease ROOT_CLOSE_FADE_EASE = Ease.InQuad;

    private Sequence _rootOpenSeq;   // 루트 패널 오픈
    private Sequence _showSequence;  // 카드 개별 애니메이션 (기존)
    private Sequence _exitSequence;  // 선택 후 종료

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
            if (debugLogSelection) Debug.LogWarning("[RewardUI] ShowRewards 호출 - rewards == null", this);
            return;
        }
        int count = Mathf.Min(maxCards, rewards.Count);
        if (count <= 0)
        {
            if (debugLogSelection) Debug.LogWarning("[RewardUI] ShowRewards 호출 - 빈 리스트 또는 maxCards=0", this);
            return;
        }

        KillAllSequences();
        ClearCards();

        _selectionLocked = false;
        _isShown = true;
        onShowStart?.Invoke();

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;           // 루트 페이드 인
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // 루트 스케일 초기화
        if (cardsRoot != null)
        {
            cardsRoot.localScale = Vector3.one * ROOT_OPEN_START_SCALE;
        }

        SpawnCards(rewards, count);
        PlayRootOpenAnimation(() => BuildAndPlayShowSequence());
    }

    // Spawn & initial placement
    private void SpawnCards(List<RewardData> rewards, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var card = Instantiate(cardPrefab, cardsRoot);
            card.gameObject.SetActive(true);
            card.Initialize(rewards[i], this);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            float gap = 40f;
            float cardWidth = rt.sizeDelta.x;
            float totalWidth = cardWidth * count + gap * (count - 1);
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;
            float targetX = startX + i * (cardWidth + gap);
            Vector2 finalPos = new Vector2(targetX, 0f);
            _finalPositions.Add(finalPos);

            rt.anchoredPosition = finalPos + new Vector2(0f, -entryYOffset);
            rt.localScale = Vector3.one * entryScaleFrom;
            card.SetAlpha(0f);
            _activeCards.Add(card);
        }
    }

    private void PlayRootOpenAnimation(System.Action onRootShown)
    {
        if (canvasGroup == null || cardsRoot == null)
        {
            // 바로 카드 애니메이션 진행
            onRootShown?.Invoke();
            return;
        }
        _rootOpenSeq = DOTween.Sequence();
        _rootOpenSeq.Join(cardsRoot.DOScale(1f, ROOT_SCALE_TIME).SetEase(ROOT_OPEN_SCALE_EASE));
        _rootOpenSeq.Join(canvasGroup.DOFade(1f, ROOT_FADE_TIME).SetEase(ROOT_OPEN_FADE_EASE));
        _rootOpenSeq.OnComplete(() =>
        {
            _rootOpenSeq = null;
            onRootShown?.Invoke();
        });
    }

    private void BuildAndPlayShowSequence()
    {
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

    internal void HandleCardClicked(RewardCard card)
    {
        if (_selectionLocked) return;
        _selectionLocked = true;
        var data = card.Data;
        if (debugLogSelection)
        {
            Debug.Log(data != null ? $"[RewardUI] Reward 선택: {data.id} ({data.displayName})" : "[RewardUI] Reward 선택: 데이터 null", this);
        }
        onRewardChosen?.Invoke(data);
        onRewardCardChosenVisual?.Invoke(card); // 비주얼 전달 (외부 전환 사용 가능)

        if (externalTransitionMode)
        {
            // 외부 전환에서 카드 애니메이션을 이어받으므로 내부 Exit 로직을 수행하지 않음
            return;
        }

        if (canvasGroup != null && blockRaycastWhileAnimating)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // 선택 카드/비선택 카드 처리 (내부 기본 애니메이션)
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

        // 종료 시퀀스 (루트 축소 + 페이드 아웃)
        if (_exitSequence != null) { _exitSequence.Kill(); _exitSequence = null; }
        _exitSequence = DOTween.Sequence();
        _exitSequence.AppendInterval(exitDelayAfterSelect);
        if (canvasGroup != null)
        {
            _exitSequence.Join(canvasGroup.DOFade(0f, exitFadeTime).SetEase(ROOT_CLOSE_FADE_EASE));
        }
        if (cardsRoot != null)
        {
            _exitSequence.Join(cardsRoot.DOScale(ROOT_CLOSE_END_SCALE, ROOT_SCALE_TIME).SetEase(ROOT_CLOSE_SCALE_EASE));
        }
        _exitSequence.OnComplete(() => HideUIImmediate());
    }

    public void PrepareExternalTransition(RewardCard chosen)
    {
        // 다른 카드 제거, 선택 카드만 남김 (남긴 카드는 StageManager가 재부모화 예정)
        for (int i = 0; i < _activeCards.Count; i++)
        {
            var c = _activeCards[i];
            if (c != null && c != chosen)
            {
                Destroy(c.gameObject);
            }
        }
        _activeCards.Clear();
        _finalPositions.Clear();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f; // 패널 자체는 숨김
        }
        _isShown = false; // 논리적으로 닫힘 처리 (선택 카드만 외부 연출용)
    }

    public void HideUIImmediate()
    {
        if (!_isShown) return;
        KillAllSequences();
        ClearCards();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);
        _isShown = false;
        onHideComplete?.Invoke();
    }

    private void KillAllSequences()
    {
        if (_rootOpenSeq != null && _rootOpenSeq.IsActive()) _rootOpenSeq.Kill();
        if (_showSequence != null && _showSequence.IsActive()) _showSequence.Kill();
        if (_exitSequence != null && _exitSequence.IsActive()) _exitSequence.Kill();
        _rootOpenSeq = _showSequence = _exitSequence = null;
    }

    /// <summary>
    /// RewardDatabase 에서 무작위(가중치 포함 선택 가능)로 count 만큼 뽑아 표시.
    /// </summary>
    public void ShowRandomFromDatabase(int count = -1, bool weightByRarity = true)
    {
        if (rewardDatabase == null)
        {
            Debug.LogWarning("[RewardUI] RewardDatabase 미할당");
            return;
        }
        int useCount = count <= 0 ? maxCards : Mathf.Min(count, maxCards);
        var list = rewardDatabase.GetRandomDistinct(useCount, weightByRarity);
        ShowRewards(list);
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

    // ===== Test Helper (Editor button will call) =====
    [ContextMenu("Test/Show Random Rewards")]
    public void TestShowRandom()
    {
        ShowRandomFromDatabase(maxCards, true);
    }

    [ContextMenu("Test/Show Random Rewards (No Weight)")]
    public void TestShowRandomNoWeight()
    {
        ShowRandomFromDatabase(maxCards, false);
    }

}
