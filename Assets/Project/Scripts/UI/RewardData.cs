using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "WhaleShark/Game/Reward", fileName = "Reward_Asset")]
public class RewardData : ScriptableObject
{
    [Header("Identity")] public string id;
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;

    // 확장 가능: rarity, value, category 등
    [Header("Meta (Optional)")] public int rarityWeight = 1;

    [Header("Icon Visual")]
    [Tooltip("아이콘 색상을 덮어씁니다 (기본: 흰색 = 원본).")]
    public Color iconTint = Color.white;
    [Tooltip("네이티브 사이즈로 맞춘 뒤 최대 크기 안으로 스케일 다운")]
    public bool useNativeSize = true;
    [Tooltip("useNativeSize가 true일 때 아이콘이 이 최대 영역을 넘으면 축소")]
    public Vector2 maxIconSize = new Vector2(256, 256);
}
