using System;
using UnityEngine;

[Flags]
public enum TileState
{
    Clean     = 0,
    Pollution = 1 << 0,
    Trash     = 1 << 1
}

/// <summary>
/// 맵을 Grid 단위로 상태(Clean/Polluted/Trash) 관리 (타일맵 비사용)
/// </summary>
public class MapGrid
{
    private TileState[,] _states;
    public Vector2Int GridSize { get; private set; }
    public bool IsInitialized => _states != null;

    public event Action<Vector2Int, TileState> OnTileStateChanged;

    /// <summary>
    /// size 크기 초기화 (모든 셀 Clean)
    /// </summary>
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

    public bool InBounds(Vector2Int p) => IsInitialized && p.x >= 0 && p.y >= 0 && p.x < GridSize.x && p.y < GridSize.y;

    private bool Validate(Vector2Int p)
    {
        if (!IsInitialized) { Debug.LogWarning("MapGrid not initialized"); return false; }
        if (!InBounds(p)) return false;
        return true;
    }

    /// <summary>
    /// 해당 셀의 전체 상태 반환 (Out of Bounds 시 Clean)
    /// </summary>
    public TileState GetTileState(Vector2Int p)
    {
        if (!Validate(p)) return TileState.Clean;
        return _states[p.x, p.y];
    }

    private void SetTileStateInternal(Vector2Int p, TileState newState)
    {
        if (!Validate(p)) return;
        var prev = _states[p.x, p.y];
        if (prev == newState) return;
        _states[p.x, p.y] = newState;
        OnTileStateChanged?.Invoke(p, newState);
    }

    public void SetFlag(Vector2Int p, TileState flag, bool enable)
    {
        if (!Validate(p)) return;
        var cur = _states[p.x, p.y];
        var next = enable ? (cur | flag) : (cur & ~flag);
        SetTileStateInternal(p, next);
    }

    public void SetPollution(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Pollution, enable);
    public void SetTrash(Vector2Int p, bool enable = true) => SetFlag(p, TileState.Trash, enable);
    public void CleanTile(Vector2Int p) => SetTileStateInternal(p, TileState.Clean);

    public bool HasPollution(Vector2Int p) => (GetTileState(p) & TileState.Pollution) != 0;
    public bool HasTrash(Vector2Int p) => (GetTileState(p) & TileState.Trash) != 0;

    /// <summary>
    /// 전체를 polluted 값으로 일괄 세팅 (true = 모두 Polluted, false = 모두 Clean)
    /// </summary>
    public void SetAllPollution(bool enable)
    {
        if (!IsInitialized) return;
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                var cell = new Vector2Int(x, y);
                SetPollution(cell, enable);
            }
    }

    public void SetAllClean()
    {
        if (!IsInitialized) return;
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
                SetTileStateInternal(new Vector2Int(x, y), TileState.Clean);
    }

    /// <summary>
    /// 정화된 타일 비율(0~1)
    /// </summary>
    public float GetCleanRatio()
    {
        if (!IsInitialized) return 0f;
        int total = GridSize.x * GridSize.y;

        if (total == 0)
            return 0f;

        int clean = 0;
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
                if (_states[x, y] == TileState.Clean) clean++;
        return (float)clean / total;
    }
}
