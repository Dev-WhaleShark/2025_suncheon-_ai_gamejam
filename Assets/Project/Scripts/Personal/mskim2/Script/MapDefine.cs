using System;
using UnityEngine;

/// <summary>
/// 직사각형 타일맵을 2D 배열로 매핑하여 타일 단위 오염도 관리.
/// </summary>
public class PollutionGrid
{
    private bool[,] polluted; // true = 오염, false = 깨끗
    public Vector2Int GridSize { get; private set; }
    public bool IsInitialized => polluted != null;

    /// <summary>
    /// size 크기 초기화.
    /// </summary>
    public void Initialize(Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0)
        {
            Debug.LogError($"PollutionGrid Initialize 실패: 잘못된 크기 {size}");
            return;
        }

        GridSize = size;
        polluted = new bool[size.x, size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                polluted[x, y] = false;
            }
        }
    }

    public bool InBounds(Vector2Int pos) => pos.x >= 0 && pos.y >= 0 && pos.x < GridSize.x && pos.y < GridSize.y;

    private bool Validate(Vector2Int pos)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("PollutionGrid is Not Initialized");
            return false;
        }

        if (!InBounds(pos)) return false;
        return true;
    }

    public bool IsPolluted(Vector2Int pos)
    {
        if (!Validate(pos)) return false;
        return polluted[pos.x, pos.y];
    }

    public void SetPolluted(Vector2Int pos, bool polluted)
    {
        if (!Validate(pos)) return;
        if (this.polluted[pos.x, pos.y] == polluted) return;
        this.polluted[pos.x, pos.y] = polluted;
        OnPollutionChanged?.Invoke(pos, polluted);
    }

    public void PolluteTile(Vector2Int pos) => SetPolluted(pos, true);
    public void CleanTile(Vector2Int pos) => SetPolluted(pos, false);

    public void Toggle(Vector2Int pos)
    {
        if (!Validate(pos)) return;
        SetPolluted(pos, !polluted[pos.x, pos.y]);
    }

    /// <summary>
    /// 전체를 polluted 값으로 일괄 세팅
    /// </summary>
    public void SetAll(bool polluted)
    {
        if (!IsInitialized) return;
        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                if (this.polluted[x, y] != polluted)
                {
                    this.polluted[x, y] = polluted;
                    OnPollutionChanged?.Invoke(new Vector2Int(x, y), polluted);
                }
            }
        }
    }

    /// <summary>
    /// 정화된 타일 비율(0~1)
    /// </summary>
    public float GetCleanRatio()
    {
        if (!IsInitialized) return 0f;
        int total = GridSize.x * GridSize.y;
        if (total == 0) return 0f;
        int clean = 0;
        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                if (!polluted[x, y]) clean++;
            }
        }

        return (float)clean / total;
    }

    public event Action<Vector2Int, bool> OnPollutionChanged; // (그리드좌표, 오염여부)
}
