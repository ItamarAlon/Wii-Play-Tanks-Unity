// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using System.Collections;
using Game.Gameplay.Level;
using Game.Gameplay.Tanks.Shared;
using Game.Gameplay.Tanks.Enemy;
using Game.UI;
using Game.Gameplay.Effects;
using UnityEngine.SceneManagement;

namespace Game.GameLoop
{
    public class StageManager : MonoBehaviour
    {
        public LevelLoader levelLoader;
        public RunManager run;
        public StageBanner banner;
        public DecalSpawner decals;

        private int aliveEnemiesCount;
        private int enemiesInCurrentStageCount;

        public void LoadGame()
        {
            SceneManager.LoadScene("Game");
        }

        public void BeginStage(GameObject def, bool firstTimeLoading)
        {
            if (firstTimeLoading)               
            {
                levelLoader.Load(def);
                wirePlayerAndEnemies();
            }
            else
            {
                levelLoader.Reload();
                aliveEnemiesCount = enemiesInCurrentStageCount;
            }
        }

        private void wirePlayerAndEnemies()
        {
            var playerH = levelLoader.PlayerInstance.GetComponent<Health>();
            playerH.OnDeath += _ => run.OnPlayerDied();

            aliveEnemiesCount = 0;
            foreach (var e in levelLoader.EnemyInstances)
            {
                var h = e.GetComponent<Health>();
                if (h != null)
                {
                    aliveEnemiesCount++;
                    h.OnDeath += OnEnemyDeath;
                }
            }
            enemiesInCurrentStageCount = aliveEnemiesCount;
        }

        private void OnEnemyDeath(Health h)
        {
            aliveEnemiesCount--;
            run.AddKill();
            if (decals) decals.PlaceX(h.transform.position);
            if (aliveEnemiesCount <= 0)
            {
                run.OnStageCleared();
            }
        }

        //private IEnumerator StageRoutine(LevelDefinition def)
        //{
        //    SetGameplayEnabled(false);
        //    inStagePreview = true;
        //    float t = def.previewSeconds;
        //    while (t > 0f)
        //    {
        //        if (banner) banner.Show(def.stageNumber, t);
        //        t -= Time.unscaledDeltaTime;
        //        yield return null;
        //    }
        //    if (banner) banner.Hide();
        //    inStagePreview = false;
        //    SetGameplayEnabled(true);
        //}

        public void SetGameplayEnabled(bool enabled)
        {
            if (levelLoader.PlayerInstance)
            {
                var playerTank = levelLoader.PlayerInstance.GetComponent<PlayerTankController>();
                if (playerTank) 
                    playerTank.enabled = enabled;
            }
            foreach (var enemyTank in levelLoader.EnemyInstances)
            {
                if (!enemyTank) continue;
                var s1 = enemyTank.GetComponent<StationaryShooterAI>();
                var s2 = enemyTank.GetComponent<MovingShooterAI>();
                var motor = enemyTank.GetComponent<TankMotor>();
                if (s1) s1.enabled = enabled;
                if (s2) s2.enabled = enabled;
                if (motor) motor.enabled = enabled;
            }
        }
    }
}
