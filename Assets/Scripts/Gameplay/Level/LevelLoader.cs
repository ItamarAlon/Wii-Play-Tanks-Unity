// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using System.Collections.Generic;
using NavMeshPlus.Components;
using System;
using Game.Gameplay.Tanks.Shared;
using System.Linq;
using UnityEngine.Tilemaps;
using System.Runtime.CompilerServices;

namespace Game.Gameplay.Level
{
    public class LevelLoader : MonoBehaviour
    {
        [HideInInspector] public GameObject StageInstance;
        [HideInInspector] public GameObject PlayerInstance;
        [HideInInspector] public List<GameObject> EnemyInstances;

        private Vector2 initialPlayerPosition;
        private List<(GameObject enemy, Vector2 position)> enemyInstancesAndInitialPos 
            = new List<(GameObject, Vector2)>();

        public void Load(GameObject def)
        {
            Clear();
            StageInstance = def;
            assignAllTanks(StageInstance);
        }

        public GameObject Reload()
        {
            ReleaseAllBullets();
            PlayerInstance.transform.position = initialPlayerPosition;
            PlayerInstance.GetComponent<Health>().Revive();

            foreach (var e in enemyInstancesAndInitialPos)
            {
                e.enemy.transform.position = e.position;
                e.enemy.GetComponent<Health>().Revive();
            }

            return StageInstance;
        }

        public void Clear()
        {
            DestroyAllBullets();
            EnemyInstances.Clear();
            enemyInstancesAndInitialPos.Clear();
        }

        public void DestroyAllBullets()
        {
            invokeOnAllTanks(destroyAllTankBullets);
        }

        public void ReleaseAllBullets()
        {
            invokeOnAllTanks(releaseAllTankBullets);
        }

        private void invokeOnAllTanks(Action<GameObject> action)
        {
            if (PlayerInstance)
                action.Invoke(PlayerInstance);
            EnemyInstances?.ForEach(action.Invoke);
        }

        private void destroyAllTankBullets(GameObject tank)
        {
            Shooter shooter = tank.GetComponent<Shooter>();
            if (shooter)
                shooter.DestroyBullets();
        }

        private void releaseAllTankBullets(GameObject tank)
        {
            Shooter shooter = tank.GetComponent<Shooter>();
            if (shooter)
                shooter.ReleaseBullets();
        }

        private void assignAllTanks(GameObject stageInstance)
        {
            Transform tanksParent = stageInstance.transform.Find("Tanks");

            foreach (Transform tank in tanksParent)
            {
                if (tank.CompareTag("Enemy"))
                    enemyInstancesAndInitialPos.Add((tank.gameObject, tank.position));
                else if (tank.CompareTag("Player"))
                {
                    PlayerInstance = tank.gameObject;
                    initialPlayerPosition = tank.position;
                }
            }

            EnemyInstances = enemyInstancesAndInitialPos.Select(e => e.Item1).ToList();
        }

    }
}
