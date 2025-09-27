using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using Febucci.UI.Core;

[DisallowMultipleComponent]
public class DialogueUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypewriterCore typewriter;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Input System")]
    [SerializeField] private InputActionReference advanceAction;
    [SerializeField] private InputActionReference fastForwardAction;

    [Header("Behaviour")]
    [Tooltip("시작 전/종료 후 자동 숨김")]
    [SerializeField] private bool hideWhenIdle = true;
    [SerializeField] private float fadeTime = 0.15f;
    [SerializeField] private bool autoFastForwardWhileHolding = true;
    [SerializeField] private float minHoldTimeForFastForward = 0.35f;

    [Header("Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;
    public UnityEvent<DialogueLine> onLineStart;
    public UnityEvent<DialogueLine> onLineComplete;
    public UnityEvent<string> onPlaySfx;

    private readonly Queue<DialogueLine> _queue = new Queue<DialogueLine>();
    private Coroutine _autoAdvanceCo;
    private Coroutine _fadeCo;

    private DialogueLine _currentLine;
    private bool _hasCurrent;
    private bool _active;
    private bool _initialized;
    private bool _isTyping;
    private float _holdTimer;

    // Reflection targets
    private MethodInfo _miSkip;
    private FieldInfo _fiSpeedMultiplier;

    private static readonly string[] SkipCandidateMethodNames =
    {
        "SkipTypewriter",
        "Skip",
        "CompleteText",
        "ShowRemainingCharacters",
        "SkipTypewriterEffect"
    };

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (hideWhenIdle)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }

        ResolveTypewriter();
        BindTypewriterEvents();
        CacheReflection();
        _initialized = true;
    }

    private void OnEnable()
    {
        if (advanceAction?.action != null) advanceAction.action.Enable();
        if (fastForwardAction?.action != null) fastForwardAction.action.Enable();
        BindTypewriterEvents();
    }

    private void OnDisable()
    {
        if (advanceAction?.action != null) advanceAction.action.Disable();
        if (fastForwardAction?.action != null) fastForwardAction.action.Disable();
        UnbindTypewriterEvents();
    }

    private void Update()
    {
        if (!_active) return;
        HandleInput();
    }

    private void HandleInput()
    {
        if (advanceAction && advanceAction.action != null && advanceAction.action.triggered)
        {
            if (IsTyping()) SkipCurrent();
            else PlayNextLine();
        }

        if (autoFastForwardWhileHolding && fastForwardAction?.action != null)
        {
            if (fastForwardAction.action.IsPressed())
            {
                _holdTimer += Time.unscaledDeltaTime;
                if (_holdTimer >= minHoldTimeForFastForward && IsTyping())
                    SkipCurrent();
            }
            else
            {
                _holdTimer = 0f;
            }
        }
    }

    // Public API
    public void StartSequence(DialogueSequence seq)
    {
        if (seq == null) return;
        StartSequence(seq.lines);
    }

    public void StartSequence(IEnumerable<DialogueLine> lines)
    {
        if (!_initialized) return;

        _queue.Clear();
        foreach (var l in lines) _queue.Enqueue(l);
        if (_queue.Count == 0) return;

        _active = true;
        FadeCanvas(true);
        onDialogueStart?.Invoke();
        PlayNextLine();
    }

    public bool IsActive() => _active;

    public bool IsTyping() => _isTyping;

    public void ForceSkipOrNext()
    {
        if (!_active) return;
        if (IsTyping()) SkipCurrent();
        else PlayNextLine();
    }

    public void ForceEnd()
    {
        if (!_active) return;
        _queue.Clear();
        EndSequence();
    }

    // Internal Flow
    private void PlayNextLine()
    {
        if (_autoAdvanceCo != null)
        {
            StopCoroutine(_autoAdvanceCo);
            _autoAdvanceCo = null;
        }

        if (_queue.Count == 0)
        {
            EndSequence();
            return;
        }

        _currentLine = _queue.Dequeue();
        _hasCurrent = true;

        if (speakerLabel)
            speakerLabel.text = _currentLine.HasSpeaker ? _currentLine.speaker : string.Empty;

        ApplySpeedMultiplier(_currentLine.speedMultiplier <= 0 ? 1f : _currentLine.speedMultiplier);

        if (!string.IsNullOrWhiteSpace(_currentLine.sfxKey))
            onPlaySfx?.Invoke(_currentLine.sfxKey);

        ShowText(_currentLine.text);
        onLineStart?.Invoke(_currentLine);

        if (_currentLine.autoAdvanceDelay > 0f)
            _autoAdvanceCo = StartCoroutine(AutoAdvanceRoutine(_currentLine.autoAdvanceDelay));
    }

    private IEnumerator AutoAdvanceRoutine(float delay)
    {
        while (IsTyping()) yield return null;
        yield return new WaitForSeconds(delay);
        if (_active) PlayNextLine();
    }

    private void SkipCurrent()
    {
        if (!IsTyping()) return;

        if (_miSkip != null && typewriter != null)
        {
            try
            {
                _miSkip.Invoke(typewriter, null);
                return;
            }
            catch { }
        }

        // 폴백: 강제 완료 처리
        _isTyping = false;
        OnTypewriterCompleted();
    }

    private void EndSequence()
    {
        _active = false;
        _hasCurrent = false;
        FadeCanvas(false);
        onDialogueEnd?.Invoke();
    }

    // UI Fade
    private void FadeCanvas(bool show)
    {
        if (canvasGroup == null) return;

        if (!hideWhenIdle)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
            return;
        }

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(show));
    }

    private IEnumerator FadeRoutine(bool show)
    {
        float start = canvasGroup.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = end;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }

    // Typewriter handling
    private void ResolveTypewriter()
    {
        if (typewriter != null) return;
        typewriter = GetComponentInChildren<TypewriterCore>(true);
        if (typewriter == null)
            Debug.LogWarning("[DialogueUI] TypewriterCore 을 찾지 못했습니다.", this);
    }

    private void BindTypewriterEvents()
    {
        if (!typewriter) return;
        typewriter.onTextShowed.RemoveListener(OnTypewriterCompleted);
        typewriter.onTextShowed.AddListener(OnTypewriterCompleted);
    }

    private void UnbindTypewriterEvents()
    {
        if (!typewriter) return;
        typewriter.onTextShowed.RemoveListener(OnTypewriterCompleted);
    }

    private void ShowText(string text)
    {
        if (!typewriter)
        {
            Debug.LogWarning("[DialogueUI] TypewriterCore 없음", this);
            return;
        }
        _isTyping = true;
        typewriter.ShowText(text);
    }

    private void OnTypewriterCompleted()
    {
        if (!_isTyping) return;
        _isTyping = false;
        if (_hasCurrent)
            onLineComplete?.Invoke(_currentLine);
    }

    private void CacheReflection()
    {
        if (!typewriter) return;

        // Skip 메서드 탐색
        foreach (var name in SkipCandidateMethodNames)
        {
            _miSkip = typewriter.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, System.Type.EmptyTypes, null);
            if (_miSkip != null) break;
        }

        // 속도 배율 필드 탐색
        _fiSpeedMultiplier = typewriter.GetType().GetField("speedMultiplier",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void ApplySpeedMultiplier(float mult)
    {
        if (_fiSpeedMultiplier != null && typewriter != null)
        {
            try { _fiSpeedMultiplier.SetValue(typewriter, mult); }
            catch { }
        }
    }
}
