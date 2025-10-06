// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using System.Collections.Generic;
using NavMeshPlus.Components;

namespace Game.Gameplay.Level
{
    public class LevelLoader : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject playerTankPrefab;
        public GameObject enemyStationaryPrefab;
        public GameObject enemyMovingPrefab;

        [HideInInspector] public GameObject StageInstance;
        [HideInInspector] public GameObject PlayerInstance;
        [HideInInspector] public List<GameObject> EnemyInstances = new List<GameObject>();

        public void Load(LevelDefinition def, Transform parent)
        {
            Clear();
            if (!def || !def.stagePrefab) { Debug.LogWarning("LevelDefinition missing"); return; }
            StageInstance = Instantiate(def.stagePrefab, parent);
            StageInstance.GetComponentInChildren<NavMeshSurface>().BuildNavMesh();
            assignAllTanks(StageInstance);
        }

        public void Clear()
        {
            if (StageInstance) Destroy(StageInstance);
            if (PlayerInstance) Destroy(PlayerInstance);
            foreach (var e in EnemyInstances) 
                if (e) 
                    Destroy(e);
            EnemyInstances.Clear();
        }

        private void assignAllTanks(GameObject stageInstance)
        {
            Transform tanksParent = stageInstance.transform.Find("Tanks");

            foreach (Transform tank in tanksParent)
            {
                if (tank.CompareTag("Enemy"))
                    EnemyInstances.Add(tank.gameObject);
                else if (tank.CompareTag("Player"))
                    PlayerInstance = tank.gameObject;
            }
        }
    }
}
