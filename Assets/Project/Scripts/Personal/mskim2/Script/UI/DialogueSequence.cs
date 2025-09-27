using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence", fileName = "DialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    [Tooltip("대사 라인 목록 (순차 재생)")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    public int Count
    {
        get { return lines != null ? lines.Count : 0; }
    }

    public DialogueLine this[int index]
    {
        get { return lines[index]; }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (lines == null)
        {
            lines = new List<DialogueLine>();
        }
    }
#endif
}

