using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public enum RoomType
{
    Start,
    Normal,
    Boss,
    Shop,
}

public enum Direction { North, South, East, West }

public class Room
{
    public Vector2Int position; // 방의 위치 (x, y)
    public RoomType type; // 방의 타입 (시작, 일반, 보스, 상점 등)
    public bool visited; // 플레이어가 방문했는지 여부
    public GameObject roomObject; // 방의 게임 오브젝트 (씬에서의 실제 방)

    public Dictionary<Direction, bool> connections;
    public PollutionGrid pollutionGrid; // 오염 데이터 추가
    public bool isCleared;
    public bool isVisited;
}

public class PollutionGrid
{
    private int[,] pollutionLevels;  // 0~4 오염도
    public Vector2Int gridSize;
    
    public int GetPollution(Vector2Int pos) => pollutionLevels[pos.x, pos.y];
    public void SetPollution(Vector2Int pos, int level) => pollutionLevels[pos.x, pos.y] = Mathf.Clamp(level, 0, 4);
}

public class TilemapData
{
    public TileBase[] floorTiles;
    public TileBase[] wallTiles;
    public TileBase[] decorationTiles;
    public Vector3Int[] doorPositions;   // 문 위치들
}