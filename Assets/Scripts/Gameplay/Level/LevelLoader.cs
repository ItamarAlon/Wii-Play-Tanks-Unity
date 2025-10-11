// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using System.Collections.Generic;
using NavMeshPlus.Components;
using System;
using Game.Gameplay.Tanks.Shared;
using System.Linq;

namespace Game.Gameplay.Level
{
    public class LevelLoader : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject playerTankPrefab;
        public GameObject enemyStationaryPrefab;
        public GameObject enemyMovingPrefab;
        public Transform stageRoot;

        [HideInInspector] public GameObject StageInstance;
        [HideInInspector] public GameObject PlayerInstance;
        [HideInInspector] public List<GameObject> EnemyInstances;

        private Vector2 initialPlayerPosition;
        private List<(GameObject, Vector2)> EnemyInstancesAndInitialPos = new List<(GameObject, Vector2)>();

        public GameObject Load(LevelDefinition def)
        {
            Clear();
            if (!def || !def.stagePrefab) { Debug.LogWarning("LevelDefinition missing"); return null; }
            StageInstance = Instantiate(def.stagePrefab, stageRoot);
            StageInstance.GetComponentInChildren<NavMeshSurface>().BuildNavMesh();
            assignAllTanks(StageInstance);

            return StageInstance;
        }

        public GameObject Reload()
        {
            PlayerInstance.transform.position = initialPlayerPosition;
            PlayerInstance.GetComponent<Health>().Revive();
            PlayerInstance.GetComponent<Shooter>().ClearBullets();

            foreach (var e in EnemyInstancesAndInitialPos)
            {
                e.Item1.transform.position = e.Item2;
                e.Item1.GetComponent<Health>().Revive();
                e.Item1.GetComponent<Shooter>().ClearBullets();
            }

            return StageInstance;
        }

        public void Clear()
        {
            if (StageInstance)
                Destroy(StageInstance);
            if (PlayerInstance)
                Destroy(PlayerInstance);
            foreach (var e in EnemyInstances) 
                if (e) 
                    Destroy(e);
            EnemyInstances.Clear();
            EnemyInstancesAndInitialPos.Clear();
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
