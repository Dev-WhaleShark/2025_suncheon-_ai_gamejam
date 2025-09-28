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
    [SerializeField] private RewardDatabase rewardDatabase;
    [SerializeField] private UIAnimatedPanel rootAnimatedPanel;

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

    [Header("Exit Delay (after selection)")]
    [SerializeField] private float exitDelayAfterSelect = 0.65f;

    [Header("Events")]
    public UnityEvent onShowStart;
    public UnityEvent onShowComplete;
    public UnityEvent onHideComplete;
    public UnityEvent<RewardData> onRewardChosen; // 선택된 데이터 알림

    [Header("External Transition")]
    [Tooltip("StageManager 전환 시 RewardUI 자체가 Exit 애니를 실행하지 않고 외부 전환 연출에 위임.")]
    public bool externalTransitionMode = false;
    public UnityEvent<RewardCard> onRewardCardChosenVisual;

    [Header("Debug")]
    [SerializeField] private bool debugLogSelection = false;

    // 내부 상태
    private readonly List<RewardCard> activeCards = new();
    private readonly List<Vector2> finalPositions = new();
    private bool isShown;
    private bool selectionLocked;

    private Sequence showSequence;   // 카드 등장 애니메이션
    private Tween pendingHideDelay;  // 선택 후 Hide 예약용

    public bool IsShown => isShown;

    #region Unity
    private void Awake()
    {
        if (cardsRoot == null)
            cardsRoot = (RectTransform)transform;
    }

    private void OnDisable()
    {
        if (pendingHideDelay != null && pendingHideDelay.IsActive()) pendingHideDelay.Kill();
        pendingHideDelay = null;
    }
    #endregion

    #region Public API
    public void ShowRewards(List<RewardData> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            if (debugLogSelection) Debug.LogWarning("[RewardUI] ShowRewards - 빈 리스트", this);
            return;
        }

        KillSequences();
        ClearCards();

        selectionLocked = false;
        isShown = true;
        onShowStart?.Invoke();
        gameObject.SetActive(true);

        // 패널 Show 완료 후 카드 애니 시작
        rootAnimatedPanel.onShowComplete.AddListener(OnPanelShownSpawnCards);
        rootAnimatedPanel.Show();

        void OnPanelShownSpawnCards()
        {
            rootAnimatedPanel.onShowComplete.RemoveListener(OnPanelShownSpawnCards);
            SpawnCards(rewards);
            PlayCardsShowSequence();
        }
    }

    public void ShowRandomFromDatabase(int count = -1, bool weightByRarity = true)
    {
        if (rewardDatabase == null)
        {
            Debug.LogWarning("[RewardUI] RewardDatabase 미할당", this);
            return;
        }
        var pool = rewardDatabase.GetRandomDistinct(count <= 0 ? maxCards : Mathf.Min(count, maxCards), weightByRarity);
        ShowRewards(pool);
    }

    public void HideUIImmediate()
    {
        if (!isShown) return;
        if (pendingHideDelay != null && pendingHideDelay.IsActive()) pendingHideDelay.Kill();
        pendingHideDelay = null;
        rootAnimatedPanel.HideImmediate();
        FinalizeHide();
    }
    #endregion

    #region Internal – Spawn / Show
    private void SpawnCards(List<RewardData> rewards)
    {
        int count = Mathf.Min(maxCards, rewards.Count);
        finalPositions.Clear();
        activeCards.Clear();

        for (int i = 0; i < count; i++)
        {
            var card = Instantiate(cardPrefab, cardsRoot);
            card.gameObject.SetActive(true);
            card.Initialize(rewards[i], this);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            float gap = 40f;
            float w = rt.sizeDelta.x;
            float totalW = w * count + gap * (count - 1);
            float startX = -totalW * 0.5f + w * 0.5f;
            float targetX = startX + i * (w + gap);
            Vector2 finalPos = new(targetX, 0f);
            finalPositions.Add(finalPos);

            rt.anchoredPosition = finalPos + new Vector2(0f, -entryYOffset);
            rt.localScale = Vector3.one * entryScaleFrom;
            card.SetAlpha(0f);
            activeCards.Add(card);
        }
    }

    private void PlayCardsShowSequence()
    {
        showSequence = DOTween.Sequence();
        for (int i = 0; i < activeCards.Count; i++)
        {
            var card = activeCards[i];
            var rt = (RectTransform)card.transform;
            float delay = i * entryStagger;
            showSequence.Insert(delay, rt.DOAnchorPos(finalPositions[i], entryMoveDuration).SetEase(entryMoveEase));
            showSequence.Insert(delay, rt.DOScale(1f, entryMoveDuration).SetEase(Ease.OutBack, 1.2f));
            showSequence.Insert(delay, DOTween.To(card.GetAlpha, card.SetAlpha, 1f, entryFadeDuration));
        }
        showSequence.OnComplete(() => onShowComplete?.Invoke());
    }
    #endregion

    #region Selection
    internal void HandleCardClicked(RewardCard card)
    {
        if (selectionLocked) return;
        selectionLocked = true;

        var data = card.Data;
        if (debugLogSelection)
            Debug.Log(data != null ? $"[RewardUI] 선택: {data.id} ({data.displayName})" : "[RewardUI] 선택: 데이터 null", this);

        onRewardChosen?.Invoke(data);
        onRewardCardChosenVisual?.Invoke(card);
    }

    public void PrepareExternalTransition(RewardCard chosen)
    {
        // 이미 카드 선택 애니 중단
        KillSequences();
        // 다른 카드 제거
        for (int i = activeCards.Count - 1; i >= 0; i--)
        {
            var c = activeCards[i];
            if (c == null) { activeCards.RemoveAt(i); continue; }
            if (c != chosen)
            {
                Destroy(c.gameObject);
                activeCards.RemoveAt(i);
            }
        }
        finalPositions.Clear();

        rootAnimatedPanel.Hide();

        if (pendingHideDelay != null && pendingHideDelay.IsActive())
            pendingHideDelay.Kill();

        pendingHideDelay = null;
    }
    #endregion

    #region Finalize / Cleanup
    private void FinalizeHide()
    {
        KillSequences();
        ClearCards();
        isShown = false;
        gameObject.SetActive(false);
        onHideComplete?.Invoke();
    }

    private void KillSequences()
    {
        if (showSequence != null && showSequence.IsActive()) showSequence.Kill();
        showSequence = null;
    }

    private void ClearCards()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] != null)
                Destroy(activeCards[i].gameObject);
        }
        activeCards.Clear();
        finalPositions.Clear();
    }
    #endregion

    #region Test Helpers
    [ContextMenu("Test/Show Random Rewards")] private void TestShowRandom() => ShowRandomFromDatabase(maxCards, true);
    [ContextMenu("Test/Show Random Rewards (No Weight)")] private void TestShowRandomNoWeight() => ShowRandomFromDatabase(maxCards, false);
    #endregion
}
