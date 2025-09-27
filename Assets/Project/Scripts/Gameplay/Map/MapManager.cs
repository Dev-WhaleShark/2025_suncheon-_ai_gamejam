using TMPro;
using UnityEngine;
using WhaleShark.Core;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] // 에디터(Play 전)에서도 초기화 및 그리드 상태 유지
public class MapManager : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private Vector2Int gridSizeInCells = new Vector2Int(32, 32);

    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    [Header("Pools")]
    public MultiPrefabPool trashPool; // TrashObject 풀
    public MultiPrefabPool pollutionPool; // PollutionObject 풀

    #region Debug
    [Header("Debug View")]
    [SerializeField] private bool debugDraw = true;

    [SerializeField] private bool debugLabels = true;
    [SerializeField] private Color cleanColor = new Color(0.2f, 0.6f, 1f, 0.10f);
    [SerializeField] private Color trashOnlyColor = new Color(1f, 0.9f, 0.15f, 0.40f);
    [SerializeField] private Color pollutionOnlyColor = new Color(0.9f, 0.2f, 1f, 0.45f);
    [SerializeField] private Color bothColor = new Color(1f, 0.45f, 0.2f, 0.55f); // Trash + Pollution
    [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private int debugMaxCells = 10000;
    [SerializeField] private float labelYOffsetFactor = 0.15f;
    #endregion

    private bool isInit;
    [SerializeField] private MapGrid mapGrid = new();
    private Vector2Int _lastInitSize;

    private readonly Dictionary<Vector2Int, TrashObject> _trashMap = new();
    private readonly Dictionary<Vector2Int, PollutionObject> _pollutionMap = new();
    private bool _runtimeSynced;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Initialize();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            if (!isInit) Initialize();
            SyncRuntimeObjectsFromGrid();
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureEditorInitialized();
        }
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureEditorInitialized();
        }
#endif
    }

    public void Initialize()
    {
        if (mapGrid != null)
        {
            mapGrid.RebuildFromSerializedIfNeeded();
        }

        if (!isInit)
        {
            if (mapGrid == null)
            {
                mapGrid = new MapGrid();
            }

            if (!mapGrid.IsInitialized)
            {
                mapGrid.Initialize(gridSizeInCells);
            }

            mapGrid.OnTileStateChanged -= HandleTileStateChanged;
            mapGrid.OnTileStateChanged += HandleTileStateChanged;
            isInit = true;
            _lastInitSize = mapGrid.GridSize; // 실제 내부 사이즈 기준
            return;
        }

        if (mapGrid != null && mapGrid.IsInitialized && mapGrid.GridSize != gridSizeInCells)
        {
            mapGrid.Resize(gridSizeInCells, preserveContents: true);
            _lastInitSize = mapGrid.GridSize;
        }

        // 4) 이벤트 누락 복구 안전장치
        if (mapGrid != null)
        {
            mapGrid.OnTileStateChanged -= HandleTileStateChanged;
            mapGrid.OnTileStateChanged += HandleTileStateChanged;
        }
    }

    private void EnsureEditorInitialized()
    {
        Initialize();
    }

    private void SyncRuntimeObjectsFromGrid()
    {
        if (_runtimeSynced) return;
        if (!isInit) return;

        var trashCells = new List<Vector2Int>(_trashMap.Keys);
        foreach (var c in trashCells) DespawnTrashObject(c);
        var pollCells = new List<Vector2Int>(_pollutionMap.Keys);
        foreach (var c in pollCells) DespawnPollutionObject(c);

        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector2Int(x, y);
                var state = mapGrid.GetTileState(cell);
                if ((state & TileState.Trash) != 0)
                {
                    SpawnTrashObject(cell, spawnOnly: true);
                }
                if ((state & TileState.Pollution) != 0)
                {
                    SpawnPollutionObject(cell, spawnOnly: true);
                }
            }
        }
        _runtimeSynced = true;
    }

    private void OnDestroy()
    {
        if (mapGrid != null)
        {
            mapGrid.OnTileStateChanged -= HandleTileStateChanged;
        }
    }

    private void HandleTileStateChanged(Vector2Int cell, TileState state)
    {
        // 에디터 모드에서도 상태 반영 (프리팹 저장용)
        bool hasTrash = (state & TileState.Trash) != 0;
        bool hasPollution = (state & TileState.Pollution) != 0;

        if (Application.isPlaying)
        {
            if (hasTrash)
            {
                if (!_trashMap.ContainsKey(cell))
                {
                    SpawnTrashObject(cell, spawnOnly: true);
                }
            }
            else
            {
                if (_trashMap.ContainsKey(cell)) DespawnTrashObject(cell);
            }

            if (hasPollution)
            {
                if (!_pollutionMap.ContainsKey(cell))
                {
                    SpawnPollutionObject(cell, spawnOnly: true);
                }
            }
            else
            {
                if (_pollutionMap.ContainsKey(cell)) DespawnPollutionObject(cell);
            }

            if (!hasTrash && !hasPollution)
            {
                DespawnTrashObject(cell);
                DespawnPollutionObject(cell);
            }
        }
#if UNITY_EDITOR
        else
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }


    #region Public API
    /// <summary>
    /// 월드 좌표를 그리드 셀 좌표로 변환. 성공 시 true 반환.
    /// </summary>
    /// <param name="worldPos">월드 좌표</param>
    /// <param name="cell">그리드 셀 (성공 시 유효)</param>
    public bool WorldToGrid(Vector3 worldPos, out Vector2Int cell)
    {
        cell = default;

        if (!isInit)
        {
            return false;
        }

        Vector3 local = worldPos - gridOrigin;

        if (local.x < 0 || local.y < 0)
        {
            return false;
        }

        int gx = Mathf.FloorToInt(local.x / cellSize);
        int gy = Mathf.FloorToInt(local.y / cellSize);
        var p = new Vector2Int(gx, gy);

        if (!mapGrid.InBounds(p))
        {
            return false;
        }

        cell = p;
        return true;
    }

    /// <summary>
    /// 그리드 셀의 월드 중앙 좌표 반환.
    /// </summary>
    public Vector3 GridToWorldCenter(Vector2Int cell)
    {
        return gridOrigin + new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
    }

    /// <summary>
    /// 셀이 그리드 범위 안인지 여부.
    /// </summary>
    public bool IsValidGridPosition(Vector2Int cell) => mapGrid.InBounds(cell);

    /// <summary>
    /// 특정 셀의 Trash 플래그 설정/해제.
    /// </summary>
    public void SetTrash(Vector2Int cell, bool enable)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.SetTrash(cell, enable);
    }

    /// <summary>
    /// 특정 셀의 Pollution 플래그 설정/해제.
    /// </summary>
    public void SetPollution(Vector2Int cell, bool enable)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.SetPollution(cell, enable);
    }

    /// <summary>
    /// 특정 셀을 완전히 Clean 상태로.
    /// </summary>
    public void CleanCell(Vector2Int cell)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.CleanTile(cell);
    }

    /// <summary>
    /// 월드 좌표에 해당하는 셀 Trash 설정/해제.
    /// </summary>
    public void SetTrashAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
        {
            SetTrash(c, enable);
        }
    }

    /// <summary>
    /// 월드 좌표에 해당하는 셀 Pollution 설정/해제.
    /// </summary>
    public void SetPollutionAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
        {
            SetPollution(c, enable);
        }
    }

    /// <summary>
    /// 월드 좌표 셀을 Clean.
    /// </summary>
    public void CleanAtWorld(Vector3 pos)
    {
        if (WorldToGrid(pos, out var c))
        {
            CleanCell(c);
        }
    }

    /// <summary>
    /// 월드 좌표 셀에 Pollution ON.
    /// </summary>
    public void PolluteAtWorld(Vector3 pos)
    {
        SetPollutionAtWorld(pos, true);
    }

    /// <summary>
    /// 월드 좌표 셀에 Trash ON.
    /// </summary>
    public void PlaceTrashAtWorld(Vector3 pos)
    {
        SetTrashAtWorld(pos, true);
    }

    /// <summary>
    /// 해당 셀 Trash 토글.
    /// </summary>
    public void ToggleTrash(Vector2Int cell)
    {
        SetTrash(cell, !mapGrid.HasTrash(cell));
    }

    /// <summary>
    /// 해당 셀 Pollution 토글.
    /// </summary>
    public void TogglePollution(Vector2Int cell)
    {
        SetPollution(cell, !mapGrid.HasPollution(cell));
    }

    /// <summary>
    /// 해당 셀 전체 상태 비트플래그 반환.
    /// </summary>
    public TileState GetState(Vector2Int cell)
    {
        return mapGrid.GetTileState(cell);
    }

    /// <summary>
    /// Trash 존재 여부.
    /// </summary>
    public bool HasTrash(Vector2Int cell)
    {
        return mapGrid.HasTrash(cell);
    }

    /// <summary>
    /// Pollution 존재 여부.
    /// </summary>
    public bool HasPollution(Vector2Int cell)
    {
        return mapGrid.HasPollution(cell);
    }

    /// <summary>
    /// 전체 셀 Trash 일괄 설정.
    /// </summary>
    public void SetAllTrash(bool enable)
    {
        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                mapGrid.SetTrash(new Vector2Int(x, y), enable);
            }
        }
    }

    /// <summary>
    /// 전체 셀 Pollution 일괄 설정.
    /// </summary>
    public void SetAllPollution(bool enable)
    {
        mapGrid.SetAllPollution(enable);
    }

    /// <summary>
    /// 전체 클린으로 초기화.
    /// </summary>
    public void SetAllClean()
    {
        mapGrid.SetAllClean();
    }
    #endregion

    // ====== Debug & Editor Utilities (게임 플레이 빌드에서 호출 지양) ==================
    #region Debug & Editor Utilities
    /// <summary>무작위 Trash 셀 생성 (디버그)</summary>
    public void TestRandomTrash(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetTrash(c, true);
        }
    }

    /// <summary>무작위 Pollution 셀 생성 (디버그)</summary>
    public void TestRandomPollution(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetPollution(c, true);
        }
    }

    /// <summary>무작위 Trash+Pollution 셀 생성 (디버그)</summary>
    public void TestRandomBoth(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetPollution(c, true);
            mapGrid.SetTrash(c, true);
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (!debugDraw)
        {
            return;
        }

        // 에디터/플레이 공통: 초기화 안 되었으면 윤곽만
        if (!isInit)
        {
            DrawGridOutline();
            return;
        }

        int total = gridSizeInCells.x * gridSizeInCells.y;
        if (total > debugMaxCells)
        {
            return;
        }

        float s = cellSize;

        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector2Int(x, y);
                var state = mapGrid.GetTileState(cell);
                bool t = (state & TileState.Trash) != 0;
                bool p = (state & TileState.Pollution) != 0;
                Color fill;

                if (t && p)
                {
                    fill = bothColor;
                }
                else if (t)
                {
                    fill = trashOnlyColor;
                }
                else if (p)
                {
                    fill = pollutionOnlyColor;
                }
                else
                {
                    fill = cleanColor;
                }

                Vector3 center = GridToWorldCenter(cell);
                Gizmos.color = fill;
                Gizmos.DrawCube(center, new Vector3(s, s, 0.01f));
                Gizmos.color = gridLineColor;
                Gizmos.DrawWireCube(center, new Vector3(s, s, 0));
#if UNITY_EDITOR
                if (debugLabels && SceneView.currentDrawingSceneView != null)
                {
                    string label = (t && p) ? "P+T" : (p ? "P" : (t ? "T" : "C"));
                    Handles.color = Color.white;
                    Handles.Label(center + Vector3.up * (s * labelYOffsetFactor), label);
                }
#endif
            }
        }
    }

    private void DrawGridOutline()
    {
        float s = Mathf.Max(0.01f, cellSize);
        Gizmos.color = gridLineColor;

        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                Vector3 center = gridOrigin + new Vector3((x + 0.5f) * s, (y + 0.5f) * s, 0);
                Gizmos.DrawWireCube(center, new Vector3(s, s, 0));
            }
        }
    }

    #region Internal Spawn/Despawn
    /// <summary>
    /// TrashObject를 해당 셀에 스폰. spawnOnly=true 이면 grid 상태는 건드리지 않고 시각 오브젝트만 생성.
    /// </summary>
    private void SpawnTrashObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (_trashMap.ContainsKey(cell)) return;
        if (trashPool == null)
        {
            Debug.LogWarning("[MapManager] trashPool 미할당");
            return;
        }
        var go = trashPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);
        if (go == null) return;
        var comp = go.GetComponent<TrashObject>();
        if (comp == null)
        {
            Debug.LogWarning("[MapManager] TrashObject 컴포넌트 누락");
            trashPool.Despawn(go);
            return;
        }
        _trashMap[cell] = comp;
        comp.onDestroyed.AddListener(() => OnTrashDestroyed(cell, comp));
        if (!spawnOnly)
        {
            mapGrid.SetTrash(cell, true);
        }
    }

    /// <summary>
    /// PollutionObject를 해당 셀에 스폰. spawnOnly=true 이면 grid 상태는 유지.
    /// </summary>
    private void SpawnPollutionObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (_pollutionMap.ContainsKey(cell)) return;
        if (pollutionPool == null)
        {
            Debug.LogWarning("[MapManager] pollutionPool 미할당");
            return;
        }
        var go = pollutionPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);
        if (go == null) return;
        var comp = go.GetComponent<PollutionObject>();
        if (comp == null)
        {
            Debug.LogWarning("[MapManager] PollutionObject 컴포넌트 누락");
            pollutionPool.Despawn(go);
            return;
        }
        _pollutionMap[cell] = comp;
        comp.onDestroyed.AddListener(() => OnPollutionDestroyed(cell, comp));
        if (!spawnOnly)
        {
            mapGrid.SetPollution(cell, true);
        }
    }

    /// <summary>
    /// TrashObject 제거 및 풀 반환.
    /// </summary>
    private void DespawnTrashObject(Vector2Int cell)
    {
        if (!_trashMap.TryGetValue(cell, out var obj) || obj == null) return;
        _trashMap.Remove(cell);
        if (trashPool != null) trashPool.Despawn(obj.gameObject); else obj.gameObject.SetActive(false);
    }

    /// <summary>
    /// PollutionObject 제거 및 풀 반환.
    /// </summary>
    private void DespawnPollutionObject(Vector2Int cell)
    {
        if (!_pollutionMap.TryGetValue(cell, out var obj) || obj == null) return;
        _pollutionMap.Remove(cell);
        if (pollutionPool != null) pollutionPool.Despawn(obj.gameObject); else obj.gameObject.SetActive(false);
    }

    /// <summary>
    /// TrashObject 실제 오브젝트가 파괴(자체 이벤트) 되었을 때 Grid 상태 동기화.
    /// </summary>
    private void OnTrashDestroyed(Vector2Int cell, TrashObject obj)
    {
        if (_trashMap.TryGetValue(cell, out var cur) && cur == obj)
        {
            _trashMap.Remove(cell);
        }
        if (mapGrid.HasTrash(cell))
        {
            mapGrid.SetTrash(cell, false);
        }
    }

    /// <summary>
    /// PollutionObject 파괴 시 Grid 상태 동기화.
    /// </summary>
    private void OnPollutionDestroyed(Vector2Int cell, PollutionObject obj)
    {
        if (_pollutionMap.TryGetValue(cell, out var cur) && cur == obj)
        {
            _pollutionMap.Remove(cell);
        }
        if (mapGrid.HasPollution(cell))
        {
            mapGrid.SetPollution(cell, false);
        }
    }
    #endregion
}
