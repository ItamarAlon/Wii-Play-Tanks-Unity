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

        //Buttons//
        public void OnPause() => Toggle();
        public void OnResume() => Toggle();
        public void OnRestart() => OnRestartRequested(EventArgs.Empty);
        public void OnQuitGame() => Application.Quit();
        public void OnMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }
    }
}
