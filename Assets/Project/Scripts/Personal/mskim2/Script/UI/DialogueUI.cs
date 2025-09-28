using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using Febucci.UI.Core;
using DG.Tweening;

[DisallowMultipleComponent]
public class DialogueUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypewriterCore typewriter;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private RectTransform speakerPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private InputActionReference advanceAction;

    [Header("Background")]
    [SerializeField] private DialogueBackgroundLayer backgroundLayer;
    [SerializeField] private DialogueBackgroundDatabase backgroundDatabase;
    [SerializeField] private float defaultBackgroundFade = 0.35f;

    [Header("Settings")]
    [SerializeField] private float fadeTime = 0.15f;

    [Header("Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;
    public UnityEvent<DialogueLine> onLineStart;
    public UnityEvent<DialogueLine> onLineComplete;
    public UnityEvent<string> onPlaySfx;
    public UnityEvent<Sprite> onBackgroundChanged; // 배경 변경 시 스프라이트 통지

    // Queue of dialogue lines
    private readonly Queue<DialogueLine> queue = new();

    // State
    private DialogueLine current;
    private bool hasCurrent;
    private bool active;
    private bool initialized;
    [SerializeField]private bool isTyping;
    private float pendingDelay;

    // Reflection (skip & speed)
    private MethodInfo miSkip;
    private FieldInfo fiSpeedMultiplier;
    private static readonly string[] skipMethodNames =
    {
        "SkipTypewriter",
        "Skip",
        "CompleteText",
        "ShowRemainingCharacters",
        "SkipTypewriterEffect"
    };

    private Tween fadeTween;
    private Tween autoAdvanceTween;

    private void Awake()
    {
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        ResolveTypewriter();
        CacheReflection();
        initialized = true;
    }

    private void OnEnable()
    {
        if (advanceAction?.action != null)
        {
            advanceAction.action.performed += OnAdvance;
            advanceAction.action.Enable();
        }

        BindTypewriter();
    }

    private void OnDisable()
    {
        if (advanceAction?.action != null)
        {
            advanceAction.action.performed -= OnAdvance;
            advanceAction.action.Disable();
        }

        UnbindTypewriter();
        KillTweens();
    }

    private void OnAdvance(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() == 0)
        {
            ForceSkipOrNext();
        }
    }

    public void StartSequence(DialogueSequence sequence)
    {
        if (sequence == null)
            return;

        StartSequence(sequence.lines);
    }

    public void StartSequence(IEnumerable<DialogueLine> lines)
    {
        if (!initialized)
            return;

        queue.Clear();

        foreach (var l in lines)
        {
            queue.Enqueue(l);
        }

        if (queue.Count == 0)
            return;

        active = true;
        Fade(true);
        onDialogueStart?.Invoke();
        PlayNextLine();
    }

    public void ForceSkipOrNext()
    {
        if (!active)
            return;

        if (isTyping)
        {
            SkipCurrent();
        }
        else
        {
            PlayNextLine();
        }
    }

    public void ForceEnd()
    {
        if (!active)
            return;

        queue.Clear();
        EndSequence();
    }

    private void PlayNextLine()
    {
        CancelAutoAdvance();

        if (queue.Count == 0)
        {
            EndSequence();
            return;
        }

        current = queue.Dequeue();
        hasCurrent = true;

        if (speakerLabel)
        {
            speakerLabel.text = current.HasSpeaker ? current.speaker : string.Empty;
            speakerLabel.gameObject.SetActive(current.HasSpeaker);
            speakerPanel.gameObject.SetActive(current.HasSpeaker);
        }

        TryApplyBackground(current);

        float speed = current.speedMultiplier <= 0f ? 1f : current.speedMultiplier;
        ApplySpeedMultiplier(speed);

        if (!string.IsNullOrWhiteSpace(current.sfxKey))
        {
            onPlaySfx?.Invoke(current.sfxKey);
        }

        pendingDelay = current.autoAdvanceDelay > 0f ? current.autoAdvanceDelay : 0f;

        ShowText(current.text);
        onLineStart?.Invoke(current);
    }

    private void TryApplyBackground(DialogueLine line)
    {
        if (!line.HasBackgroundChange) return;
        if (backgroundLayer == null) return;
        Sprite target = line.backgroundSprite;
        if (target == null && backgroundDatabase != null && !string.IsNullOrWhiteSpace(line.backgroundKey))
        {
            target = backgroundDatabase.Get(line.backgroundKey);
        }
        if (target == null) return; // 키/스프라이트 둘 다 무효
        float fade = line.backgroundFade > 0f ? line.backgroundFade : defaultBackgroundFade;
        backgroundLayer.Apply(target, fade);
        onBackgroundChanged?.Invoke(target);
    }

    private void SkipCurrent()
    {
        if (!isTyping)
            return;

        // 스킵 시 pendingDelay 유지하여 자동 진행이 정상적으로 동작하도록 함
        CancelAutoAdvance();

        if (miSkip != null && typewriter != null)
        {
            try
            {
                miSkip.Invoke(typewriter, null);
                // TypewriterCore가 즉시 onTextShowed 이벤트를 호출하지 않을 수도 있으니 방어적으로 isTyping=false 처리
                isTyping = false;
                return;
            }
            catch { }
        }

        isTyping = false;
        OnTypewriterCompleted();
    }

    private void OnTypewriterCompleted()
    {
        isTyping = false;

        if (hasCurrent)
        {
            onLineComplete?.Invoke(current);
        }

        // 이전엔 skipFlag 로 스킵 시 auto advance 방지했지만 현재는 스킵 후에도 자동 진행 허용
        if (pendingDelay > 0f && active)
        {
            autoAdvanceTween = DOVirtual.DelayedCall(pendingDelay, () =>
            {
                if (active)
                {
                    PlayNextLine();
                }
            }).SetUpdate(true);
        }
    }

    private void EndSequence()
    {
        active = false;
        hasCurrent = false;
        CancelAutoAdvance();
        Fade(false);
        onDialogueEnd?.Invoke();
    }

    private void Fade(bool show)
    {
        if (!canvasGroup)
            return;

        if (fadeTween != null)
        {
            fadeTween.Kill();
        }

        if (show)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        fadeTween = canvasGroup
            .DOFade(show ? 1f : 0f, fadeTime)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (!show)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            });
    }

    private void CancelAutoAdvance()
    {
        if (autoAdvanceTween != null)
        {
            autoAdvanceTween.Kill();
        }
        autoAdvanceTween = null;
    }

    private void KillTweens()
    {
        if (fadeTween != null)
        {
            fadeTween.Kill();
        }

        if (autoAdvanceTween != null)
        {
            autoAdvanceTween.Kill();
        }

        fadeTween = null;
        autoAdvanceTween = null;
    }

    private void ResolveTypewriter()
    {
        if (typewriter)
            return;

        typewriter = GetComponentInChildren<TypewriterCore>(true);

        if (!typewriter)
        {
            Debug.LogWarning("[DialogueUI] TypewriterCore not found", this);
        }
    }

    private void BindTypewriter()
    {
        if (!typewriter)
            return;

        typewriter.onTextShowed.RemoveListener(OnTypewriterCompleted);
        typewriter.onTextShowed.AddListener(OnTypewriterCompleted);
    }

    private void UnbindTypewriter()
    {
        if (!typewriter)
            return;

        typewriter.onTextShowed.RemoveListener(OnTypewriterCompleted);
    }

    private void ShowText(string text)
    {
        if (!typewriter)
        {
            Debug.LogWarning("[DialogueUI] Missing TypewriterCore", this);
            return;
        }

        isTyping = true;
        typewriter.ShowText(text);
    }

    private void CacheReflection()
    {
        if (!typewriter)
            return;

        foreach (var name in skipMethodNames)
        {
            miSkip = typewriter.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (miSkip != null)
            {
                break;
            }
        }

        fiSpeedMultiplier = typewriter.GetType().GetField(
            "speedMultiplier",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void ApplySpeedMultiplier(float mult)
    {
        if (fiSpeedMultiplier == null || typewriter == null)
            return;

        try
        {
            fiSpeedMultiplier.SetValue(typewriter, mult);
        }
        catch { }
    }
}
