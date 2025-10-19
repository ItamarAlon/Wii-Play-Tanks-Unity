using System;
using UnityEngine;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMPro.TMP_Text killCountText;

    private bool isActive = false;
    public event EventHandler RestartRequested;
    public event EventHandler MainMenuRequested;
    public event EventHandler QuitGameRequested;

    void Awake()
    {
        if (panel)
            panel.SetActive(false);
        if (killCountText)
            killCountText.text = String.Empty;
    }

    public void Toggle()
    {
        isActive = !isActive;
        if (panel)
            panel.SetActive(isActive);
        Time.timeScale = isActive ? 0.4f : 1f;
    }

    public void SetKills(int tanksDestroyed)
    {
        if (!killCountText) return;
        string congratulations = String.Empty;

        if (tanksDestroyed > 1)
            congratulations = "Great Job!";
        else
            congratulations = "You Suck";

        killCountText.text = $"You've destroyed {tanksDestroyed} Tanks\r\n{congratulations}";
    }

    public void OnRestart() => OnRestartRequested(EventArgs.Empty);
    public void OnQuitGame() => OnQuitGameRequested(EventArgs.Empty);
    public void OnMainMenu() => OnMainMenuRequested(EventArgs.Empty);

    protected virtual void OnRestartRequested(EventArgs e)
    {
        RestartRequested?.Invoke(this, e);
    }
    protected virtual void OnMainMenuRequested(EventArgs e)
    {
        MainMenuRequested?.Invoke(this, e);
    }
    protected virtual void OnQuitGameRequested(EventArgs e)
    {
        QuitGameRequested?.Invoke(this, e);
    }
}
