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
        private List<(GameObject, Vector2)> EnemyInstancesAndInitialPos = new List<(GameObject, Vector2)>();

        public void Load(GameObject def)
        {
            Clear();
            StageInstance = def;
            assignAllTanks(StageInstance);
        }

        public GameObject Reload()
        {
            RemoveAllBullets();
            PlayerInstance.transform.position = initialPlayerPosition;
            PlayerInstance.GetComponent<Health>().Revive();

            foreach (var e in EnemyInstancesAndInitialPos)
            {
                e.Item1.transform.position = e.Item2;
                e.Item1.GetComponent<Health>().Revive();
            }

            return StageInstance;
        }

        public void Clear()
        {
            EnemyInstances.Clear();
            EnemyInstancesAndInitialPos.Clear();
            RemoveAllBullets();
        }

        public void RemoveAllBullets()
        {
            if (PlayerInstance)
                removeAllTankBullets(PlayerInstance);
            EnemyInstances?.ForEach(removeAllTankBullets);
        }

        private void removeAllTankBullets(GameObject tank)
        {
            Shooter shooter = tank.GetComponent<Shooter>();
            if (shooter)
                shooter.ClearBullets();
        }

        private void assignAllTanks(GameObject stageInstance)
        {
            Transform tanksParent = stageInstance.transform.Find("Tanks");

            foreach (Transform tank in tanksParent)
            {
                if (tank.CompareTag("Enemy"))
                    EnemyInstancesAndInitialPos.Add((tank.gameObject, tank.position));
                else if (tank.CompareTag("Player"))
                {
                    PlayerInstance = tank.gameObject;
                    initialPlayerPosition = tank.position;
                }
            }

            EnemyInstances = EnemyInstancesAndInitialPos.Select(e => e.Item1).ToList();
        }

    }
}
