using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using WhaleShark.Core;

public class MapManager : MonoBehaviour
{ 
    [Header("UI")]
    [SerializeField] private TMP_Text cleanRatioText;

    [Header("Tilemap")] 
    [SerializeField] private Tilemap baseTilemap; // 고정 맵 타일맵

    [Header("Pollution Tiles")] 
    [SerializeField] private TileBase pollutedTile; // 오염 상태 표현용 타일
    [SerializeField] private TileBase cleanTile; // 정화 상태 대체 타일(옵션)
    [SerializeField] private bool useCleanTile; // true: cleanTile 사용, false: 원본 타일 복원

    [Header("Trash")] public SimplePool trashPool;
    
    public PollutionGrid PollutionGrid { get; private set; } = new PollutionGrid();

    private Vector2Int gridSizeInCells; // 전체 그리드 셀 개수 (width/height)
    private Vector2 cellWorldSize;
    private BoundsInt bounds; // 타일맵 경계
    private bool isInit;
    private TileBase[,] originalTiles; // 원본 타일 저장 (복원용)

    private void OnValidate()
    {
        if (baseTilemap == null)
            baseTilemap = GetComponentInChildren<Tilemap>();
    }

    private void Awake() => Initialize();

    private void Start()
    {
        if (!isInit) 
            Initialize();
    }

    /// <summary>
    /// PollutionGrid 초기화 및 원본 타일 캐시, 초기 오염 표시
    /// </summary>
    public void Initialize()
    {
        if (isInit) return;
        if (baseTilemap == null)
        {
            Debug.LogError("MapManager: baseTilemap 이 설정되지 않았습니다.", this);
            return;
        }

        bounds = baseTilemap.cellBounds;
        gridSizeInCells = new Vector2Int(bounds.size.x, bounds.size.y);
        cellWorldSize = baseTilemap.cellSize;

        PollutionGrid.Initialize(gridSizeInCells);
        CacheOriginalTiles();
        PollutionGrid.OnPollutionChanged += HandlePollutionChanged;
        RefreshAllTiles();
        isInit = true;

        UpdateCleanRatioUI();
    }

    private void OnDestroy()
    {
        PollutionGrid.OnPollutionChanged -= HandlePollutionChanged;
    }

    private void CacheOriginalTiles()
    {
        originalTiles = new TileBase[gridSizeInCells.x, gridSizeInCells.y];
        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                originalTiles[x, y] = baseTilemap.HasTile(cell) ? baseTilemap.GetTile(cell) : null;
            }
        }
    }

    private void HandlePollutionChanged(Vector2Int gridPos, bool polluted)
    {
        var cell = new Vector3Int(gridPos.x + bounds.xMin, gridPos.y + bounds.yMin, 0);
        if (!baseTilemap.HasTile(cell)) return; // 타일 없는 곳은 무시
        ApplyTileState(cell, gridPos, polluted);
        UpdateCleanRatioUI();
    }

    private void UpdateCleanRatioUI()
    {
        if (cleanRatioText == null) return;
        // :P1 이 퍼센트 변환을 이미 수행하므로 % 추가하지 않음
        cleanRatioText.text = $"Clean Ratio: {GetCleanRatio():P1}";
    }

    private void RefreshAllTiles()
    {
        for (int x = 0; x < gridSizeInCells.x; x++)
        {
            for (int y = 0; y < gridSizeInCells.y; y++)
            {
                var cell = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                if (!baseTilemap.HasTile(cell)) continue;
                bool polluted = PollutionGrid.IsPolluted(new Vector2Int(x, y));
                ApplyTileState(cell, new Vector2Int(x, y), polluted);
            }
        }
    }

    private void ApplyTileState(Vector3Int cell, Vector2Int gridPos, bool polluted)
    {
        if (polluted)
        {
            if (pollutedTile == null) return; // 오염 타일 미지정이면 스킵
            baseTilemap.SetTile(cell, pollutedTile);
        }
        else
        {
            if (useCleanTile && cleanTile != null)
            {
                baseTilemap.SetTile(cell, cleanTile);
            }
            else
            {
                int lx = gridPos.x;
                int ly = gridPos.y;
                if (originalTiles != null && lx >= 0 && ly >= 0 && lx < originalTiles.GetLength(0) && ly < originalTiles.GetLength(1))
                {
                    baseTilemap.SetTile(cell, originalTiles[lx, ly]);
                }
            }
        }
    }

    public bool WorldToPollutionGrid(Vector3 worldPos, out Vector2Int gridPos)
    {
        gridPos = default;
        if (!isInit) return false;
        var cell = baseTilemap.WorldToCell(worldPos);
        int gx = cell.x - bounds.xMin;
        int gy = cell.y - bounds.yMin;
        if (gx < 0 || gy < 0 || gx >= gridSizeInCells.x || gy >= gridSizeInCells.y) return false;
        gridPos = new Vector2Int(gx, gy);
        return true;
    }

    public bool IsPollutedAtWorld(Vector3 worldPos)
        => WorldToPollutionGrid(worldPos, out var gp) && PollutionGrid.IsPolluted(gp);

    public void CleanAtWorld(Vector3 worldPos)
    {
        if (WorldToPollutionGrid(worldPos, out var gp)) PollutionGrid.CleanTile(gp);
    }

    public void PolluteAtWorld(Vector3 worldPos)
    {
        if (WorldToPollutionGrid(worldPos, out var gp)) PollutionGrid.PolluteTile(gp);
    }

    public void ToggleAtWorld(Vector3 worldPos)
    {
        if (WorldToPollutionGrid(worldPos, out var gp)) PollutionGrid.Toggle(gp);
    }

    public void CleanAtCell(Vector3Int cell)
    {
        var gp = new Vector2Int(cell.x - bounds.xMin, cell.y - bounds.yMin);
        PollutionGrid.CleanTile(gp);
    }

    public void PolluteAtCell(Vector3Int cell)
    {
        var gp = new Vector2Int(cell.x - bounds.xMin, cell.y - bounds.yMin);
        PollutionGrid.PolluteTile(gp);
    }

    public void ToggleAtCell(Vector3Int cell)
    {
        var gp = new Vector2Int(cell.x - bounds.xMin, cell.y - bounds.yMin);
        PollutionGrid.Toggle(gp);
    }

    public void CleanArea(Vector3Int cellMinInclusive, Vector3Int cellMaxInclusive)
        => IterateArea(cellMinInclusive, cellMaxInclusive, PollutionGrid.CleanTile);

    public void PolluteArea(Vector3Int cellMinInclusive, Vector3Int cellMaxInclusive)
        => IterateArea(cellMinInclusive, cellMaxInclusive, PollutionGrid.PolluteTile);

    public void ToggleArea(Vector3Int cellMinInclusive, Vector3Int cellMaxInclusive)
        => IterateArea(cellMinInclusive, cellMaxInclusive, PollutionGrid.Toggle);

    public void SetAllPollute(bool isPolluted)
    {
        PollutionGrid.SetAll(isPolluted);
        RefreshAllTiles();
        UpdateCleanRatioUI();
    }

    private void IterateArea(Vector3Int minCell, Vector3Int maxCell, System.Action<Vector2Int> action)
    {
        int minX = Mathf.Min(minCell.x, maxCell.x);
        int maxX = Mathf.Max(minCell.x, maxCell.x);
        int minY = Mathf.Min(minCell.y, maxCell.y);
        int maxY = Mathf.Max(minCell.y, maxCell.y);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var baseCell = new Vector3Int(x, y, 0);
                if (!baseTilemap.HasTile(baseCell)) continue;
                var gp = new Vector2Int(x - bounds.xMin, y - bounds.yMin);
                action?.Invoke(gp);
            }
        }
    }

    public float GetCleanRatio() => PollutionGrid.GetCleanRatio();

    public static int CalculateVisibleTileCountX(Camera cam, int tilePixelSize, int pixelsPerUnit)
    {
        if (cam == null || !cam.orthographic || tilePixelSize <= 0 || pixelsPerUnit <= 0) return 0;
        float vertWorldUnits = cam.orthographicSize * 2f;            // 세로 월드 유닛
        float horizWorldUnits = vertWorldUnits * cam.aspect;         // 가로 월드 유닛
        float tileWorldSize = (float)tilePixelSize / pixelsPerUnit;  // 한 타일의 월드 유닛 크기
        return Mathf.FloorToInt(horizWorldUnits / tileWorldSize);
    }
    
    public void SpawnTrash(Vector3 worldPos)
    {
        if (trashPool == null) return;
        var obj = trashPool.Spawn(worldPos, Quaternion.identity);
        if (obj == null) return;
        var trash = obj.GetComponent<TrashObject>();
        if (trash != null)
        {
            trash.onDestroyed.AddListener(() => trashPool.Despawn(obj));
        }
    }

    public void TestRandomTrashSpawn()
    {
        if (baseTilemap == null || trashPool == null) return;
        int attempts = 10;
        for (int i = 0; i < attempts; i++)
        {
            int rx = Random.Range(0, gridSizeInCells.x);
            int ry = Random.Range(0, gridSizeInCells.y);
            var cell = new Vector3Int(rx + bounds.xMin, ry + bounds.yMin, 0);
            if (!baseTilemap.HasTile(cell)) continue;
            var worldPos = baseTilemap.GetCellCenterWorld(cell);
            SpawnTrash(worldPos);
        }
        
    }
}
