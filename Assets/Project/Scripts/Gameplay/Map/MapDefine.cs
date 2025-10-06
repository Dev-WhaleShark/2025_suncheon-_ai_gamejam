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
    [OdinSerialize, TableMatrix(SquareCells = true, ResizableColumns = false, DrawElementMethod = nameof(DrawCell))]
    private TileState[,] _states;

    [ShowInInspector, ReadOnly]
    public Vector2Int GridSize { get; private set; }

    public bool IsInitialized => _states != null;
    public event Action<Vector2Int, TileState> OnTileStateChanged;

    /// <summary>
    /// size 크기 초기화 (모든 셀 Clean)
    /// </summary>
    [Button(ButtonSizes.Medium)]
    public void Initialize(Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0)
        {
            Debug.LogError($"MapGrid Initialize 실패: 잘못된 크기 {size}");
            return;
        }
        GridSize = size;
        _states = new TileState[size.x, size.y]; // 기본 Clean(0)
    }

    public bool InBounds(Vector2Int p) => IsInitialized && p is { x: >= 0, y: >= 0 } && p.x < GridSize.x && p.y < GridSize.y;

    private bool Validate(Vector2Int p)
    {
        if (!IsInitialized)
            return false;

        return InBounds(p);
    }

    /// <summary>
    /// 해당 셀의 전체 상태 반환 (Out of Bounds 시 Clean)
    /// </summary>
    public TileState GetTileState(Vector2Int p) => Validate(p) ? _states[p.x, p.y] : TileState.Clean;

    private void SetTileStateInternal(Vector2Int p, TileState newState)
    {
        if (!Validate(p))
            return;

        var prev = _states[p.x, p.y];
        if (prev == newState)
            return;

        _states[p.x, p.y] = newState;
        OnTileStateChanged?.Invoke(p, newState);
    }

    public void SetFlag(Vector2Int p, TileState flag, bool enable)
    {
        if (!Validate(p))
            return;

        var cur = _states[p.x, p.y];
        var next = enable ? (cur | flag) : (cur & ~flag);

        SetTileStateInternal(p, next);
    }

    public void SetPollution(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Pollution, enable);
    public void SetTrash(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Trash, enable);
    public void CleanTile(Vector2Int p) => SetTileStateInternal(p, TileState.Clean);

    public bool HasPollution(Vector2Int p) => (GetTileState(p) & TileState.Pollution) != 0;
    public bool HasTrash(Vector2Int p) => (GetTileState(p) & TileState.Trash) != 0;


    [Button]
    public void SetAllPollution(bool enable)
    {
        if (!IsInitialized)
            return;

        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                var cell = new Vector2Int(x, y);
                var cur = _states[x, y];
                var has = (cur & TileState.Pollution) != 0;
                if (enable != has)
                {
                    _states[x, y] = enable ? (cur | TileState.Pollution) : (cur & ~TileState.Pollution);
                    OnTileStateChanged?.Invoke(cell, _states[x, y]);
                }
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
                if (_states[x, y] != TileState.Clean)
                {
                    _states[x, y] = TileState.Clean;
                    OnTileStateChanged?.Invoke(new Vector2Int(x, y), TileState.Clean);
                }
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

    public void Resize(Vector2Int newSize, bool preserveContents)
    {
        if (newSize.x <= 0 || newSize.y <= 0)
        {
            Debug.LogError($"MapGrid Resize 실패: {newSize}");
            return;
        }

        if (!IsInitialized || !preserveContents)
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

    private static TileState DrawCell(Rect r, TileState value)
    {
#if UNITY_EDITOR
        if (GUI.Button(r, ShortLabel(value)))
        {
            value = Next(value);
        }
#endif
        return value;
    }

    private static string ShortLabel(TileState state)
    {
        return state switch
        {
            TileState.Clean => "C",
            TileState.Pollution => "P",
            TileState.Trash => "T",
            TileState.Pollution | TileState.Trash => "PT",
            _ => "?"
        };
    }

    private static TileState Next(TileState state)
    {
        return state switch
        {
            TileState.Clean => TileState.Pollution,
            TileState.Pollution => TileState.Trash,
            TileState.Trash => TileState.Pollution | TileState.Trash,
            TileState.Pollution | TileState.Trash => TileState.Clean,
            _ => TileState.Clean
        };
    }
}
