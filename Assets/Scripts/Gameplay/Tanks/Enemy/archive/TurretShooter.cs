using UnityEngine;
using System.Collections.Generic;

/// 2D turret/shot logic. Turret forward = turret.up. Uses Rigidbody2D for bullets.
public class TurretShooter : MonoBehaviour
{
    [Header("Scene References")]
    public Transform turret;                 // rotates around Z
    public Transform barrelMuzzle;           // bullet spawn point
    public Transform[] playerTargets;
    public LayerMask rayMask = ~0;           // for “would hit” checks (optional)
    public GameObject bulletPrefab;          // must have Rigidbody2D

    [Header("Per-frame params; scaled by assumedFPS")]
    public float turretAngleOffsetDeg = 40f;     // random offset range (±)
    public float turretTurnRadPerFrame = 0.01f;
    public int targetRefreshFrames = 45;

    public int bulletLimit = 1;
    public int fireTimerA = 30, fireTimerB = 30;  // equal → eligible every frame
    public int bulletCooldownFrames = 180;
    public float bulletSpeedPerFrame = 2.5f;
    public float aiBarrelRadius = 70f;

    public float selfDetectDeg = 5f;
    public float aiDetectDeg = 20f;
    public float playerDetectDeg = 20f;

    [Header("Runtime/Tuning")]
    public float assumedFPS = 60f;

    int m_turretTargetTimer;
    int m_fireTimer, m_fireTimerTarget;
    int m_cooldown;
    int m_bulletsAlive;
    bool m_inSurvival, m_survivalBlocks;

    Vector2 m_targetPoint;
    HashSet<GameObject> m_mine = new();

    void OnEnable()
    {
        ResetTurretTimer();
        ResetFireTimerWindow();
    }

    public void SetSurvivalState(bool inSurvival, bool blockActions)
    {
        m_inSurvival = inSurvival;
        m_survivalBlocks = blockActions;
    }

    void Update()
    {
        // (1) Targeting
        if (m_turretTargetTimer <= 0)
        {
            Vector2 p = ClosestPlayer();
            m_targetPoint = p;

            // random offset (±turretAngleOffsetDeg)
            Vector2 dir = p - (Vector2)turret.position;
            if (dir.sqrMagnitude > 1e-6f)
            {
                float off = Random.Range(-turretAngleOffsetDeg, turretAngleOffsetDeg);
                dir = Rotate(dir.normalized, off);
                m_targetPoint = (Vector2)turret.position + dir * 1000f;
            }
            m_turretTargetTimer = targetRefreshFrames;
        }
        RotateTurretToward(m_targetPoint);
        m_turretTargetTimer--;

        // (2) Firing cadence & gates
        if (m_cooldown > 0) m_cooldown--;

        bool firingOpportunity = (fireTimerA == fireTimerB) ? true : (m_fireTimer >= m_fireTimerTarget);
        if (fireTimerA != fireTimerB && m_fireTimer >= m_fireTimerTarget) ResetFireTimerWindow(); else m_fireTimer++;

        if (firingOpportunity)
        {
            if ((m_inSurvival && m_survivalBlocks) ||
                m_bulletsAlive >= bulletLimit ||
                m_cooldown > 0 ||
                AIInRadius(aiBarrelRadius) ||
                WouldHitSelf(selfDetectDeg) ||
                WouldHitAI(aiDetectDeg) ||
                !PlayerInAngle(playerDetectDeg))
            {
                // fail a gate → no shot
            }
            else
            {
                FireOne();
            }
        }

        PruneBullets();
    }

    void ResetTurretTimer() => m_turretTargetTimer = targetRefreshFrames;

    void ResetFireTimerWindow()
    {
        m_fireTimer = 0;
        int a = Mathf.Min(fireTimerA, fireTimerB);
        int b = Mathf.Max(fireTimerA, fireTimerB);
        m_fireTimerTarget = (a == b) ? a : Random.Range(a, b + 1);
    }

    void RotateTurretToward(Vector2 worldPoint)
    {
        Vector2 dir = worldPoint - (Vector2)turret.position;
        if (dir.sqrMagnitude < 1e-6f) return;

        float desired = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // up = forward
        float current = turret.eulerAngles.z;
        float maxStepDeg = turretTurnRadPerFrame * Mathf.Rad2Deg * (Time.deltaTime * assumedFPS);
        float next = Mathf.MoveTowardsAngle(current, desired, maxStepDeg);
        turret.rotation = Quaternion.Euler(0f, 0f, next);
    }

    Vector2 ClosestPlayer()
    {
        float best = float.PositiveInfinity;
        Vector2 pos = turret.position;
        Vector2 result = pos + (Vector2)turret.up;
        foreach (var t in playerTargets)
        {
            if (!t) continue;
            float d = ((Vector2)t.position - pos).sqrMagnitude;
            if (d < best) { best = d; result = t.position; }
        }
        return result;
    }

    bool PlayerInAngle(float deg)
    {
        foreach (var t in playerTargets)
        {
            if (!t) continue;
            Vector2 v = (Vector2)t.position - (Vector2)barrelMuzzle.position;
            if (v.sqrMagnitude < 1e-6f) continue;
            float ang = Vector2.Angle(turret.up, v);
            if (ang <= deg) return true;
        }
        return false;
    }

    bool WouldHitSelf(float deg)
    {
        // Simple placeholder cone test; your bullet collision should still protect self-hits.
        float ang = Vector2.Angle(turret.up, -turret.up); // 180
        return ang <= deg; // almost always false
    }

    bool WouldHitAI(float deg)
    {
        // Wire in your AI registry here if friendly-fire should gate shots.
        return false;
    }

    bool AIInRadius(float radius)
    {
        // Wire in your AI registry here if muzzle-crowding should block shots.
        return false;
    }

    void FireOne()
    {
        if (!bulletPrefab || !barrelMuzzle) return;

        var go = Instantiate(bulletPrefab, barrelMuzzle.position, barrelMuzzle.rotation);
        m_mine.Add(go);
        m_bulletsAlive++;

        float vPerSecond = bulletSpeedPerFrame * assumedFPS;
        var rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d) rb2d.linearVelocity = (Vector2)turret.up * vPerSecond;

        m_cooldown = bulletCooldownFrames;
    }

    void PruneBullets()
    {
        m_mine.RemoveWhere(go => go == null);
        m_bulletsAlive = m_mine.Count;
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
