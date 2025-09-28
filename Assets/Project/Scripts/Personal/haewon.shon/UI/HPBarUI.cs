using DG.Tweening;
using Febucci.UI.Core;
using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    private float maxHP;
    private float currentHP;
    public Image HPBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Initialize(float maxHPAmount)
    {
        maxHP = maxHPAmount;
        UpdateHPBar(maxHPAmount);
    }

    public void UpdateHPBar(float currentHPAmount)
    {
        currentHP = currentHPAmount;
        float rate = currentHP / maxHP;
        HPBar.fillAmount = rate;
    }
}
