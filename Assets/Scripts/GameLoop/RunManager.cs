// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Gameplay.Level;
using Game.UI;

namespace Game.GameLoop
{
    public class RunManager : MonoBehaviour
    {
        public int startingLives = 3;
        public int currentLives { get; private set; }
        public int totalKills { get; private set; }
        public int currentStageIndex { get; private set; } = 0;

        public GameObject[] StageList;
        public LevelLoader levelLoader;
        public HUDController hud;
        public StageManager stageManager;

        void Start()
        {
            deactivateAllStages();

            currentLives = startingLives;
            totalKills = 0;
            LoadStage(currentStageIndex, true);
            RefreshHUD();

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("PlayerBullet"), LayerMask.NameToLayer("Holes"));
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("EnemyBullet"), LayerMask.NameToLayer("Holes"));
        }

        private void deactivateAllStages()
        {
            foreach (var stage in StageList)
            {
                stage.SetActive(false);
            }
        }

        public void AddKill() 
        { 
            totalKills++; 
            RefreshHUD(); 
        }

        public void OnPlayerDied()
        {
            currentLives--;
            if (currentLives <= 0)
            {
                SceneManager.LoadScene("Title");
                return;
            }
            LoadStage(currentStageIndex, false);
            RefreshHUD();
        }

        public void OnStageCleared()
        {
            StageList[currentStageIndex].SetActive(false);
            currentStageIndex++;
            if (currentStageIndex >= StageList.Length)
            {
                SceneManager.LoadScene("Title");
                return;
            }
            LoadStage(currentStageIndex, true);
            RefreshHUD();
        }

        private void LoadStage(int idx, bool firstTimeLoading)
        {
            var currentStage = StageList[idx];
            currentStage.SetActive(true);
            stageManager.BeginStage(currentStage, firstTimeLoading);
        }

        private void RefreshHUD()
        {
            if (!hud) return;
            hud.SetLives(currentLives);
            hud.SetStage(currentStageIndex + 1);
            hud.SetKills(totalKills);
        }
    }
}
