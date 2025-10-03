// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public GameObject panel;
        private bool _paused;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        public void Toggle()
        {
            _paused = !_paused;
            if (panel) panel.SetActive(_paused);
            Time.timeScale = _paused ? 0f : 1f;
        }

        public void OnResume() => Toggle();
        public void OnQuitToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }
    }
}
