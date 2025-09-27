using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class EnemyHPDisplayUI : MonoBehaviour
{
    private float maxHP;
    private float currentHP;
    private Image fillImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fillImage = GetComponent<Image>();
    }

    public void Initialize(float maxHPAmount)
    {
        maxHP = maxHPAmount;
        SetHP(maxHP);
    }
    
    public void SetHP(float hp)
    {
        currentHP = Mathf.Clamp(hp, 0f, maxHP);
        UpdateBar();
    }

    void UpdateBar()
    {
        fillImage.fillAmount = currentHP / maxHP;
    }
}
