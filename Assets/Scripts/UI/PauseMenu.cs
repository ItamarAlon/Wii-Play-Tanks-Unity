using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        private bool paused;
        public event EventHandler Toggled;

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
            OnToggled(paused);
        }

        public void OnResume() => Toggle();
        public void OnQuitToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }

        protected virtual void OnToggled(EventArgs<bool> e)
        {
            Toggled?.Invoke(this, e);
        }
    }
}
