using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Core
{
    public static class General
    {
        public static void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title", LoadSceneMode.Single);
        }

        public static void LoadNewGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }

        public static void QuitGame()
        {
            Application.Quit();
        }
    }
}
