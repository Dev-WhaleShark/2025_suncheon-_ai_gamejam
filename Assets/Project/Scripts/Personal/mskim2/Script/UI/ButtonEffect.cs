using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonEffect : MonoBehaviour
{
    private Button btn;
    private Vector3 originalScale;
    public float scaleFactor = 1.1f; // 확대 비율
    public float duration = 0.2f;    // 애니메이션 지속 시간

    private Tween hoverTween;
    private void Awake()
    {
        btn = GetComponent<Button>();
        originalScale = transform.localScale;


        if (btn == null) return;
        var tr = btn.transform;

        // Ensure EventTrigger component
        var trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        AddTriggerEvent(trigger, EventTriggerType.PointerEnter, (e) => HoverEnter(tr));
        AddTriggerEvent(trigger, EventTriggerType.PointerExit, (e) => HoverExit(tr));
        AddTriggerEvent(trigger, EventTriggerType.PointerDown, (e) => HoverExit(tr));
        btn.onClick.AddListener(() => HoverExit(tr));
    }

    private void AddTriggerEvent(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(callback));
        trigger.triggers.Add(entry);
    }


    private void HoverEnter(Transform tr)
    {
        if (tr == null) return;

        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
        }

        var targetScale = originalScale * scaleFactor;
        hoverTween = tr.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
    }

    private void HoverExit(Transform tr)
    {
        if (tr == null) return;

        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
        }

        hoverTween = tr.DOScale(originalScale, duration).SetEase(Ease.InQuad);
    }

    public void OnDestroy()
    {
        if(hoverTween != null && hoverTween.IsActive())
            hoverTween.Kill();
    }
}
