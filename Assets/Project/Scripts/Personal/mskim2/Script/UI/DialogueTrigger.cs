using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 트리거(또는 수동 호출)로 DialogueSequence 를 DialogueUI 에 재생시키는 컴포넌트.
/// - playOnStart 가 true 면 Start 시 자동 재생
/// - 2D Trigger 충돌(OnTriggerEnter2D) 또는 3D Trigger(OnTriggerEnter) 사용 가능 (isTrigger Collider 필요)
/// - oneShot 이 true 면 한 번만 재생
/// - 재생 직전/직후 이벤트 제공
/// </summary>
[DisallowMultipleComponent]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private DialogueUI dialogueUI;

    [Header("Behaviour")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool playOnTriggerEnter = false;
    [SerializeField] private bool oneShot = true;
    [SerializeField] private float startDelay = 0f;

    [Tooltip("트리거 태그 필터 (비우면 모든 것 허용)")]
    [SerializeField] private string requiredTag = string.Empty;

    [Header("Events")]
    public UnityEvent onBeforePlay;
    public UnityEvent onAfterPlay; // 첫 라인 재생 직후

    private bool _played;
    private bool _queued;

    private void Start()
    {
        if (playOnStart)
        {
            TryPlay();
        }
    }

    /// <summary>
    /// 외부에서 수동 호출.
    /// </summary>
    public void TryPlay()
    {
        if (oneShot && _played)
        {
            return;
        }

        if (_queued)
        {
            return;
        }

        if (sequence == null || dialogueUI == null)
        {
            Debug.LogWarning("[DialogueTrigger] Missing assignment (sequence or dialogueUI)", this);
            return;
        }

        _queued = true;

        if (startDelay <= 0f)
        {
            PlayInternal();
        }
        else
        {
            Invoke(nameof(PlayInternal), startDelay);
        }
    }

    private void PlayInternal()
    {
        if (oneShot && _played)
        {
            return;
        }

        _played = true;
        _queued = false;
        onBeforePlay?.Invoke();
        dialogueUI.StartSequence(sequence);
        onAfterPlay?.Invoke();
    }

    private bool CheckTag(GameObject other)
    {
        if (string.IsNullOrWhiteSpace(requiredTag))
        {
            return true;
        }

        return other.CompareTag(requiredTag);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!playOnTriggerEnter)
        {
            return;
        }

        if (!CheckTag(other.gameObject))
        {
            return;
        }

        TryPlay();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnTriggerEnter)
        {
            return;
        }

        if (!CheckTag(other.gameObject))
        {
            return;
        }

        TryPlay();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (startDelay < 0f)
        {
            startDelay = 0f;
        }
    }
#endif
}

