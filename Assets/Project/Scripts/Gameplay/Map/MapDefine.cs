using System;
using System.Collections.Generic;
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
/// 프리팹/에디터 저장을 위해 커스텀 직렬화 구현.
/// </summary>
[Serializable]
public class MapGrid : ISerializationCallbackReceiver
{
    // 런타임 전용 2D 상태 (Unity 직접 직렬화 불가)
    private TileState[,] _states;

    // 직렬화용 플랫 데이터
    [SerializeField] private Vector2Int _serializedSize;
    [SerializeField] private List<int> _serializedData = new(); // (int)TileState 플랫 리스트

    public Vector2Int GridSize { get; private set; }
    public bool IsInitialized => _states != null;
    public bool HasSerializedData => _serializedSize.x > 0 && _serializedSize.y > 0 && _serializedData != null && _serializedData.Count == _serializedSize.x * _serializedSize.y;

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
        SyncSerialized();
    }

    public bool InBounds(Vector2Int p) => IsInitialized && p.x >= 0 && p.y >= 0 && p.x < GridSize.x && p.y < GridSize.y;

    private bool Validate(Vector2Int p)
    {
        if (!IsInitialized)
        {
            // 직렬화 데이터만 있고 아직 Initialize 안 된 경우 재구성 시도
            TryRebuildFromSerialized();
            if (!IsInitialized)
            {
                Debug.LogWarning("MapGrid not initialized");
                return false;
            }
        }
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
        SyncSerialized();
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
                var cur = _states[x, y];
                var has = (cur & TileState.Pollution) != 0;
                if (enable != has)
                {
                    _states[x, y] = enable ? (cur | TileState.Pollution) : (cur & ~TileState.Pollution);
                    OnTileStateChanged?.Invoke(cell, _states[x, y]);
                }
            }
        SyncSerialized();
    }

    public void SetAllClean()
    {
        if (!IsInitialized) return;
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                if (_states[x, y] != TileState.Clean)
                {
                    _states[x, y] = TileState.Clean;
                    OnTileStateChanged?.Invoke(new Vector2Int(x, y), TileState.Clean);
                }
            }
        SyncSerialized();
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

    // ================= Serialization Helpers =================
    private void SyncSerialized()
    {
        if (!IsInitialized) return;
        _serializedSize = GridSize;
        int total = GridSize.x * GridSize.y;
        if (_serializedData == null) _serializedData = new List<int>(total);
        if (_serializedData.Count != total)
        {
            _serializedData.Clear();
            for (int i = 0; i < total; i++) _serializedData.Add(0);
        }
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                int idx = x + y * GridSize.x;
                _serializedData[idx] = (int)_states[x, y];
            }
    }

    private void TryRebuildFromSerialized()
    {
        if (_states != null) return;
        if (_serializedSize.x <= 0 || _serializedSize.y <= 0) return;
        int total = _serializedSize.x * _serializedSize.y;
        if (_serializedData == null || _serializedData.Count != total) return;
        GridSize = _serializedSize;
        _states = new TileState[GridSize.x, GridSize.y];
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                int idx = x + y * GridSize.x;
                _states[x, y] = (TileState)_serializedData[idx];
            }
    }

    public void OnBeforeSerialize()
    {
        SyncSerialized();
    }

    public void OnAfterDeserialize()
    {
        // 실제 배열 재구성 (Play 전 에디터서 호출됨)
        _states = null;
        TryRebuildFromSerialized();
    }

    public void RebuildFromSerializedIfNeeded()
    {
        if (!IsInitialized && HasSerializedData)
        {
            TryRebuildFromSerialized();
        }
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
        SyncSerialized();
    }
}
