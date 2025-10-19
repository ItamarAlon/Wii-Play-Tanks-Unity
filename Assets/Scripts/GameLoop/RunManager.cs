using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Gameplay.Level;
using Game.UI;
using System.Collections;
using System;
using Assets.Scripts.Core;

namespace Game.GameLoop
{
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;
        [SerializeField] private float previewTimeSeconds = 3f;
        [SerializeField] GameObject[] StageList;
        [SerializeField] HUDController hud;
        [SerializeField] StageManager stageManager;
        [SerializeField] StageBanner banner;
        [SerializeField] PauseMenu pauseMenu;

        public int CurrentLives { get; private set; }
        public int TotalKills { get; private set; }
        public int CurrentStageIndex { get; private set; } = 0;
        
        private int CurrentStageNum => CurrentStageIndex + 1;
        private bool allowStagePreview = true;
        private bool inStagePreview = false;

        public event EventHandler GameEnded;

        void Awake()
        {
            TitleMenu titleMenu = FindFirstObjectByType<TitleMenu>();
            pauseMenu.ToggleRequested += PauseMenu_ToggleRequested;
        }

        private void PauseMenu_ToggleRequested(object sender, EventArgs e)
        {
            bool wasPaused = e as EventArgs<bool>;
            toggleGameplayAndStagePreview(wasPaused);
        }

        private void toggleGameplayAndStagePreview(bool wasPaused)
        {
            stageManager.SetGameplayEnabled(!wasPaused, true);
            toggleStagePreview(!wasPaused);
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
                endGame();
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
                endGame();
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

        private void toggleStagePreview(bool? enablePreview = null)
        {
            if (enablePreview.HasValue)
                allowStagePreview = enablePreview.Value;
            else
                allowStagePreview = !allowStagePreview;
        }

        private IEnumerator StageRoutine()
        {
            stageManager.SetGameplayEnabled(false);
            inStagePreview = true;
            float t = previewTimeSeconds;
            while (t > 0f)
            {
                while (!allowStagePreview)
                    yield return null;

                if (banner)
                    banner.Show(CurrentStageNum, t);
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (banner) 
                banner.Hide();
            inStagePreview = false;
            allowStagePreview = true;
            stageManager.SetGameplayEnabled(true);
        }

        private void endGame()
        {
            General.LoadMainMenu();
            OnGameEnded(EventArgs.Empty);
        }

        protected virtual void OnGameEnded(EventArgs e)
        {
            GameEnded?.Invoke(this, e);
        }
    }
}
