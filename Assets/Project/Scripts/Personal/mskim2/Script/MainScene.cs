using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WhaleShark.Gameplay;
using DG.Tweening; // DOTween 추가

public class MainScene : MonoBehaviour
{
    public CanvasGroup guidePanel;
    public RectTransform guideRect;

    public Button startButton;
    public Button guideButton;
    public Button guideCloseButton;

    // --- Guide Panel Animation Settings ---
    [Header("Guide Panel Animation")]
    public float guideFadeDuration = 0.25f;
    public float guideScaleDuration = 0.4f;
    [Range(0.5f,1f)] public float guideOpenStartScale = 0.9f;
    [Range(0.5f,1f)] public float guideCloseEndScale = 0.9f;
    public Ease guideOpenScaleEase = Ease.OutBack;
    public Ease guideCloseScaleEase = Ease.InCubic;
    public Ease guideOpenFadeEase = Ease.OutQuad;
    public Ease guideCloseFadeEase = Ease.InQuad;
    public bool useUnscaledTime = true;

    private Sequence guideSeq;
    private bool guideOpen;

    // Hover animation state (internal only)
    private readonly Dictionary<Transform, Vector3> _origScale = new();
    private readonly Dictionary<Transform, Tween> _hoverTweens = new();
    private const float _hoverScaleFactor = 1.06f;
    private const float _hoverDuration = 0.18f;

    public void Awake()
    {
        startButton.onClick.AddListener(StartPrologue);
        guideButton.onClick.AddListener(OpenGuide);
        guideCloseButton.onClick.AddListener(CloseGuide);

        if (guidePanel != null)
        {
            guidePanel.gameObject.SetActive(false);
            guideOpen = false;
        }

        // Hover scaling setup
        SetupHover(startButton);
        SetupHover(guideButton);
        SetupHover(guideCloseButton);
    }

    public void OnDestroy()
    {
        startButton.onClick.RemoveListener(StartPrologue);
        guideButton.onClick.RemoveListener(OpenGuide);
        guideCloseButton.onClick.RemoveListener(CloseGuide);
        KillGuideSequence();

        // Kill hover tweens
        foreach (var kv in _hoverTweens)
        {
            if (kv.Value != null && kv.Value.IsActive()) kv.Value.Kill();
        }
        _hoverTweens.Clear();
    }

    public void StartPrologue()
    {
        GameManager.Instance.LoadScene("Prologue");
    }

    public void OpenGuide()
    {
        if (guidePanel == null) return;
        if (guideOpen) return; // 이미 열림
        guidePanel.gameObject.SetActive(true);
        KillGuideSequence();

        if (guideRect != null)
        {
            guideRect.localScale = Vector3.one * guideOpenStartScale;
        }
        guidePanel.alpha = 0f;
        guidePanel.interactable = false;
        guidePanel.blocksRaycasts = false;

        guideSeq = DOTween.Sequence();
        guideSeq.SetUpdate(useUnscaledTime);

        if (guideRect != null)
        {
            guideSeq.Join(guideRect.DOScale(1f, guideScaleDuration).SetEase(guideOpenScaleEase));
        }
        guideSeq.Join(guidePanel.DOFade(1f, guideFadeDuration).SetEase(guideOpenFadeEase));
        guideSeq.OnComplete(() =>
        {
            guidePanel.interactable = true;
            guidePanel.blocksRaycasts = true;
            guideOpen = true;
            guideSeq = null;
        });
    }

    public void CloseGuide()
    {
        if (guidePanel == null) return;
        if (!guideOpen) return; // 이미 닫힘
        KillGuideSequence();

        guidePanel.interactable = false;
        guidePanel.blocksRaycasts = false;

        guideSeq = DOTween.Sequence();
        guideSeq.SetUpdate(useUnscaledTime);

        if (guideRect != null)
        {
            guideSeq.Join(guideRect.DOScale(guideCloseEndScale, guideScaleDuration).SetEase(guideCloseScaleEase));
        }
        guideSeq.Join(guidePanel.DOFade(0f, guideFadeDuration).SetEase(guideCloseFadeEase));
        guideSeq.OnComplete(() =>
        {
            guidePanel.gameObject.SetActive(false);
            guideOpen = false;
            guideSeq = null;
        });
    }

    private void KillGuideSequence()
    {
        if (guideSeq != null && guideSeq.IsActive())
        {
            guideSeq.Kill();
            guideSeq = null;
        }
    }

    private void SetupHover(Button btn)
    {
        if (btn == null) return;
        var tr = btn.transform;
        if (!_origScale.ContainsKey(tr)) _origScale.Add(tr, tr.localScale);

        // Ensure EventTrigger component
        var trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        AddTriggerEvent(trigger, UnityEngine.EventSystems.EventTriggerType.PointerEnter, (e) => HoverEnter(tr));
        AddTriggerEvent(trigger, UnityEngine.EventSystems.EventTriggerType.PointerExit, (e) => HoverExit(tr));
        AddTriggerEvent(trigger, UnityEngine.EventSystems.EventTriggerType.PointerDown, (e) => HoverExit(tr)); // 클릭 시 약간 줄였다가 재호버 시 다시 커지도록
        btn.onClick.AddListener(() => HoverExit(tr));
    }

    private void AddTriggerEvent(UnityEngine.EventSystems.EventTrigger trigger, UnityEngine.EventSystems.EventTriggerType type, System.Action<UnityEngine.EventSystems.BaseEventData> callback)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData>(callback));
        trigger.triggers.Add(entry);
    }

    private void HoverEnter(Transform tr)
    {
        if (tr == null) return;
        Tween t;
        if (_hoverTweens.TryGetValue(tr, out t))
        {
            if (t != null && t.IsActive()) t.Kill();
        }
        var targetScale = _origScale.TryGetValue(tr, out var baseScale) ? baseScale * _hoverScaleFactor : tr.localScale * _hoverScaleFactor;
        _hoverTweens[tr] = tr.DOScale(targetScale, _hoverDuration).SetEase(Ease.OutQuad);
    }

    private void HoverExit(Transform tr)
    {
        if (tr == null) return;
        Tween t;
        if (_hoverTweens.TryGetValue(tr, out t))
        {
            if (t != null && t.IsActive()) t.Kill();
        }
        var baseScale = _origScale.TryGetValue(tr, out var s) ? s : Vector3.one;
        _hoverTweens[tr] = tr.DOScale(baseScale, _hoverDuration).SetEase(Ease.InQuad);
    }
}
