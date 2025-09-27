using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using WhaleShark.Gameplay;

public class StageManager : MonoBehaviour
{
    public RewardUI rewardUI;

    [Header("Stage Prefabs")]
    [SerializeField] private List<Stage> stages = new List<Stage>();

    [Header("Runtime Parent")]
    [SerializeField] private Transform stageRoot;

    [SerializeField] private int startStageIndex = 0;

    private int currentStageIndex = -1;
    private Stage currentStageInstance;

    // 인스턴스/클리어 상태 보관
    private Stage[] runtimeInstances;
    private bool[] clearedFlags;

    private bool waitingReward = false;
    private int pendingNextStageIndex = -1;
    private RewardData lastChosenReward;

    // Transition overlay & sequence
    private CanvasGroup transitionOverlay;
    private RectTransform transitionOverlayRect;
    private Sequence rewardTransitionSeq;

    public Stage CurrentStage => currentStageInstance;
    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages?.Count ?? 0;

    public event Action<int, Stage> OnStageLoaded;
    public event Action<int, Stage> OnStageUnloaded;
    public event Action<int, Stage> OnStageCleared;

    public event Action<RewardData> OnRewardChosen;

    private RewardData[] collectedRewards;

    #region Unity
    private void Awake()
    {
        PrepareArrays();
        if (stageRoot == null) stageRoot = transform;
    }

    private void Start()
    {
        if (StageCount == 0)
        {
            Debug.LogWarning("[StageManager] 스테이지 프리팹이 비어 있습니다.");
            return;
        }
        LoadStage(startStageIndex);
    }
    #endregion

    #region Initialization Helpers
    private void PrepareArrays()
    {
        int count = StageCount;
        if (count <= 0)
            return;

        runtimeInstances = new Stage[count];
        clearedFlags = new bool[count];
    }

    private bool IsValidIndex(int index) => index >= 0 && index < StageCount;
    #endregion

    #region Public API
    public void LoadStage(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogError("[StageManager] Invalid stage index: " + index);
            return;
        }
        if (index == currentStageIndex && currentStageInstance != null)
        {
            return;
        }

        UnloadCurrentInternal();

        Stage instance = runtimeInstances[index];
        if (instance == null && stages[index] != null)
        {
            instance = InstantiateStage(stages[index]);
            runtimeInstances[index] = instance;

            instance.onStageCleared += ReportStageCleared;
        }

        currentStageIndex = index;
        currentStageInstance = instance;

        if (currentStageInstance == null)
        {
            Debug.LogError("[StageManager] 스테이지 인스턴스 생성 실패 index=" + index);
            return;
        }

        currentStageInstance.gameObject.SetActive(true);

        if (!clearedFlags[index])
        {
            SafeInitialize(currentStageInstance);
        }

        OnStageLoaded?.Invoke(index, currentStageInstance);
    }

    /// <summary>
    /// 현재 스테이지 다시 로드(초기화) - 클리어 플래그는 유지.
    /// </summary>
    public void ReloadCurrentStage()
    {
        if (currentStageIndex < 0)
        {
            return;
        }
        // 클리어 되어있지 않다면 단순 Initialize 재호출, 되어있으면 굳이 다시 초기화 안할 수도 있음.
        if (currentStageInstance != null)
        {
            SafeInitialize(currentStageInstance);
        }
        else
        {
            LoadStage(currentStageIndex);
        }
    }

    /// <summary>
    /// 다음 스테이지 로드 (마지막이면 아무 동작 안 함)
    /// </summary>
    public void LoadNextStage()
    {
        int next = currentStageIndex + 1;
        if (!IsValidIndex(next))
        {
            Debug.Log("[StageManager] 마지막 스테이지입니다.");
            GameManager.Instance.GameClear();
            return;
        }
        LoadStage(next);
    }

    /// <summary>
    /// 현재 스테이지 언로드.
    /// </summary>
    public void UnloadCurrentStage()
    {
        UnloadCurrentInternal();
    }

    /// <summary>
    /// 외부에서 (예: Stage 내부) 클리어 보고 시 호출.
    /// </summary>
    public void ReportStageCleared()
    {
        if (!IsValidIndex(currentStageIndex) || currentStageInstance == null) return;
        if (!clearedFlags[currentStageIndex])
        {
            clearedFlags[currentStageIndex] = true;
            currentStageInstance.isCleared = true;
            OnStageCleared?.Invoke(currentStageIndex, currentStageInstance);

            int next = currentStageIndex + 1;
            bool hasNext = IsValidIndex(next);

            // 마지막 스테이지면 즉시 게임 클리어
            if (!hasNext)
            {
                GameManager.Instance.GameClear();
                return;
            }

            // 이미 보상 대기 중이면 중복 처리 방지
            if (waitingReward)
            {
                return;
            }

            if ( rewardUI != null)
            {
                waitingReward = true;
                pendingNextStageIndex = next;

                rewardUI.externalTransitionMode = true;
                rewardUI.onRewardChosen.AddListener(StoreChosenReward);
                rewardUI.onRewardCardChosenVisual.AddListener(OnRewardCardVisualChosen);

                if (!rewardUI.IsShown)
                {
                    rewardUI.ShowRandomFromDatabase(-1 );
                }
            }
            else
            {
                LoadStage(next);
            }
        }
    }

    /// <summary>
    /// 모든 진행 상황 초기화(클리어 플래그 및 인스턴스)
    /// </summary>
    public void ResetAllProgress(bool destroyInstances = true)
    {
        if (clearedFlags != null)
        {
            for (int i = 0; i < clearedFlags.Length; i++) clearedFlags[i] = false;
        }
        if (destroyInstances && runtimeInstances != null)
        {
            for (int i = 0; i < runtimeInstances.Length; i++)
            {
                if (runtimeInstances[i] != null)
                {
                    DestroyImmediate(runtimeInstances[i].gameObject);
                    runtimeInstances[i] = null;
                }
            }
        }
        currentStageInstance = null;
        currentStageIndex = -1;
        waitingReward = false;
        pendingNextStageIndex = -1;
    }

    public bool IsStageCleared(int index)
    {
        if (!IsValidIndex(index)) return false;
        return clearedFlags[index];
    }

    private void StoreChosenReward(RewardData data)
    {
        if (lastChosenReward == null)
        {
            lastChosenReward = data;
        }
    }

    private void OnRewardCardVisualChosen(RewardCard card)
    {
        if (!waitingReward) return;
        waitingReward = false;

        if (rewardUI != null)
        {
            rewardUI.onRewardChosen.RemoveListener(StoreChosenReward);
            rewardUI.onRewardCardChosenVisual.RemoveListener(OnRewardCardVisualChosen);
            rewardUI.PrepareExternalTransition(card); // 다른 카드 제거 & 패널 숨김
        }

        OnRewardChosen?.Invoke(card.Data);
        StartRewardToNextStageSequence(card, lastChosenReward, pendingNextStageIndex);
    }

    private void StartRewardToNextStageSequence(RewardCard card, RewardData data, int nextIndex)
    {
        KillRewardTransitionSequence();
        EnsureTransitionOverlay();

        if (card != null)
        {
            Destroy(card.gameObject);
        }

        transitionOverlay.alpha = 0f;

        rewardTransitionSeq = DOTween.Sequence();

        rewardTransitionSeq.Append(transitionOverlay.DOFade(0.6f, 0.30f).SetEase(Ease.Linear));
        rewardTransitionSeq.AppendCallback(() =>
        {
            int tgt = nextIndex;
            pendingNextStageIndex = -1;
            if (IsValidIndex(tgt))
            {
                LoadStage(tgt);
            }
            else
            {
                Debug.LogWarning("[StageManager] Invalid next index during transition: " + tgt);
            }
        });

        rewardTransitionSeq.AppendInterval(0.02f);
        rewardTransitionSeq.AppendCallback(() =>
        {
            if (currentStageInstance != null)
            {
                var st = currentStageInstance.transform;
                st.localScale = Vector3.one * 1.05f; // 가벼운 팝 효과 (과한 강조 제거)
                rewardTransitionSeq.Join(st.DOScale(1f, 0.28f).SetEase(Ease.OutCubic));
            }
            rewardTransitionSeq.Join(transitionOverlay.DOFade(0f, 0.30f));
        });
        rewardTransitionSeq.OnComplete(() =>
        {
            transitionOverlay.gameObject.SetActive(false);
            lastChosenReward = null;
            rewardTransitionSeq = null;
        });
    }

    private void EnsureTransitionOverlay()
    {
        if (transitionOverlay != null) { transitionOverlay.gameObject.SetActive(true); return; }
        var canvasGO = new GameObject("StageTransitionOverlay", typeof(RectTransform), typeof(CanvasGroup));

        Transform parent = rewardUI != null ? rewardUI.transform.root : transform;
        canvasGO.transform.SetParent(parent, false);

        transitionOverlayRect = canvasGO.GetComponent<RectTransform>();
        transitionOverlayRect.anchorMin = Vector2.zero;
        transitionOverlayRect.anchorMax = Vector2.one;
        transitionOverlayRect.offsetMin = Vector2.zero;
        transitionOverlayRect.offsetMax = Vector2.zero;
        transitionOverlay = canvasGO.GetComponent<CanvasGroup>();

        var img = canvasGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        transitionOverlay.alpha = 0f;
        canvasGO.SetActive(true);
        // CanvasGroup raycast 차단 (필요 시 true)
        transitionOverlay.blocksRaycasts = false;
        transitionOverlay.interactable = false;
    }

    private void KillRewardTransitionSequence()
    {
        if (rewardTransitionSeq != null && rewardTransitionSeq.IsActive())
        {
            rewardTransitionSeq.Kill();
            rewardTransitionSeq = null;
        }
    }

    #endregion

    #region Internal
    private Stage InstantiateStage(Stage prefab)
    {
        if (prefab == null) return null;
        var inst = Instantiate(prefab, stageRoot != null ? stageRoot : transform);
        inst.gameObject.name = $"Stage_{prefab.name}";
        inst.gameObject.SetActive(true);
        return inst;
    }

    private void SafeInitialize(Stage stage)
    {
        try
        {
            stage.Initialize();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StageManager] Stage Initialize 예외: {ex.Message}\n{ex}");
        }
    }

    private void UnloadCurrentInternal()
    {
        if (currentStageInstance == null) return;
        int oldIndex = currentStageIndex;
        var inst = currentStageInstance;

        // 파괴하지 않고 비활성화하여 재사용
        inst.gameObject.SetActive(false);

        currentStageInstance = null;
        currentStageIndex = -1;
        OnStageUnloaded?.Invoke(oldIndex, inst);
    }
    #endregion
}
