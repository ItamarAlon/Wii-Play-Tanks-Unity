using System;
using UnityEngine;

public class TitleMenu : MonoBehaviour
{
    public event EventHandler StartGameRequested;
    public event EventHandler QuitGameRequested;

    public void OnStartGame() => OnStartGameRequested(EventArgs.Empty);
    public void OnQuitGame() => OnQuitGameRequested(EventArgs.Empty);

    protected virtual void OnStartGameRequested(EventArgs e)
    {
        StartGameRequested?.Invoke(this, e);
    }

    protected virtual void OnQuitGameRequested(EventArgs e)
    {
        QuitGameRequested?.Invoke(this, e);
    }
}