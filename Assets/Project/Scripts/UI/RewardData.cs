using System;
using UnityEngine;

[Serializable]
public class RewardData
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;
    public Sprite icon;
}

