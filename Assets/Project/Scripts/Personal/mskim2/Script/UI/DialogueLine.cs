using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    [Tooltip("화자 이름(비어있으면 표시 안 함)")] public string speaker;

    [Tooltip("본문(Text Animator 태그 사용 가능)")] [TextArea(2, 5)] public string text;

    [Tooltip("타자 속도 배율 (1 = 기본). 0 이하면 1 로 처리")] public float speedMultiplier;

    [Tooltip("타이핑 완료 후 자동 진행 대기 시간(0 이하면 수동 진행)")] public float autoAdvanceDelay;

    [Tooltip("이 라인 재생 직전에 실행할 SFX Key (선택)")] public string sfxKey;

    public bool HasSpeaker => !string.IsNullOrWhiteSpace(speaker);
}

