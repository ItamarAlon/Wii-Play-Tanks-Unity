using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        private bool paused = false;
        public event EventHandler ToggleRequested;
        public event EventHandler RestartRequested;
        public event EventHandler MainMenuRequested;
        public event EventHandler QuitGameRequested;

        void Awake()
        {
            panel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) 
                Toggle();
        }

        public void Toggle()
        {
            paused = !paused;
            if (panel) 
                panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            OnToggleRequested(paused);
        }

        protected virtual void OnToggleRequested(EventArgs<bool> e)
        {
            ToggleRequested?.Invoke(this, e);
        }
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

        //Buttons//
        public void OnPause() => Toggle();
        public void OnResume() => Toggle();
        public void OnRestart() => OnRestartRequested(EventArgs.Empty);
        public void OnQuitGame() => OnQuitGameRequested(EventArgs.Empty);
        public void OnMainMenu() => OnMainMenuRequested(EventArgs.Empty);
        //{
        //    Time.timeScale = 1f;
        //    SceneManager.LoadScene("Title");
        //}
    }
}
