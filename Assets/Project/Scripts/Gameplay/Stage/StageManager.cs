using System;
using System.Collections.Generic;
using UnityEngine;
using WhaleShark.Gameplay;

public class StageManager : MonoBehaviour
{
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

    public Stage CurrentStage => currentStageInstance;
    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages?.Count ?? 0;

    public event Action<int, Stage> OnStageLoaded;
    public event Action<int, Stage> OnStageUnloaded;
    public event Action<int, Stage> OnStageCleared;

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

            // 자동으로 다음 스테이지 로드
            LoadNextStage();
        }
    }

    /// <summary>
    /// 특정 스테이지가 클리어됐는지 여부.
    /// </summary>
    public bool IsCleared(int index)
    {
        if (!IsValidIndex(index)) return false;
        return clearedFlags[index];
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
