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
using Assets.Scripts.Gameplay.Tanks.Enemy;
using UnityEngine.AI;
using System.Linq;
using System;

namespace Game.GameLoop
{
    public class StageManager : MonoBehaviour
    {
        public LevelLoader levelLoader;
        public RunManager run;
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
            playerH.OnDeath += (s,e) => run.OnPlayerDied();

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

        private void OnEnemyDeath(object sender, EventArgs e)
        {
            Health h = sender as Health;
            aliveEnemiesCount--;
            run.AddKill();
            if (decals) decals.PlaceX(h.transform.position);
            if (aliveEnemiesCount <= 0)
            {
                run.OnStageCleared();
            }
        }

        public void SetGameplayEnabled(bool enabled, bool affectTurretAimer = false)
        {
            enablePlayerGameplay(enabled, affectTurretAimer);
            enableEnemyGameplay(enabled);
        }

        private void enableEnemyGameplay(bool enabled)
        {
            foreach (var enemyTank in levelLoader.EnemyInstances)
            {
                if (!enemyTank)
                    continue;

                EnemyAI[] aiScripts = enemyTank.GetComponents<EnemyAI>();

                foreach (var script in aiScripts)
                    script.Enable = enabled;
            }
        }

        private void enablePlayerGameplay(bool enabled, bool affectTurretAimer)
        {
            GameObject player = levelLoader.PlayerInstance;
            if (!player) return;

            var playerTank = player.GetComponent<PlayerTankController>();
            if (playerTank)
                playerTank.enabled = enabled;

            if (affectTurretAimer)
            {
                var turretAimer = player.GetComponentInChildren<TurretAimer>();
                if (turretAimer)
                    turretAimer.enabled = enabled;
            }
        }
    }
}
