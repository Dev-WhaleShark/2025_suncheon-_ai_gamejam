using System;
using System.Collections.Generic;
using UnityEngine;
using WhaleShark.Core;
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

    // 보상 선택 후 이동 예정 스테이지 인덱스
    private int pendingNextStageIndex = -1;

    public Stage CurrentStage => currentStageInstance;
    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages?.Count ?? 0;

    public event Action<int, Stage> OnStageLoaded;
    public event Action<int, Stage> OnStageUnloaded;
    public event Action<int, Stage> OnStageCleared;
    public event Action<RewardData> OnRewardChosen; // 선택된 보상 외부 알림

    private RewardData[] collectedRewards; // (향후 확장 대비) 현재는 사용 안 함

    #region Unity
    private void Awake()
    {
        PrepareArrays();
        if (stageRoot == null) stageRoot = transform;
        EventBus.CurrentStageCleared += ReportStageCleared;
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
        if (count <= 0) return;
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

    public void ReloadCurrentStage()
    {
        if (currentStageIndex < 0) return;
        if (currentStageInstance != null) SafeInitialize(currentStageInstance); else LoadStage(currentStageIndex);
    }

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

    public void UnloadCurrentStage() => UnloadCurrentInternal();

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

            if (!hasNext)
            {
                // 마지막 스테이지: 보상 없이 게임 클리어 (요구사항 명시 없으므로 기존 동작 유지)
                GameManager.Instance.GameClear();
                return;
            }

            if (rewardUI != null)
            {
                pendingNextStageIndex = next;

                // 이전 구독 제거 (안전)
                rewardUI.onRewardChosen.RemoveListener(OnRewardSelectionComplete);
                rewardUI.onRewardChosen.AddListener(OnRewardSelectionComplete);

                rewardUI.ShowRandomFromDatabase(-1);
            }
            else
            {
                LoadStage(next);
            }
        }
    }

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
        pendingNextStageIndex = -1;
    }

    public bool IsStageCleared(int index) => IsValidIndex(index) && clearedFlags[index];
    #endregion

    #region Reward Handling
    private void OnRewardSelectionComplete(RewardData data)
    {
        rewardUI.onRewardChosen.RemoveListener(OnRewardSelectionComplete);

        OnRewardChosen?.Invoke(data);

        int next = pendingNextStageIndex;
        pendingNextStageIndex = -1;
        if (IsValidIndex(next))
        {
            LoadStage(next);
        }
        else
        {
            Debug.LogWarning("[StageManager] 보상 완료 후 잘못된 다음 인덱스: " + next);
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
            ResetCharacterMap();
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
        inst.gameObject.SetActive(false); // 재사용
        currentStageInstance = null;
        currentStageIndex = -1;
        OnStageUnloaded?.Invoke(oldIndex, inst);
    }
    #endregion

    public Character character;
    void ResetCharacterMap()
    {
        character.ResetMap();
    }
}
