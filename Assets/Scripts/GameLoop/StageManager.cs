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
        public Transform stageRoot;
        public LevelLoader levelLoader;
        public RunManager run;
        public StageBanner banner;
        public DecalSpawner decals;

        private bool _inPreview;
        private int _aliveEnemies;
        private int _enemiesInCurrentStage;

        public void LoadGame()
        {
            SceneManager.LoadScene("Game");
        }

        public void BeginStage(LevelDefinition def, bool firstTimeLoading)
        {
            StopAllCoroutines();

            if (firstTimeLoading)               
            { 
                levelLoader.Load(def, stageRoot);
                WirePlayerAndEnemies();
            }
            else
            {
                levelLoader.Reload();
                _aliveEnemies = _enemiesInCurrentStage;
            }
            
            StartCoroutine(StageRoutine(def));
        }

        private void WirePlayerAndEnemies()
        {
            var playerH = levelLoader.PlayerInstance.GetComponent<Health>();
            playerH.OnDeath += _ => run.OnPlayerDied();

            _aliveEnemies = 0;
            foreach (var e in levelLoader.EnemyInstances)
            {
                var h = e.GetComponent<Health>();
                if (h != null)
                {
                    _aliveEnemies++;
                    h.OnDeath += OnEnemyDeath;
                }
            }
            _enemiesInCurrentStage = _aliveEnemies;
        }

        private void OnEnemyDeath(Health h)
        {
            _aliveEnemies--;
            run.AddKill();
            if (decals) decals.PlaceX(h.transform.position);
            if (_aliveEnemies <= 0 && !_inPreview)
            {
                run.OnStageCleared();
            }
        }

        private IEnumerator StageRoutine(LevelDefinition def)
        {
            SetGameplayEnabled(false);
            _inPreview = true;
            float t = def.previewSeconds;
            while (t > 0f)
            {
                if (banner) banner.Show(def.stageNumber, t);
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (banner) banner.Hide();
            _inPreview = false;
            SetGameplayEnabled(true);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (levelLoader.PlayerInstance)
            {
                var m = levelLoader.PlayerInstance.GetComponent<PlayerTankController>();
                if (m) m.enabled = enabled;
            }
            foreach (var e in levelLoader.EnemyInstances)
            {
                if (!e) continue;
                var s1 = e.GetComponent<StationaryShooterAI>();
                var s2 = e.GetComponent<MovingShooterAI>();
                var motor = e.GetComponent<TankMotor>();
                if (s1) s1.enabled = enabled;
                if (s2) s2.enabled = enabled;
                if (motor) motor.enabled = enabled;
            }
        }
    }
}
