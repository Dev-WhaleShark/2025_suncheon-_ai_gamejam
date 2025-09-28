using DG.Tweening;
using Febucci.UI.Core;
using UnityEngine;
using UnityEngine.UI;

public class PurifyUI : MonoBehaviour
{
    private float purifyProgress; // 0 - 1

    public Image purifyBar;
    public Image contaminentBar;
    public Image progressDisplayObject;

    private Vector2 range;
    private float length;

    public Sprite happyFace;
    public Sprite uglyFace;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        range = new Vector2(purifyBar.rectTransform.anchorMin.x, purifyBar.rectTransform.anchorMax.x);
        length = range.y - range.x;
    }

    public void UpdatePurifyProgress(float progress)
    {
        purifyProgress = progress / 100.0f;

        purifyBar.fillAmount = purifyProgress;
        contaminentBar.fillAmount = 1.0f - purifyProgress;

        progressDisplayObject.GetComponent<RectTransform>().anchoredPosition.Set(range.x + purifyProgress * length, purifyBar.rectTransform.anchoredPosition.y);

        RectTransform rt = purifyBar.rectTransform;
        float x = rt.rect.xMin + rt.rect.width * purifyBar.fillAmount;
        Vector3 world = rt.TransformPoint(new Vector3(x, rt.rect.center.y, 0));

        if (world.x < 0 && progressDisplayObject.transform.position.x > 0)
        {
            progressDisplayObject.sprite = uglyFace;
        }
        else if (world.x > 0 && progressDisplayObject.transform.position.x < 0)
        { 
            progressDisplayObject.sprite = happyFace;
        }


        progressDisplayObject.transform.position = world;

        
    }
}
