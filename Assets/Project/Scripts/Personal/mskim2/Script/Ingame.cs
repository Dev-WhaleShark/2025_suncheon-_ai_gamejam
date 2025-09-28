using UnityEngine;
using WhaleShark.Gameplay;

public class Ingame : MonoBehaviour
{
    [SerializeField] private UIAnimatedPanel gameOverUI;
    [SerializeField] private UIAnimatedPanel gameClearUI;

    [SerializeField] private UIAnimatedPanel gameGuideUI;
    [SerializeField] private UIAnimatedPanel gameMapUI;

    public StageManager stageManager;

    private void Start()
    {
        gameGuideUI.Show();

        WhaleShark.Core.EventBus.PlayerDied += ShowGameOver;
        WhaleShark.Core.EventBus.GameCleared += ShowGameClear;

        GameManager.Instance.IsGameStarted = true;
    }

    private void OnDestroy()
    {
        WhaleShark.Core.EventBus.PlayerDied -= ShowGameOver;
        WhaleShark.Core.EventBus.GameCleared -= ShowGameClear;
    }

    public void ShowGameOver()
    {
        GameManager.Instance.IsGameStarted = false;
        gameOverUI.Show();
    }

    public void ShowGameClear()
    {
        GameManager.Instance.IsGameStarted = false;
        gameClearUI.Show();
    }

    public void RestartGame()
    {
        GameManager.Instance.LoadScene("InGame");
    }

    public void GoMainMenu()
    {
        GameManager.Instance.LoadScene("MainMenu");
    }

    public void HideGuide()
    {
        gameGuideUI.Hide();
        stageManager.StartStage();
    }
}
