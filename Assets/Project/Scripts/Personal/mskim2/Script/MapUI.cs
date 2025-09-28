using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    public List<Sprite> mapSprites; // 맵 이미지들을 저장할 리스트
    public StageManager stageManager;
    public Image renderer;
    public UIAnimatedPanel animatedPanel;
    public void Start()
    {
        stageManager.OnStageCleared += (idx, stage) =>
        {
            UpdateMapImage(idx);
        };
    }

    public void UpdateMapImage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= mapSprites.Count)
        {
            Debug.LogWarning("Invalid stage index for map image.");
            return;
        }

        renderer.sprite = mapSprites[stageIndex];
    }
}
