using System;
using UnityEngine;

public class Prologue : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private DialogueSequence prologueSequence;

    private void Start()
    {
        dialogueUI.StartSequence(prologueSequence);
    }
}
