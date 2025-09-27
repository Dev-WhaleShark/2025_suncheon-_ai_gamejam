using System;
using DG.Tweening;
using UnityEngine;
using WhaleShark.Gameplay;

public class Prologue : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueSequence prologueSequence;

    private void Start()
    {
        dialogueUI.StartSequence(prologueSequence);
        dialogueUI.onDialogueEnd.AddListener(() =>
        {
            DOVirtual.DelayedCall(0.5f, () => { GameManager.Instance.LoadScene("InGame"); }, false);
        });
    }
}
