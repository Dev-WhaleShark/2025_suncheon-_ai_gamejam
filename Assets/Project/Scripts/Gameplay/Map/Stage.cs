using UnityEngine;
using WhaleShark.Core;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

[ExecuteAlways]
public class Stage : SerializedMonoBehaviour
{
    public bool isCleared = false;

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

    [SerializeField, OdinSerialize]
    private MapGrid mapGrid = new();

    private readonly Dictionary<Vector2Int, TrashObject> trashMap = new();
    private readonly Dictionary<Vector2Int, PollutionObject> pollutionMap = new();
    private bool runtimeSynced;

    private readonly HashSet<Enemy> registeredEnemies = new();
    private Enemy bossInstance;

    private bool isBossSummoned;
    [SerializeField] GameObject bossPrefab;

    private void Awake()
    {
        if (Application.isPlaying)
            Initialize();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        if (!isInit)
            Initialize();

        SyncRuntimeObjectsFromGrid();
        RegisterExistingEnemies();
        CheckBossSummonCondition();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EnsureEditorInitialized();
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureEditorInitialized();
            if (mapGrid.IsInitialized && mapGrid.GridSize != gridSizeInCells)
                mapGrid.Resize(gridSizeInCells, preserve: true);
        }
#endif
    }

    public void ForceInitialize()
    {
        isInit = false;
        Initialize();
    }

    public void Initialize()
    {
        if (!isInit)
        {
            mapGrid ??= new MapGrid();
            mapGrid.OnAfterDeserialize();

            if (!mapGrid.IsInitialized)
                mapGrid.Initialize(gridSizeInCells);

            mapGrid.OnTileStateChanged -= HandleTileStateChanged;
            mapGrid.OnTileStateChanged += HandleTileStateChanged;

            isInit = true;
        }
    }

    private void EnsureEditorInitialized() => Initialize();

    private void SyncRuntimeObjectsFromGrid()
    {
        if (runtimeSynced || !isInit)
            return;

        var trashCells = new List<Vector2Int>(trashMap.Keys);
        foreach (var c in trashCells)
            DespawnTrashObject(c);

        var pollCells = new List<Vector2Int>(pollutionMap.Keys);
        foreach (var c in pollCells)
            DespawnPollutionObject(c);

        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector2Int(x, y);
                var state = mapGrid.GetTileState(cell);
                if ((state & TileState.Trash) != 0)
                    SpawnTrashObject(cell, spawnOnly: true);
                if ((state & TileState.Pollution) != 0)
                    SpawnPollutionObject(cell, spawnOnly: true);
            }
        }
        runtimeSynced = true;
    }

    private void OnDestroy()
    {
        if (mapGrid != null)
            mapGrid.OnTileStateChanged -= HandleTileStateChanged;

        foreach (var e in registeredEnemies)
            if (e != null) e.onEnemyDied -= OnNormalEnemyDied;
    }

    private void HandleTileStateChanged(Vector2Int cell, TileState state)
    {
        bool hasTrash = (state & TileState.Trash) != 0;
        bool hasPollution = (state & TileState.Pollution) != 0;

        if (Application.isPlaying)
        {
            if (hasTrash)
            {
                if (!trashMap.ContainsKey(cell))
                    SpawnTrashObject(cell, spawnOnly: true);
            }
            else if (trashMap.ContainsKey(cell))
            {
                DespawnTrashObject(cell);
            }

            if (hasPollution)
            {
                if (!pollutionMap.ContainsKey(cell))
                    SpawnPollutionObject(cell, spawnOnly: true);
            }
            else if (pollutionMap.ContainsKey(cell))
            {
                DespawnPollutionObject(cell);
            }

            if (!hasTrash && !hasPollution)
            {
                DespawnTrashObject(cell);
                DespawnPollutionObject(cell);
            }

            EventBus.PublishPurifyProgressUpdated(GetCleanPercentage());

            CheckBossSummonCondition();
        }
#if UNITY_EDITOR
        else
        {
            MarkEditorDirty();
        }
#endif
    }

    #region Public API
    public bool WorldToGrid(Vector3 worldPos, out Vector2Int cell)
    {
        cell = default;
        if (!isInit)
            return false;

        Vector3 local = worldPos - gridOrigin;
        if (local.x < 0 || local.y < 0)
            return false;

        int gx = Mathf.FloorToInt(local.x / cellSize);
        int gy = Mathf.FloorToInt(local.y / cellSize);

        var p = new Vector2Int(gx, gy);
        if (!mapGrid.InBounds(p))
            return false;
        cell = p;

        return true;
    }

    public Vector3 GridToWorldCenter(Vector2Int cell) => gridOrigin + new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
    public bool IsValidGridPosition(Vector2Int cell) => mapGrid.InBounds(cell);

    public void SetTrash(Vector2Int cell, bool enable)
    {
        if (mapGrid.InBounds(cell))
            mapGrid.SetTrash(cell, enable);

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void SetPollution(Vector2Int cell, bool enable)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.SetPollution(cell, enable);

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void CleanCell(Vector2Int cell)
    {
        if (mapGrid.InBounds(cell))
            mapGrid.CleanTile(cell);

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void SetTrashAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
            SetTrash(c, enable);
    }

    public void SetPollutionAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
            SetPollution(c, enable);
    }

    public void CleanAtWorld(Vector3 pos)
    {
        if (WorldToGrid(pos, out var c))
            CleanCell(c);
    }

    public void PolluteAtWorld(Vector3 pos) => SetPollutionAtWorld(pos, true);
    public void PlaceTrashAtWorld(Vector3 pos) => SetTrashAtWorld(pos, true);
    public void ToggleTrash(Vector2Int cell) => SetTrash(cell, !mapGrid.HasTrash(cell));
    public void TogglePollution(Vector2Int cell) => SetPollution(cell, !mapGrid.HasPollution(cell));
    public TileState GetState(Vector2Int cell) => mapGrid.GetTileState(cell);
    public bool HasTrash(Vector2Int cell) => mapGrid.HasTrash(cell);
    public bool HasPollution(Vector2Int cell) => mapGrid.HasPollution(cell);

    public void SetAllTrash(bool enable)
    {
        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
                mapGrid.SetTrash(new Vector2Int(x, y), enable);
#if UNITY_EDITOR
            MarkEditorDirtyIfNotPlaying();
#endif
        }
    }

    public void SetAllPollution(bool enable)
    {
        mapGrid.SetAllPollution(enable);
#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void SetAllClean()
    {
        mapGrid.SetAllClean();
#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void CheckBossSummonCondition()
    {
        if (isCleared || isBossSummoned) return;
        if (GetCleanPercentage() >= 99.5f && !HasRemainingNormalEnemies())
        {
            isCleared = true;
            SummonBoss();
        }
    }

    private bool HasRemainingNormalEnemies()
    {
        var all = GetComponentsInChildren<Enemy>(includeInactive: false);
        for (int i = 0; i < all.Length; i++)
        {
            var e = all[i];
            if (e == null || e == bossInstance || e.currentState == EnemyState.Dead) continue;
            return true;
        }
        return false;
    }

    public float GetCleanPercentage() => !isInit ? 100f : mapGrid.GetCleanRatio() * 100f;

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || enemy == bossInstance || registeredEnemies.Contains(enemy)) return;
        registeredEnemies.Add(enemy);
        enemy.onEnemyDied -= OnNormalEnemyDied;
        enemy.onEnemyDied += OnNormalEnemyDied;
    }
    #endregion

    #region Debug & Editor Utilities

    public void TestRandomTrash(int count = 5)
    {
        for (int i = 0; i < count; i++)
            mapGrid.SetTrash(new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y)), true);

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void TestRandomPollution(int count = 5)
    {
        for (int i = 0; i < count; i++)
            mapGrid.SetPollution(new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y)), true);

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    public void TestRandomBoth(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetPollution(c, true);
            mapGrid.SetTrash(c, true);
        }

#if UNITY_EDITOR
        MarkEditorDirtyIfNotPlaying();
#endif
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!debugDraw)
            return;

        if (!isInit)
        {
            DrawGridOutline();
            return;
        }

        int total = gridSizeInCells.x * gridSizeInCells.y;
        if (total > debugMaxCells)
            return;

        float s = cellSize;
        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector2Int(x, y);
                var state = mapGrid.GetTileState(cell);
                bool t = (state & TileState.Trash) != 0;
                bool p = (state & TileState.Pollution) != 0;
                Color fill = t && p ? bothColor : t ? trashOnlyColor : p ? pollutionOnlyColor : cleanColor;
                Vector3 center = GridToWorldCenter(cell);
                Gizmos.color = fill; Gizmos.DrawCube(center, new Vector3(s, s, 0.01f));
                Gizmos.color = gridLineColor; Gizmos.DrawWireCube(center, new Vector3(s, s, 0));
#if UNITY_EDITOR
                if (debugLabels && SceneView.currentDrawingSceneView != null)
                {
                    string label = t && p ? "P+T" : p ? "P" : t ? "T" : "C";
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
            for (int y = 0; y < gridSizeInCells.y; y++)
                Gizmos.DrawWireCube(gridOrigin + new Vector3((x + 0.5f) * s, (y + 0.5f) * s, 0), new Vector3(s, s, 0));
    }

    #region Internal Spawn/Despawn
    private void SpawnTrashObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (trashMap.ContainsKey(cell) || trashPool == null)
        {
            if (trashPool == null)
                Debug.LogWarning("[MapManager] trashPool 미할당");

            return;
        }
        var go = trashPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);
        if (!go)
            return;

        var comp = go.GetComponent<TrashObject>();
        if (!comp)
        {
            Debug.LogWarning("[MapManager] TrashObject 컴포넌트 누락");
            trashPool.Despawn(go); return;
        }

        trashMap[cell] = comp;
        comp.onDestroyed.AddListener(() => OnTrashDestroyed(cell, comp));
        if (!spawnOnly)
            mapGrid.SetTrash(cell, true);
    }

    private void SpawnPollutionObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (pollutionMap.ContainsKey(cell) || pollutionPool == null)
        {
            if (pollutionPool == null)
                Debug.LogWarning("[MapManager] pollutionPool 미할당");

            return;
        }

        var go = pollutionPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);
        if (!go)
            return;

        var comp = go.GetComponent<PollutionObject>();
        if (!comp)
        {
            Debug.LogWarning("[MapManager] PollutionObject 컴포넌트 누락");
            pollutionPool.Despawn(go);
            return;
        }

        pollutionMap[cell] = comp;
        comp.onDestroyed.AddListener(() => OnPollutionDestroyed(cell, comp));
        if (!spawnOnly)
            mapGrid.SetPollution(cell, true);
    }

    private void DespawnTrashObject(Vector2Int cell)
    {
        if (!trashMap.TryGetValue(cell, out var obj) || !obj)
            return;

        trashMap.Remove(cell);
        if (trashPool != null)
            trashPool.Despawn(obj.gameObject);
        else
            obj.gameObject.SetActive(false);
    }

    private void DespawnPollutionObject(Vector2Int cell)
    {
        if (!pollutionMap.TryGetValue(cell, out var obj) || !obj)
            return;

        pollutionMap.Remove(cell);
        if (pollutionPool != null)
            pollutionPool.Despawn(obj.gameObject);
        else
            obj.gameObject.SetActive(false);
    }

    private void OnTrashDestroyed(Vector2Int cell, TrashObject obj)
    {
        if (trashMap.TryGetValue(cell, out var cur) && cur == obj)
            trashMap.Remove(cell);

        if (mapGrid.HasTrash(cell))
            mapGrid.SetTrash(cell, false);

        CheckBossSummonCondition();
    }

    private void OnPollutionDestroyed(Vector2Int cell, PollutionObject obj)
    {
        if (pollutionMap.TryGetValue(cell, out var cur) && cur == obj)
            pollutionMap.Remove(cell);

        if (mapGrid.HasPollution(cell))
            mapGrid.SetPollution(cell, false);

        CheckBossSummonCondition();
    }
    #endregion
    private void SummonBoss()
    {
        if (isBossSummoned)
            return;
        if (!bossPrefab)
        {
            EventBus.PublishCurrentStageCleared();
            return;
        }

        var bossGo = Instantiate(bossPrefab, transform.position, Quaternion.identity);
        bossInstance = bossGo.GetComponent<Enemy>();
        if (!bossInstance)
        {
            Debug.LogWarning("[Stage] Boss prefab에 Enemy 컴포넌트가 없습니다.");
            EventBus.PublishCurrentStageCleared();
            return;
        }
        isBossSummoned = true;
        bossInstance.onEnemyDied += OnBossDied;
    }

    private void OnBossDied()
    {
        bossInstance = null;
        Debug.Log("Stage OnBossDied called");
        EventBus.PublishCurrentStageCleared();
    }

    private void OnNormalEnemyDied() => CheckBossSummonCondition();

    private void RegisterExistingEnemies()
    {
        var all = GetComponentsInChildren<Enemy>(includeInactive: false);
        for (int i = 0; i < all.Length; i++)
            RegisterEnemy(all[i]);
    }

#if UNITY_EDITOR
    private void MarkEditorDirty()
    {
        Debug.Log($"[Stage] MarkEditorDirty: Prefab={PrefabUtility.IsPartOfPrefabInstance(this)}, Scene={gameObject.scene.name}");
        EditorUtility.SetDirty(this);

        if(PrefabUtility.IsPartOfPrefabInstance(this))
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage != null)
        {
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }
        else
        {
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }

    private void MarkEditorDirtyIfNotPlaying()
    {
        if (!Application.isPlaying)
            MarkEditorDirty();
    }
#endif
}
