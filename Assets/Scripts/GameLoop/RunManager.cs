using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Gameplay.Level;
using Game.UI;
using System.Collections;

namespace Game.GameLoop
{
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;
        [SerializeField] private float previewTimeSeconds = 3f;
        [SerializeField] GameObject[] StageList;
        //public LevelLoader levelLoader;
        [SerializeField] HUDController hud;
        [SerializeField] StageManager stageManager;
        [SerializeField] StageBanner banner;
        [SerializeField] PauseMenu pauseMenu;

        public int CurrentLives { get; private set; }
        public int TotalKills { get; private set; }
        public int CurrentStageIndex { get; private set; } = 0;
        
        private int CurrentStageNum { get => CurrentStageIndex + 1; }
        private bool inStagePreview = false;

        void Awake()
        {
            pauseMenu.Toggled += PauseMenu_Toggled;
        }

        private void PauseMenu_Toggled(object sender, System.EventArgs e)
        {
            bool wasPaused = e as EventArgs<bool>;
            stageManager.SetGameplayEnabled(!wasPaused);
        }

        void Start()
        {
            deactivateAllStages();

            CurrentLives = startingLives;
            TotalKills = 0;
            LoadStage(CurrentStageIndex, true);
            RefreshHUD();

            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Bullet"), LayerMask.NameToLayer("Holes"));
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
            TotalKills++; 
            RefreshHUD(); 
        }

        public void OnPlayerDied()
        {
            CurrentLives--;
            if (CurrentLives <= 0)
            {
                SceneManager.LoadScene("Title");
                return;
            }
            LoadStage(CurrentStageIndex, false);
            RefreshHUD();
        }

        public void OnStageCleared()
        {
            StageList[CurrentStageIndex].SetActive(false);
            CurrentStageIndex++;
            if (CurrentStageIndex >= StageList.Length)
            {
                SceneManager.LoadScene("Title");
                return;
            }
            LoadStage(CurrentStageIndex, true);
            RefreshHUD();
        }

        private void LoadStage(int idx, bool firstTimeLoading)
        {
            var currentStage = StageList[idx];
            currentStage.SetActive(true);
            stageManager.BeginStage(currentStage, firstTimeLoading);

            StartCoroutine(StageRoutine());
        }

        private void RefreshHUD()
        {
            if (!hud) return;
            hud.SetLives(CurrentLives);
            hud.SetStage(CurrentStageIndex + 1);
            hud.SetKills(TotalKills);
        }

        private IEnumerator StageRoutine()
        {
            stageManager.SetGameplayEnabled(false);
            inStagePreview = true;
            float t = previewTimeSeconds;
            while (t > 0f)
            {
                if (banner)
                    banner.Show(CurrentStageNum, t);
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (banner) 
                banner.Hide();
            inStagePreview = false;
            stageManager.SetGameplayEnabled(true);
        }
    }
}
