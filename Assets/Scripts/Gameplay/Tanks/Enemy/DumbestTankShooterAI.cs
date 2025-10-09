using UnityEngine;
using Game.Gameplay.Tanks.Shared; // Shooter

/// <summary>
/// TankTurretShooterAI (refactor: same behavior, smaller methods)
/// </summary>
public class TankTurretShooterAI : MonoBehaviour
{
    [Header("Prefab Refs (see hierarchy screenshot)")]
    [SerializeField] Transform turretPivot; // rotates around tank center
    [SerializeField] Transform turret;      // visual barrel (child of pivot); its .up is the fire direction
    [SerializeField] Transform target;      // usually the player
    [SerializeField] Shooter shooter;       // bullet spawner (owns muzzle, cooldown, maxActive, speed)

    [Header("Line of Sight")]
    [SerializeField] LayerMask lineOfSightMask; // walls/obstacles that block shots
    [SerializeField] bool requireLineOfSight = true;

    [Header("Opportunity Window (frames)")]
    [SerializeField] int shootTimerA = 90;
    [SerializeField] int shootTimerB = 150;

    [Header("Turret Rotation & Fire Gate")]
    [SerializeField] float turretTurnSpeedRadPerFrame = 0.08f; // radians per frame
    [SerializeField] float fireAngleToleranceDeg = 3f;

    [Header("Distance Gates (Unity units)")]
    [SerializeField] float minFireDistance = 0.75f;
    [SerializeField] float maxFireDistance = 30f;

    [Header("Aim Model")]
    [SerializeField, Range(0f, 45f)] float aimRandomMaxDeg = 0f;
    [SerializeField] bool enableLeadAim = false;
    [SerializeField] float overrideBulletSpeedUPS = 0f;

    [Header("Cadence & Optional behavior")]
    [SerializeField] bool oneShotPerOpportunity = true;
    [SerializeField] float simulationFps = 60f;

    // --- internal state (unchanged) ---
    int shootTimerFrames;
    int currentShootPick;
    bool hasPendingShot;
    float desiredTurretAngleDeg;
    Rigidbody2D targetRb;

    void Awake()
    {
        if (!turretPivot) turretPivot = transform;
        if (!turret) turret = turretPivot;
        if (!shooter) shooter = GetComponentInChildren<Shooter>();
        if (target) targetRb = target.GetComponent<Rigidbody2D>();

        desiredTurretAngleDeg = getAngleDeg(turret.up);
        pickNewShootWindow();
    }

    void Update()
    {
        HandleShootingOpportunityCadenceAndScheduling();
        RotateTurretTowardDesired();
        FireIfAligned();
    }

    // ───────────────────────── helpers / steps ─────────────────────────

    void HandleShootingOpportunityCadenceAndScheduling()
    {
        if (shootTimerFrames >= currentShootPick)
        {
            pickNewShootWindow();
            shootTimerFrames = 0;
            hasPendingShot = false;

            if (target && (!oneShotPerOpportunity || !hasPendingShot) && shooterConfigured())
            {
                Vector2 origin = shooter && shooter.muzzle ? (Vector2)shooter.muzzle.position
                                                           : (Vector2)turretPivot.position;
                Vector2 toTarget = (Vector2)target.position - origin;
                float dist = toTarget.magnitude;

                if (dist >= minFireDistance && dist <= maxFireDistance)
                {
                    if (!requireLineOfSight || hasLineOfSight(origin, target.position))
                    {
                        float bulletSpeed = getBulletSpeedUPS();
                        float aimDeg = (enableLeadAim && targetRb && bulletSpeed > 0f)
                            ? computeLeadAngleDeg(origin, target.position, targetRb.linearVelocity, bulletSpeed)
                            : getAngleDeg(toTarget);

                        if (aimRandomMaxDeg > 0f)
                            aimDeg += Random.Range(-aimRandomMaxDeg, aimRandomMaxDeg);

                        desiredTurretAngleDeg = normalizeDeg(aimDeg);
                        hasPendingShot = true;
                    }
                }
            }
        }
        shootTimerFrames++;
    }

    void RotateTurretTowardDesired()
    {
        float degPerFrame = turretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
        float step = degPerFrame * simulationFps * Time.deltaTime;

        float currentDeg = getAngleDeg(turretPivot.up);
        float delta = Mathf.DeltaAngle(currentDeg, desiredTurretAngleDeg);

        if (Mathf.Abs(delta) <= step) setPivotAngleDeg(desiredTurretAngleDeg);
        else setPivotAngleDeg(currentDeg + Mathf.Sign(delta) * step);
    }

    void FireIfAligned()
    {
        if (!hasPendingShot || !shooterConfigured()) return;

        float err = Mathf.Abs(Mathf.DeltaAngle(getAngleDeg(turret.up), desiredTurretAngleDeg));
        if (err > fireAngleToleranceDeg) return;

        Vector2 origin = shooter && shooter.muzzle ? (Vector2)shooter.muzzle.position
                                                   : (Vector2)turretPivot.position;
        Vector2 toTarget = target ? (Vector2)target.position - origin : Vector2.zero;
        float dist = toTarget.magnitude;

        bool ok = !target || (dist >= minFireDistance && dist <= maxFireDistance);
        if (ok && target && requireLineOfSight) ok = hasLineOfSight(origin, target.position);

        if (ok)
        {
            shooter.TryFire(); // unchanged
            if (oneShotPerOpportunity) hasPendingShot = false;
        }
    }

    // ───────────────────────── unchanged utility methods ─────────────────────────

    void pickNewShootWindow()
    {
        int a = shootTimerA, b = shootTimerB;
        if (a == b) { currentShootPick = a; return; }
        int lo = Mathf.Min(a, b);
        int hi = Mathf.Max(a, b) + 1; // int Random.Range is [lo, hi)
        currentShootPick = Random.Range(lo, hi);
    }

    bool shooterConfigured()
    {
        if (!shooter) return false;
        if (!shooter.muzzle) return false;
        if (!shooter.bulletPrefab) return false;
        return true;
    }

    bool hasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        var hit = Physics2D.Raycast(from, d.normalized, d.magnitude, lineOfSightMask);
        return hit.collider == null;
    }

    float getBulletSpeedUPS()
    {
        if (overrideBulletSpeedUPS > 0f) return overrideBulletSpeedUPS;
        return shooter ? shooter.muzzleSpeed : 0f;
    }

    static float getAngleDeg(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    static float normalizeDeg(float a) { a %= 360f; if (a < 0) a += 360f; return a; }

    void setPivotAngleDeg(float angleDeg)
    {
        // NOTE: left intact to preserve exact behavior
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized - (Vector2)turretPivot.position;
        turretPivot.up = dir;
    }

    float computeLeadAngleDeg(Vector2 muzzlePos, Vector2 targetPos, Vector2 targetVel, float bulletUPS)
    {
        Vector2 r = targetPos - muzzlePos;
        float a = Vector2.Dot(targetVel, targetVel) - bulletUPS * bulletUPS;
        float b = 2f * Vector2.Dot(r, targetVel);
        float c = Vector2.Dot(r, r);

        if (Mathf.Abs(a) < 1e-6f) return getAngleDeg(r);

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return getAngleDeg(r);

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        float t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
        if (t <= 0f) return getAngleDeg(r);

        Vector2 aimPoint = targetPos + targetVel * t;
        return getAngleDeg(aimPoint - muzzlePos);
    }
}
