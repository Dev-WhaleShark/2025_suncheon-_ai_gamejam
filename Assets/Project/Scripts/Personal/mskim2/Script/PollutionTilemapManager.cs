using UnityEngine;
using UnityEngine.Tilemaps;

public class PollutionTilemapManager : MonoBehaviour
{
    [Header("오염 타일들")]
    public TileBase cleanTile;           // 깨끗함 (0)
    public TileBase lightPollutionTile;  // 경미 (1)
    public TileBase mediumPollutionTile; // 보통 (2)
    public TileBase heavyPollutionTile;  // 심각 (3)
    public TileBase extremePollutionTile; // 극심 (4)
    
    [Header("타일맵")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public Tilemap pollutionOverlayTilemap; // 오염 오버레이
    
    private PollutionGrid currentPollutionGrid;
    
    public void UpdatePollutionVisualization(PollutionGrid grid)
    {
        currentPollutionGrid = grid;
        
        for (int x = 0; x < grid.gridSize.x; x++)
        {
            for (int y = 0; y < grid.gridSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                int pollutionLevel = grid.GetPollution(new Vector2Int(x, y));
                
                TileBase tileToUse = GetPollutionTile(pollutionLevel);
                pollutionOverlayTilemap.SetTile(tilePos, tileToUse);
            }
        }
    }
    
    private TileBase GetPollutionTile(int level)
    {
        return level switch
        {
            0 => null, // 깨끗하면 오버레이 없음
            1 => lightPollutionTile,
            2 => mediumPollutionTile,
            3 => heavyPollutionTile,
            4 => extremePollutionTile,
            _ => null
        };
    }
}