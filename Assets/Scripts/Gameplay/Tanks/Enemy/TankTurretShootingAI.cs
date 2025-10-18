using Game.Gameplay.Tanks.Shared;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Gameplay.Tanks.Enemy
{
    [RequireComponent(typeof(Shooter))]
    public class TankTurretShootingAI : EnemyAI
    {
        [SerializeField] int word35_RandomTimerA;
        [SerializeField] int word36_RandomTimerB;
        [SerializeField] Shooter shooter;
        [SerializeField] TankLineOfSight lineOfSight;

        private bool CanShoot => lineOfSight.PlayerInSight && !lineOfSight.EnemyInSight;
        public override bool Enable { get; set; }

        void OnValidate()
        {
            if (word35_RandomTimerA < 0) word35_RandomTimerA = 0;
            if (word36_RandomTimerB < 0) word36_RandomTimerB = 0;
            if (word35_RandomTimerA > word36_RandomTimerB)
            {
                int tmp = word35_RandomTimerA;
                word35_RandomTimerA = word36_RandomTimerB;
                word36_RandomTimerB = tmp;
            }               
        }

        void Start()
        {
            if (lineOfSight == null)
                lineOfSight = GetComponentInChildren<TankLineOfSight>();
            if (shooter == null)
                shooter = GetComponent<Shooter>();
        }

        void Update()
        {
            if (CanShoot && Enable)
                waitRandomTimeThenShoot();
        }

        private void waitRandomTimeThenShoot()
        {
            StartCoroutine(waitThenShootRoutine());
        }

        private IEnumerator waitThenShootRoutine()
        {
            int randomTimer = generateRandomTimer();
            for (int i = 0; i < randomTimer; i++)
                yield return null;
            if (Enable)
                shooter.TryFire();
        }

        private int generateRandomTimer()
        {
            return Random.Range(word35_RandomTimerA, word36_RandomTimerB);
        }
    }
}
