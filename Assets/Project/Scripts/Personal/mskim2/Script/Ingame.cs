using System;
using Unity.VisualScripting;
using UnityEngine;
using WhaleShark.Gameplay;

public class Ingame : MonoBehaviour
{
    public CanvasGroup gameOverUI;
    public CanvasGroup gameClearUI;

    private void Start()
    {
        WhaleShark.Core.EventBus.PlayerDied += ShowGameOver;
        WhaleShark.Core.EventBus.GameCleared += ShowGameClear;

        gameOverUI.alpha = 0;
        gameOverUI.interactable = false;
        gameOverUI.blocksRaycasts = false;

        gameClearUI.alpha = 0;
        gameClearUI.interactable = false;
        gameClearUI.blocksRaycasts = false;

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

        gameOverUI.gameObject.SetActive(true);
        gameOverUI.alpha = 1;
        gameOverUI.interactable = true;
        gameOverUI.blocksRaycasts = true;
    }

    public void ShowGameClear()
    {
        GameManager.Instance.IsGameStarted = false;

        gameClearUI.gameObject.SetActive(true);
        gameClearUI.alpha = 1;
        gameClearUI.interactable = true;
        gameClearUI.blocksRaycasts = true;
    }

    public void RestartGame()
    {
        GameManager.Instance.LoadScene("InGame");
    }

    public void GoMainMenu()
    {
        GameManager.Instance.LoadScene("MainMenu");
    }


}
