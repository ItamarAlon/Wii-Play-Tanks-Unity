using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wii Play Tanks - Dumbest Moving Tank (Movement-Only AI)
/// Implements the PDF's Tank Movement Procedure (pages 39–42) and General Game Info (pages 4–6):
/// - Random timers -> "movement opportunities" gate (Words 14 & 15)
/// - Turn-type hierarchy: Survival → Large → Sequence → Random (pages 4–6)
/// - Queueing movements (Word 22), mini-turn splitting
/// - 90°/reverse logic; rotation at Word 26 radians per frame; pivot + accel/decel (Words 27, 11–12, 23)
/// - Obstacle awareness (Word 28), look-ahead based on N = Word28 / 2
/// Movement uses NavMeshAgent.Move along transform.up (2D with NavMeshPlus).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DumbestMovingTankAI : MonoBehaviour
{
    [Header("NavMesh / 2D Setup")]
    [Tooltip("Assign your NavMeshAgent (NavMeshPlus 2D: set updateRotation=false, updateUpAxis=false).")]
    public NavMeshAgent agent;

    [Tooltip("Units: internal-units → Unity units scale. (PDF uses internal units; a block = 35.0 units).")]
    public float internalUnitsToUnity = 1f; // You can set 1f if your map uses same units, or 1f/35f if tiles are 1 unit.

    [Tooltip("Simulation FPS used to convert per-frame PDF values to Unity time.")]
    public float simulationFps = 60f;

    [Header("Words 11–15 (Movement Timers & Turn Randomness)")]
    [Tooltip("Word 11 – Tank Acceleration Value (internal units per frame). Brown nominal 0, others ~0.3")]
    public float word11_Accel = 0.3f;

    [Tooltip("Word 12 – Tank Deceleration Multiplier (0..1; 0 = instant stop). Brown nominal 0.")]
    public float word12_DecelMul = 0.6f;

    [Tooltip("Word 13 – Random Turn Max Angle (degrees).")]
    public float word13_RandomTurnMaxAngle = 30f;

    [Tooltip("Word 14 – Random Timer Boundary A (Movement) (frames).")]
    public int word14_TimerA = 15;

    [Tooltip("Word 15 – Random Timer Boundary B (Movement) (frames).")]
    public int word15_TimerB = 10;

    [Header("Words 16–21 (Survival & Aggressiveness)")]
    [Tooltip("Word 16 – AI Mine Awareness (radius, internal units). Brown nominal 0.")]
    public float word16_AIMineAwareness = 120f;

    [Tooltip("Word 17 – AI Bullet Awareness (radius, internal units).")]
    public float word17_AIBulletAwareness = 120f;

    [Tooltip("Word 18 – Player Mine Awareness (radius, internal units).")]
    public float word18_PlayerMineAwareness = 0f;

    [Tooltip("Word 19 – Player Bullet Awareness (radius, internal units).")]
    public float word19_PlayerBulletAwareness = 40f;

    [Tooltip("Word 20 – Survival Mode Activity Flag (nonzero enables).")]
    public int word20_SurvivalModeFlag = 1;

    [Tooltip("Word 21 – Tank Aggressiveness Bias (0..1; 0=no bias, 1=always bias towards player). Brown often 0.")]
    [Range(0f, 1f)] public float word21_Aggressiveness = 0.03f;

    [Header("Words 22–28 (Queueing, Speed, Turn, Obstacle Awareness)")]
    [Tooltip("Word 22 – Total Queueing Movements Value (1..10 typical).")]
    [Range(1, 10)] public int word22_QueueCount = 4;

    [Tooltip("Word 23 – Tank Max Speed (internal units per frame).")]
    public float word23_MaxSpeed = 1.2f;

    // (Words 24–25 are for player stick input; N/A here)

    [Tooltip("Word 26 – Tank Turn Speed (radians per frame). Brown nominal 0.08 rad/frame.")]
    public float word26_TurnSpeedRadPerFrame = 0.08f;

    [Tooltip("Word 27 – Tank Max Turn Pivot Angle (degrees) above which we decelerate.")]
    public float word27_MaxTurnPivotDeg = 10f;

    [Tooltip("Word 28 – Obstacle Awareness (Movement) look-ahead factor (frames). N = Word28 / 2.")]
    public int word28_ObstacleAwareness = 30;

    [Header("Layers / Detection (2D)")]
    public LayerMask aiBulletMask;
    public LayerMask playerBulletMask;
    public LayerMask aiMineMask;
    public LayerMask playerMineMask;

    [Header("Optional: Player Target (for aggressiveness bias)")]
    public Transform playerTarget;

    // === Internal state (mirrors the PDF’s step flow) ===
    // Timers & random windows
    private int turningTimerFrames = 0;
    private int randomTurningValue;    // chosen inside [TimerA, TimerB] per opportunity

    // Flags controlling step 3 (IF/HOW to update the turn target)
    private bool survivalModeFlag;
    private bool largeTurnFlag;
    private bool sequenceTurnFlag;
    private bool randomTurnFlag;
    private bool mineMovementOverrideFlag;

    // Queue entries track source so Large Turn can overwrite only Random entries
    private enum QueueSource { None, RandomTurn, LargeTurn }
    private readonly Queue<(float angleDeg, QueueSource src)> movementQueue = new();
    private QueueSource lastQueueSource = QueueSource.None;

    // Movement/turn state
    private float currentVelocity_InternalPerFrame = 0f; // internal units per frame
    private float requestedSpeed_InternalPerFrame = 0f;  // Word 23 each frame for AI
    private float turnTargetAngleDeg;                    // absolute heading we aim tank body at (world-space)
    private bool moveForwardThisFrame = true;            // reverse when target is > 90° away

    // Optional external stuns (Words 10 & 42), let other systems set these
    [Header("Stun Timers (optional, set by shooter/mine systems)")]
    public int bulletStunTimerFrames = 0; // Word 42
    public int mineStunTimerFrames = 0;   // Word 10

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // NavMeshPlus/2D friendly settings
        agent.updateRotation = false;   // we'll rotate transform manually (2D)
        agent.updateUpAxis = false;   // keep Z-up disabled for 2D

        // Start facing = current transform.up
        turnTargetAngleDeg = GetFacingAngleDeg();

        // Seed the first random window
        PickNewRandomTurningValue();
    }

    void Update()
    {
        // === Movement opportunity cadence (ALWAYS enabled) ===
        if (turningTimerFrames >= randomTurningValue)
        {
            PickNewRandomTurningValue(); // if A==B, deterministic every A frames
            turningTimerFrames = 0;

            // Decide which kind of turn to schedule (Survival -> Large -> Sequence -> Random)
            if (word20_SurvivalModeFlag != 0 && DetectThreatsForSurvival())
            {
                survivalModeFlag = true;
            }
            else
            {
                // Compute look-ahead distance once (NavMesh-based checks use it)
                float N = Mathf.Max(0f, word28_ObstacleAwareness / 2f);
                float lookDistInternal = Mathf.Max(1f, currentVelocity_InternalPerFrame * N + 2f);
                float lookDistUnity = lookDistInternal * internalUnitsToUnity;

                if (ObstacleAhead_NavMesh(lookDistUnity))
                {
                    largeTurnFlag = true;
                }
                else if (movementQueue.Count > 0)
                {
                    sequenceTurnFlag = true;
                }
                else
                {
                    randomTurnFlag = true;
                }
            }
        }
        turningTimerFrames++;

        // 3) IF/HOW to update the turn target (strict order)
        if (mineMovementOverrideFlag)
        {
            mineMovementOverrideFlag = false;
        }
        else if (survivalModeFlag)
        {
            Vector2 away = ComputeSurvivalEscapeVector();
            if (away.sqrMagnitude > 0.0001f)
                turnTargetAngleDeg = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
            survivalModeFlag = false;
        }
        else if (largeTurnFlag)
        {
            bool overwriteAllowed = (lastQueueSource != QueueSource.LargeTurn);
            if (overwriteAllowed)
            {
                float N = Mathf.Max(0f, word28_ObstacleAwareness / 2f);
                float lookDistInternal = Mathf.Max(1f, currentVelocity_InternalPerFrame * N + 2f);
                float lookDistUnity = lookDistInternal * internalUnitsToUnity;

                var (leftOpen, rightOpen) = ProbeLeftRightForLargeTurn_NavMesh(lookDistUnity);

                if (leftOpen || rightOpen)
                {
                    float chosenDelta = (leftOpen && rightOpen)
                        ? (Random.value < 0.5f ? +90f : -90f)
                        : (leftOpen ? +90f : -90f);
                    EnqueueMiniTurnsRelative(chosenDelta, QueueSource.LargeTurn);
                }
                else
                {
                    turnTargetAngleDeg = Mathf.Repeat(GetFacingAngleDeg() + 180f, 360f); // reverse
                    movementQueue.Clear();
                    lastQueueSource = QueueSource.None;
                }
            }
            largeTurnFlag = false;
            sequenceTurnFlag = true; // consume next queued mini-turn
        }
        else if (randomTurnFlag)
        {
            float delta = Random.Range(-word13_RandomTurnMaxAngle, word13_RandomTurnMaxAngle);

            // Apply aggressiveness bias ONLY if > 0
            if (playerTarget != null && word21_Aggressiveness > 0f)
            {
                Vector2 initialDir = DirFromAngle(GetFacingAngleDeg() + delta).normalized;
                Vector2 toPlayer = ((Vector2)(playerTarget.position - transform.position)).normalized;
                Vector2 mixed = Vector2.Lerp(initialDir, toPlayer, Mathf.Clamp01(word21_Aggressiveness)).normalized;
                float mixedDelta = Mathf.DeltaAngle(AngleFromDir(initialDir), AngleFromDir(mixed));
                delta += mixedDelta;
            }

            EnqueueMiniTurnsRelative(delta, QueueSource.RandomTurn);
            randomTurnFlag = false;
            sequenceTurnFlag = true;
        }

        if (sequenceTurnFlag)
        {
            if (movementQueue.Count > 0)
            {
                var entry = movementQueue.Dequeue();
                lastQueueSource = entry.src;
                turnTargetAngleDeg = Mathf.Repeat(entry.angleDeg, 360f);
            }
            sequenceTurnFlag = false;
        }

        // 4) Rotate body by exactly Word26 (rad/frame) → deg/sec
        float facing = GetFacingAngleDeg();
        float deltaToTarget = Mathf.DeltaAngle(facing, turnTargetAngleDeg);

        // If target is > 90° away, we'll move backwards this frame
        moveForwardThisFrame = Mathf.Abs(deltaToTarget) <= 90f;

        float degPerFrame = word26_TurnSpeedRadPerFrame * Mathf.Rad2Deg;
        float degPerSecond = degPerFrame * simulationFps;
        float step = degPerSecond * Time.deltaTime;

        if (Mathf.Abs(deltaToTarget) <= step)
            SetFacingAngleDeg(turnTargetAngleDeg);
        else
            SetFacingAngleDeg(facing + Mathf.Sign(deltaToTarget) * step);

        // 5) Requested speed = Word 23 (internal units / frame)
        requestedSpeed_InternalPerFrame = Mathf.Max(0f, word23_MaxSpeed);

        // 6) Accel / Decel (Word 11 / Word 12) with pivot rule (Word 27)
        float requiredTurnNow = Mathf.Abs(Mathf.DeltaAngle(GetFacingAngleDeg(), turnTargetAngleDeg));
        if (requiredTurnNow > word27_MaxTurnPivotDeg || requestedSpeed_InternalPerFrame < currentVelocity_InternalPerFrame)
        {
            // decelerate: v = v * word12 (0 → instant stop)
            currentVelocity_InternalPerFrame *= Mathf.Clamp01(word12_DecelMul);
        }
        else if (requestedSpeed_InternalPerFrame > currentVelocity_InternalPerFrame)
        {
            currentVelocity_InternalPerFrame += word11_Accel;
        }

        // 7) Stuns + clamp to max
        if (bulletStunTimerFrames > 0 || mineStunTimerFrames > 0)
            currentVelocity_InternalPerFrame = 0f;

        currentVelocity_InternalPerFrame = Mathf.Min(currentVelocity_InternalPerFrame, word23_MaxSpeed);

        if (bulletStunTimerFrames > 0) bulletStunTimerFrames--;
        if (mineStunTimerFrames > 0) mineStunTimerFrames--;

        // 8) MOVE using NavMeshAgent SPEED + DESTINATION along forward (or reverse)
        Vector2 dir2D = transform.up * (moveForwardThisFrame ? 1f : -1f);

        // Convert internal-units/frame -> units/second
        float unitsPerSecond = currentVelocity_InternalPerFrame * simulationFps * internalUnitsToUnity;

        // Make Max Speed immediately reflect in motion:
        agent.speed = unitsPerSecond;

        // Give the agent a short ahead point to walk toward (on-mesh) each frame
        // The distance here is arbitrary; ~2–3 Unity units works well in 2D top-down arenas.
        Vector3 shortAhead = transform.position + (Vector3)(dir2D * 2f);
        agent.SetDestination(shortAhead);
    }

    // === Helpers ===

    private void PickNewRandomTurningValue()
    {
        int a = word14_TimerA;
        int b = word15_TimerB;
        if (a == b)
        {
            randomTurningValue = a;
            return;
        }
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b) + 1; // max is exclusive in Random.Range for ints
        randomTurningValue = Random.Range(min, max);
    }

    // Survival detection gate (Words 16–19). Returns true if we should enter survival mode this frame.
    private bool DetectThreatsForSurvival()
    {
        bool aiMine = word16_AIMineAwareness > 0f && Physics2D.OverlapCircle(transform.position, word16_AIMineAwareness * internalUnitsToUnity, aiMineMask);
        bool aiBullet = word17_AIBulletAwareness > 0f && AnyBulletHeadingTowardsMe(aiBulletMask, word17_AIBulletAwareness);
        bool playerMine = word18_PlayerMineAwareness > 0f && Physics2D.OverlapCircle(transform.position, word18_PlayerMineAwareness * internalUnitsToUnity, playerMineMask);
        bool playerBullet = word19_PlayerBulletAwareness > 0f && AnyBulletHeadingTowardsMe(playerBulletMask, word19_PlayerBulletAwareness);
        return aiMine || aiBullet || playerMine || playerBullet;
    }

    private Vector2 ComputeSurvivalEscapeVector()
    {
        Vector2 sum = Vector2.zero;
        AccumulateInverseVectors(aiMineMask, word16_AIMineAwareness, ref sum);
        AccumulateInverseVectors(playerMineMask, word18_PlayerMineAwareness, ref sum);
        AccumulateInverseVectors(aiBulletMask, word17_AIBulletAwareness, ref sum, onlyTowardMe: true);
        AccumulateInverseVectors(playerBulletMask, word19_PlayerBulletAwareness, ref sum, onlyTowardMe: true);
        return sum.normalized;
    }

    private void AccumulateInverseVectors(LayerMask mask, float radiusInternal, ref Vector2 sum, bool onlyTowardMe = false)
    {
        if (radiusInternal <= 0f) return;
        float r = radiusInternal * internalUnitsToUnity;
        var hits = Physics2D.OverlapCircleAll(transform.position, r, mask);
        foreach (var h in hits)
        {
            Vector2 to = (Vector2)(h.transform.position - transform.position);
            if (onlyTowardMe)
            {
                // If bullet has a rigidbody/velocity, only count if heading roughly towards me
                var rb = h.attachedRigidbody;
                if (rb != null)
                {
                    float approaching = Vector2.Dot(rb.linearVelocity.normalized, to.normalized);
                    if (approaching <= 0f) continue; // heading away -> ignore
                }
            }
            if (to.sqrMagnitude > 0.0001f) sum += (-to).normalized;
        }
    }

    private bool AnyBulletHeadingTowardsMe(LayerMask mask, float radiusInternal)
    {
        float r = radiusInternal * internalUnitsToUnity;
        var hits = Physics2D.OverlapCircleAll(transform.position, r, mask);
        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb == null) return true; // count as threat if no data
            Vector2 to = (Vector2)(transform.position - h.transform.position);
            if (to.sqrMagnitude < 0.0001f) return true;
            float dot = Vector2.Dot(rb.linearVelocity.normalized, to.normalized);
            if (dot > 0f) return true; // bullet velocity points toward me
        }
        return false;
    }

    bool ObstacleAhead_NavMesh(float distanceUnity)
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * distanceUnity;

        // Raycasts along the navmesh; returns true if a wall/edge blocks the straight line
        return agent.Raycast(end, out _);
    }

    (bool leftOpen, bool rightOpen) ProbeLeftRightForLargeTurn_NavMesh(float distanceUnity)
    {
        Vector3 start = transform.position;
        Vector3 leftEnd = start + (Vector3)Rotate90(transform.up, +1) * distanceUnity;
        Vector3 rightEnd = start + (Vector3)Rotate90(transform.up, -1) * distanceUnity;

        NavMeshHit hit;
        bool leftBlocked = agent.Raycast(leftEnd, out hit);
        bool rightBlocked = agent.Raycast(rightEnd, out hit);

        return (!leftBlocked, !rightBlocked);
    }

    static Vector2 Rotate90(Vector2 v, int leftPlusOneRightMinusOne) =>
        leftPlusOneRightMinusOne >= 0 ? new Vector2(-v.y, v.x) : new Vector2(v.y, -v.x);


    private void EnqueueMiniTurnsRelative(float deltaAngleDeg, QueueSource src)
    {
        float start = GetFacingAngleDeg();
        float target = Mathf.Repeat(start + deltaAngleDeg, 360f);

        movementQueue.Clear();
        lastQueueSource = src;

        int count = Mathf.Max(1, word22_QueueCount);
        float step = Mathf.DeltaAngle(start, target) / count;

        float accum = start;
        for (int i = 1; i <= count; i++)
        {
            accum = Mathf.Repeat(start + step * i, 360f);
            movementQueue.Enqueue((accum, src));
        }
    }

    // === Angle utilities (2D, up = forward) ===
    private float GetFacingAngleDeg() => Mathf.Atan2(transform.up.y, transform.up.x) * Mathf.Rad2Deg;

    private void SetFacingAngleDeg(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        transform.up = dir.normalized;
    }

    private static Vector2 DirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private static float AngleFromDir(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    private static float SignedAngleDeg(float fromDeg, float toDeg) => Mathf.DeltaAngle(fromDeg, toDeg);

    // === Public API hooks (if other systems need to interact) ===

    /// <summary>Call this the frame a mine is laid so the mover will honor the "mine movement override" gate on step 3.</summary>
    public void NotifyMineMovementOverride() => mineMovementOverrideFlag = true;

    /// <summary>Force an immediate "movement opportunity" next frame (e.g., for debugging).</summary>
    public void ForceMovementOpportunityNextFrame() => turningTimerFrames = Mathf.Max(turningTimerFrames, randomTurningValue);

    /// <summary>Reset movement queue (e.g., when respawning).</summary>
    public void ClearQueue()
    {
        movementQueue.Clear();
        lastQueueSource = QueueSource.None;
    }
}
