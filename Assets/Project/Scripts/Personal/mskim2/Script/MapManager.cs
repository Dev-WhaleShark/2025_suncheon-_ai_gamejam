using TMPro;
using UnityEngine;
using WhaleShark.Core;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapManager : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private Vector2Int gridSizeInCells = new Vector2Int(32, 32);

    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    [Header("UI")]
    [SerializeField] private TMP_Text cleanRatioText;

    [Header("Pools")]
    public MultiPrefabPool trashPool; // TrashObject 풀
    public MultiPrefabPool pollutionPool; // PollutionObject 풀

    [Header("Options")]
    [Tooltip("타일이 Clean 상태로 완전히 돌아갈 때 잔여 오브젝트 자동 제거")]
    [SerializeField] private bool autoRemoveWhenFullyClean = true;

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

    private MapGrid mapGrid = new();
    private bool isInit;

    private readonly Dictionary<Vector2Int, TrashObject> _trashMap = new();
    private readonly Dictionary<Vector2Int, PollutionObject> _pollutionMap = new();

    public MapGrid Grid => mapGrid;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        if (!isInit)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        if (isInit)
        {
            return;
        }

        if (gridSizeInCells.x <= 0 || gridSizeInCells.y <= 0)
        {
            Debug.LogError($"[MapManager] 잘못된 Grid Size {gridSizeInCells}");
            gridSizeInCells = new Vector2Int(Mathf.Max(1, gridSizeInCells.x), Mathf.Max(1, gridSizeInCells.y));
        }

        cellSize = Mathf.Max(0.01f, cellSize);
        mapGrid.Initialize(gridSizeInCells);
        mapGrid.OnTileStateChanged += HandleTileStateChanged;
        isInit = true;
        UpdateCleanRatioUI();
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
        bool hasTrash = (state & TileState.Trash) != 0;
        bool hasPollution = (state & TileState.Pollution) != 0;

        if (hasTrash)
        {
            if (!_trashMap.ContainsKey(cell))
            {
                SpawnTrashObject(cell, spawnOnly: true);
            }
        }
        else
        {
            if (_trashMap.ContainsKey(cell))
            {
                DespawnTrashObject(cell);
            }
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
            if (_pollutionMap.ContainsKey(cell))
            {
                DespawnPollutionObject(cell);
            }
        }

        if (!hasTrash && !hasPollution && autoRemoveWhenFullyClean)
        {
            DespawnTrashObject(cell);
            DespawnPollutionObject(cell);
        }

        UpdateCleanRatioUI();
    }

    private void UpdateCleanRatioUI()
    {
        if (cleanRatioText != null)
        {
            cleanRatioText.text = $"Clean Ratio: {mapGrid.GetCleanRatio():P1}";
        }
    }

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

    public Vector3 GridToWorldCenter(Vector2Int cell)
    {
        return gridOrigin + new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
    }

    public void SetTrash(Vector2Int cell, bool enable)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.SetTrash(cell, enable);
    }

    public void SetPollution(Vector2Int cell, bool enable)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.SetPollution(cell, enable);
    }

    public void CleanCell(Vector2Int cell)
    {
        if (!mapGrid.InBounds(cell))
        {
            return;
        }

        mapGrid.CleanTile(cell);
    }

    public void SetTrashAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
        {
            SetTrash(c, enable);
        }
    }

    public void SetPollutionAtWorld(Vector3 pos, bool enable)
    {
        if (WorldToGrid(pos, out var c))
        {
            SetPollution(c, enable);
        }
    }

    public void CleanAtWorld(Vector3 pos)
    {
        if (WorldToGrid(pos, out var c))
        {
            CleanCell(c);
        }
    }

    public void PolluteAtWorld(Vector3 pos)
    {
        SetPollutionAtWorld(pos, true);
    }

    public void PlaceTrashAtWorld(Vector3 pos)
    {
        SetTrashAtWorld(pos, true);
    }

    public void ToggleTrash(Vector2Int cell)
    {
        SetTrash(cell, !mapGrid.HasTrash(cell));
    }

    public void TogglePollution(Vector2Int cell)
    {
        SetPollution(cell, !mapGrid.HasPollution(cell));
    }

    public TileState GetState(Vector2Int cell)
    {
        return mapGrid.GetTileState(cell);
    }

    public bool HasTrash(Vector2Int cell)
    {
        return mapGrid.HasTrash(cell);
    }

    public bool HasPollution(Vector2Int cell)
    {
        return mapGrid.HasPollution(cell);
    }

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

    public void SetAllPollution(bool enable)
    {
        mapGrid.SetAllPollution(enable);
    }

    public void SetAllClean()
    {
        mapGrid.SetAllClean();
    }

    public float GetCleanRatio()
    {
        return mapGrid.GetCleanRatio();
    }

    private void SpawnTrashObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (_trashMap.ContainsKey(cell))
        {
            return;
        }

        if (trashPool == null)
        {
            Debug.LogWarning("[MapManager] trashPool 미할당");
            return;
        }

        var go = trashPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);

        if (go == null)
        {
            return;
        }

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

    private void SpawnPollutionObject(Vector2Int cell, bool spawnOnly = false)
    {
        if (_pollutionMap.ContainsKey(cell))
        {
            return;
        }

        if (pollutionPool == null)
        {
            Debug.LogWarning("[MapManager] pollutionPool 미할당");
            return;
        }

        var go = pollutionPool.Spawn(GridToWorldCenter(cell), Quaternion.identity);

        if (go == null)
        {
            return;
        }

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

    private void DespawnTrashObject(Vector2Int cell)
    {
        if (!_trashMap.TryGetValue(cell, out var obj) || obj == null)
        {
            return;
        }

        _trashMap.Remove(cell);

        if (trashPool != null)
        {
            trashPool.Despawn(obj.gameObject);
        }
        else
        {
            obj.gameObject.SetActive(false);
        }
    }

    private void DespawnPollutionObject(Vector2Int cell)
    {
        if (!_pollutionMap.TryGetValue(cell, out var obj) || obj == null)
        {
            return;
        }

        _pollutionMap.Remove(cell);

        if (pollutionPool != null)
        {
            pollutionPool.Despawn(obj.gameObject);
        }
        else
        {
            obj.gameObject.SetActive(false);
        }
    }

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

    public void TestRandomTrash(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetTrash(c, true);
        }
    }

    public void TestRandomPollution(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetPollution(c, true);
        }
    }

    public void TestRandomBoth(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var c = new Vector2Int(Random.Range(0, gridSizeInCells.x), Random.Range(0, gridSizeInCells.y));
            mapGrid.SetPollution(c, true);
            mapGrid.SetTrash(c, true);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw)
        {
            return;
        }

        if (!Application.isPlaying || !isInit)
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

#if UNITY_EDITOR
    [CustomEditor(typeof(MapManager))]
    private class MapManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var mgr = (MapManager)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("All Clean"))
                {
                    mgr.SetAllClean();
                }
                if (GUILayout.Button("All Poll ON"))
                {
                    mgr.SetAllPollution(true);
                }
                if (GUILayout.Button("All Poll OFF"))
                {
                    mgr.SetAllPollution(false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("All Trash ON"))
                {
                    mgr.SetAllTrash(true);
                }
                if (GUILayout.Button("All Trash OFF"))
                {
                    mgr.SetAllTrash(false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rand Trash 5"))
                {
                    mgr.TestRandomTrash();
                }
                if (GUILayout.Button("Rand Poll 5"))
                {
                    mgr.TestRandomPollution();
                }
                if (GUILayout.Button("Rand Both 5"))
                {
                    mgr.TestRandomBoth();
                }
            }
        }
    }
#endif
}
