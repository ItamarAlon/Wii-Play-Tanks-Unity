using UnityEngine;
using Game.Gameplay.Tanks.Shared; // Shooter

/// <summary>
/// TankTurretShooterAI
/// - Uses shooting "opportunities" (random frame in [shootTimerA, shootTimerB]; A==B => periodic).
/// - Aims by rotating the TurretPivot at a fixed radians/frame until within tolerance, then calls Shooter.TryFire(turret.up).
/// - No muzzle reference required (uses shooter.muzzle internally for LOS/dist checks only).
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
    [SerializeField] float turretTurnSpeedRadPerFrame = 0.08f; // radians per frame (PDF-style)
    [SerializeField] float fireAngleToleranceDeg = 3f;         // must be within this to fire

    [Header("Distance Gates (Unity units)")]
    [SerializeField] float minFireDistance = 0.75f;
    [SerializeField] float maxFireDistance = 30f;

    [Header("Aim Model")]
    [SerializeField, Range(0f, 45f)] float aimRandomMaxDeg = 0f; // add inaccuracy for "dumber" tanks
    [SerializeField] bool enableLeadAim = false;                 // uses Shooter.muzzleSpeed when on
    [SerializeField] float overrideBulletSpeedUPS = 0f;          // leave 0 to use Shooter.muzzleSpeed

    [Header("Cadence & Optional behavior")]
    [SerializeField] bool oneShotPerOpportunity = true; // fire exactly once per opportunity
    [SerializeField] float simulationFps = 60f;         // converts per-frame params to deltaTime

    // --- internal state ---
    int shootTimerFrames;
    int currentShootPick;          // chosen frame in [A,B] for this window
    bool hasPendingShot;           // true after scheduling a shot until fired (or next window)
    float desiredTurretAngleDeg;   // world angle we want the pivot to reach
    Rigidbody2D targetRb;          // optional for leading

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
        // 1) "Shooting opportunity" cadence (ALWAYS enabled)
        if (shootTimerFrames >= currentShootPick)
        {
            pickNewShootWindow();
            shootTimerFrames = 0;
            hasPendingShot = false;

            if (target && (!oneShotPerOpportunity || !hasPendingShot) && shooterConfigured())
            {
                // Distance & LOS gates (origin = shooter.muzzle if present, else turretPivot)
                Vector2 origin = shooter && shooter.muzzle ? (Vector2)shooter.muzzle.position
                                                           : (Vector2)turretPivot.position;
                Vector2 toTarget = (Vector2)target.position - origin;
                float dist = toTarget.magnitude;

                if (dist >= minFireDistance && dist <= maxFireDistance)
                {
                    if (!requireLineOfSight || hasLineOfSight(origin, target.position))
                    {
                        // Choose aim angle (lead or direct) + optional inaccuracy
                        float bulletSpeed = getBulletSpeedUPS();
                        float aimDeg = (enableLeadAim && targetRb && bulletSpeed > 0f)
                            ? computeLeadAngleDeg(origin, target.position, targetRb.linearVelocity, bulletSpeed)
                            : getAngleDeg(toTarget);

                        if (aimRandomMaxDeg > 0f)
                            aimDeg += Random.Range(-aimRandomMaxDeg, aimRandomMaxDeg);

                        desiredTurretAngleDeg = normalizeDeg(aimDeg);
                        hasPendingShot = true; // schedule: fire when aligned
                    }
                }
            }
        }
        shootTimerFrames++;

        // 2) Rotate TurretPivot toward desiredTurretAngleDeg at fixed radians/frame
        float degPerFrame = turretTurnSpeedRadPerFrame * Mathf.Rad2Deg;
        float step = degPerFrame * simulationFps * Time.deltaTime;

        float currentDeg = getAngleDeg(turretPivot.up);
        float delta = Mathf.DeltaAngle(currentDeg, desiredTurretAngleDeg);
        if (Mathf.Abs(delta) <= step) setPivotAngleDeg(desiredTurretAngleDeg);
        else setPivotAngleDeg(currentDeg + Mathf.Sign(delta) * step);

        // 3) Fire once when aligned (Shooter enforces cooldown/maxActive internally)
        if (hasPendingShot && shooterConfigured())
        {
            float err = Mathf.Abs(Mathf.DeltaAngle(getAngleDeg(turret.up), desiredTurretAngleDeg));
            if (err <= fireAngleToleranceDeg)
            {
                // Re-check simple gates at fire moment
                Vector2 origin = shooter && shooter.muzzle ? (Vector2)shooter.muzzle.position
                                                           : (Vector2)turretPivot.position;
                Vector2 toTarget = target ? (Vector2)target.position - origin : Vector2.zero;
                float dist = toTarget.magnitude;
                bool ok = !target || (dist >= minFireDistance && dist <= maxFireDistance);
                if (ok && target && requireLineOfSight) ok = hasLineOfSight(origin, target.position);

                if (ok)
                {
                    // Fire in the turret's forward direction
                    shooter.TryFire(); // Shooter handles spawn at its muzzle, cooldown, maxActive, speed
                    if (oneShotPerOpportunity) hasPendingShot = false;
                }
            }
        }
    }

    // ----------------------------- helpers -----------------------------

    void pickNewShootWindow()
    {
        int a = shootTimerA, b = shootTimerB;
        if (a == b) { currentShootPick = a; return; }
        int lo = Mathf.Min(a, b);
        int hi = Mathf.Max(a, b) + 1;     // int Random.Range is [lo, hi)
        currentShootPick = Random.Range(lo, hi);
    }

    bool shooterConfigured()
    {
        // Shooter needs a muzzle & a bullet prefab to actually spawn; cooldown/maxActive are internal.
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

    // angles/orientation (2D; up = forward)
    static float getAngleDeg(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    static float normalizeDeg(float a) { a %= 360f; if (a < 0) a += 360f; return a; }

    void setPivotAngleDeg(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized - (Vector2)turretPivot.position;
        turretPivot.up = dir;   // rotate pivot (turret & muzzle follow because they’re children)
    }

    // Leading shot: solve constant-velocity intercept in 2D.
    // Returns the absolute aim angle (degrees) from muzzlePos.
    float computeLeadAngleDeg(Vector2 muzzlePos, Vector2 targetPos, Vector2 targetVel, float bulletUPS)
    {
        Vector2 r = targetPos - muzzlePos;                  // relative position
        float a = Vector2.Dot(targetVel, targetVel) - bulletUPS * bulletUPS;
        float b = 2f * Vector2.Dot(r, targetVel);
        float c = Vector2.Dot(r, r);

        // If bullet speed is too low or equation degenerates, fall back to direct aim.
        if (Mathf.Abs(a) < 1e-6f)
            return getAngleDeg(r);

        float disc = b * b - 4f * a * c;
        if (disc < 0f)
            return getAngleDeg(r);

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        // Choose the earliest positive intercept time.
        float t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
        if (t <= 0f)
            return getAngleDeg(r);

        Vector2 aimPoint = targetPos + targetVel * t;
        return getAngleDeg(aimPoint - muzzlePos);
    }

}
