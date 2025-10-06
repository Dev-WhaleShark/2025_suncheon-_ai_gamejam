using System;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

[Flags]
public enum TileState
{
    Clean     = 0,
    Pollution = 1 << 0,
    Trash     = 1 << 1
}

/// <summary>
/// 맵을 Grid 단위로 상태(Clean/Polluted/Trash) 관리
/// </summary>
[Serializable]
public class MapGrid : ISerializationCallbackReceiver
{
    [ShowInInspector, ReadOnly]
    public Vector2Int GridSize { get; private set; }

    [ShowInInspector, OdinSerialize, TableMatrix(SquareCells = true, ResizableColumns = false)]
    private TileState[,] _states;

    public bool IsInitialized => _states != null;
    public event Action<Vector2Int, TileState> OnTileStateChanged;

    /// <summary>
    /// size 크기 초기화 (모든 셀 Clean)
    /// </summary>
    public void Initialize(Vector2Int size)
    {
        GridSize = size;
        _states = new TileState[size.x, size.y]; // 기본 Clean(0)
    }

    public bool InBounds(Vector2Int p) => IsInitialized && p is { x: >= 0, y: >= 0 } && p.x < GridSize.x && p.y < GridSize.y;

    public TileState GetTileState(Vector2Int p) => InBounds(p) ? _states[p.x, p.y] : TileState.Clean;

    public void SetTrash(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Trash, enable);
    public void SetPollution(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Pollution, enable);
    public void CleanTile(Vector2Int p) => SetState(p, TileState.Clean);

    public bool HasTrash(Vector2Int p) => (GetTileState(p) & TileState.Trash) != 0;
    public bool HasPollution(Vector2Int p) => (GetTileState(p) & TileState.Pollution) != 0;

    private void SetFlag(Vector2Int p, TileState flag, bool enable)
    {
        if (!InBounds(p))
            return;

        var cur = _states[p.x, p.y];
        var next = enable ? (cur | flag) : (cur & ~flag);

        if (next != cur)
            SetState(p, next);
    }

    private void SetState(Vector2Int p, TileState s)
    {
        if (!InBounds(p))
            return;

        var prev = _states[p.x, p.y];
        if (prev == s)
            return;

        _states[p.x, p.y] = s;
        OnTileStateChanged?.Invoke(p, s);
    }

    public void SetAllPollution(bool enable)
    {
        if (!IsInitialized)
            return;

        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                SetFlag(new Vector2Int(x, y), TileState.Pollution, enable);
            }
    }

    [Button]
    public void SetAllClean()
    {
        if (!IsInitialized)
            return;

        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                SetState(new Vector2Int(x, y), TileState.Clean);
            }
    }

    /// <summary>
    /// 정화된 타일 비율(0~1)
    /// </summary>
    public float GetCleanRatio()
    {
        if (!IsInitialized)
            return 0f;

        int total = GridSize.x * GridSize.y;

        if (total == 0)
            return 0f;

        int clean = _states.Cast<TileState>().Count(s => s == TileState.Clean);

        return (float)clean / total;
    }

    public void Resize(Vector2Int newSize, bool preserve)
    {
        if (!IsInitialized || !preserve)
        {
            Initialize(newSize);
            return;
        }

        if (newSize == GridSize) return;

        var newStates = new TileState[newSize.x, newSize.y];
        int minX = Mathf.Min(newSize.x, GridSize.x);
        int minY = Mathf.Min(newSize.y, GridSize.y);

        for (int x = 0; x < minX; x++)
            for (int y = 0; y < minY; y++)
                newStates[x, y] = _states[x, y];

        _states = newStates;
        GridSize = newSize;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        GridSize = _states != null ? new Vector2Int(_states.GetLength(0), _states.GetLength(1)) : Vector2Int.zero;
    }
}
