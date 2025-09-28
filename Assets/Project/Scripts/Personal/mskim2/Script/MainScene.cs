using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WhaleShark.Gameplay;

public class MainScene : MonoBehaviour
{
    [SerializeField] private UIAnimatedPanel guideAnimatedPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button guideButton;
    public Button guideCloseButton;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartPrologue);

        if (guideButton != null)
            guideButton.onClick.AddListener(OpenGuide);

        if (guideCloseButton != null)
            guideCloseButton.onClick.AddListener(CloseGuide);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartPrologue);

        if (guideButton != null)
            guideButton.onClick.RemoveListener(OpenGuide);

        if (guideCloseButton != null)
            guideCloseButton.onClick.RemoveListener(CloseGuide);
    }

    private void StartPrologue()
    {
        GameManager.Instance.LoadScene("Prologue");
    }

    public void OpenGuide()
    {
        if (guideAnimatedPanel == null) return;
        guideAnimatedPanel.Show();
    }

    public void CloseGuide()
    {
        if (guideAnimatedPanel == null) return;
        guideAnimatedPanel.Hide();
    }
}
