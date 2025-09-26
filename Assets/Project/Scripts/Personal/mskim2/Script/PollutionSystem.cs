// using UnityEngine;
//
// public class PollutionSystem : MonoBehaviour
// {
//     public PollutionTilemapManager tilemapManager;
//     private PollutionGrid currentGrid;
//     
//     // 적이 이동할 때 오염 확산
//     public void SpreadPollutionFromEnemy(Vector2 worldPos)
//     {
//         Vector2Int gridPos = WorldToGridPosition(worldPos);
//         
//         // 3x3 범위 오염
//         for (int x = -1; x <= 1; x++)
//         {
//             for (int y = -1; y <= 1; y++)
//             {
//                 Vector2Int targetPos = gridPos + new Vector2Int(x, y);
//                 if (IsValidGridPosition(targetPos))
//                 {
//                     int currentPollution = currentGrid.GetPollution(targetPos);
//                     currentGrid.SetPollution(targetPos, currentPollution + 1);
//                 }
//             }
//         }
//         
//         tilemapManager.UpdatePollutionVisualization(currentGrid);
//     }
//     
//     // 플레이어 정화 행동
//     public void PurifyArea(Vector2 worldPos, int range)
//     {
//         Vector2Int gridPos = WorldToGridPosition(worldPos);
//         
//         for (int x = -range; x <= range; x++)
//         {
//             for (int y = -range; y <= range; y++)
//             {
//                 Vector2Int targetPos = gridPos + new Vector2Int(x, y);
//                 if (IsValidGridPosition(targetPos))
//                 {
//                     int currentPollution = currentGrid.GetPollution(targetPos);
//                     currentGrid.SetPollution(targetPos, Mathf.Max(0, currentPollution - 1));
//                 }
//             }
//         }
//         
//         tilemapManager.UpdatePollutionVisualization(currentGrid);
//     }
//     
//     // 오염도에 따른 플레이어 디버프
//     public void ApplyPollutionEffects(Vector2 playerWorldPos)
//     {
//         Vector2Int gridPos = WorldToGridPosition(playerWorldPos);
//         int pollutionLevel = currentGrid.GetPollution(gridPos);
//         
//         PlayerController player = FindObjectOfType<PlayerController>();
//         
//         switch (pollutionLevel)
//         {
//             case 1:
//             case 2:
//                 player.ApplySpeedDebuff(0.1f * pollutionLevel); // 10-20% 감속
//                 break;
//             case 3:
//             case 4:
//                 player.ApplySpeedDebuff(0.3f); // 30% 감속
//                 player.ApplyPoisonDamage(5 * Time.deltaTime); // 독 데미지
//                 break;
//         }
//     }
// }