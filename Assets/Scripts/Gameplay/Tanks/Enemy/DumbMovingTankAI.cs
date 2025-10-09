using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wii Play Tanks - Dumbest Moving Tank (Movement-Only AI) – refactor only (behavior unchanged)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DumbestMovingTankAI : MonoBehaviour
{
    [Header("NavMesh / 2D Setup")]
    [Tooltip("Assign your NavMeshAgent (NavMeshPlus 2D: set updateRotation=false, updateUpAxis=false).")]
    public NavMeshAgent agent;

    [Tooltip("Units: internal-units → Unity units scale. (PDF uses internal units; a block = 35.0 units).")]
    public float internalUnitsToUnity = 1f;

    [Tooltip("Simulation FPS used to convert per-frame PDF values to Unity time.")]
    public float simulationFps = 60f;

    [Header("Words 11–15 (Movement Timers & Turn Randomness)")]
    [Tooltip("Word 11 – Tank Acceleration Value (internal units per frame).")]
    public float word11_Accel = 0.3f;

    [Tooltip("Word 12 – Tank Deceleration Multiplier (0..1; 0 = instant stop).")]
    public float word12_DecelMul = 0.6f;

    [Tooltip("Word 13 – Random Turn Max Angle (degrees).")]
    public float word13_RandomTurnMaxAngle = 30f;

    [Tooltip("Word 14 – Random Timer Boundary A (Movement) (frames).")]
    public int word14_TimerA = 15;

    [Tooltip("Word 15 – Random Timer Boundary B (Movement) (frames).")]
    public int word15_TimerB = 10;

    [Header("Words 16–21 (Survival & Aggressiveness)")]
    [Tooltip("Word 16 – AI Mine Awareness (radius, internal units).")]
    public float word16_AIMineAwareness = 120f;

    [Tooltip("Word 17 – AI Bullet Awareness (radius, internal units).")]
    public float word17_AIBulletAwareness = 120f;

    [Tooltip("Word 18 – Player Mine Awareness (radius, internal units).")]
    public float word18_PlayerMineAwareness = 0f;

    [Tooltip("Word 19 – Player Bullet Awareness (radius, internal units).")]
    public float word19_PlayerBulletAwareness = 40f;

    [Tooltip("Word 20 – Survival Mode Activity Flag (nonzero enables).")]
    public int word20_SurvivalModeFlag = 1;

    [Tooltip("Word 21 – Tank Aggressiveness Bias (0..1; 0=no bias, 1=always bias towards player).")]
    [Range(0f, 1f)] public float word21_Aggressiveness = 0.03f;

    [Header("Words 22–28 (Queueing, Speed, Turn, Obstacle Awareness)")]
    [Tooltip("Word 22 – Total Queueing Movements Value (1..10 typical).")]
    [Range(1, 10)] public int word22_QueueCount = 4;

    [Tooltip("Word 23 – Tank Max Speed (internal units per frame).")]
    public float word23_MaxSpeed = 1.2f;

    [Tooltip("Word 26 – Tank Turn Speed (radians per frame).")]
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

    // === Internal state (unchanged) ===
    private int turningTimerFrames = 0;
    private int randomTurningValue;

    private bool survivalModeFlag;
    private bool largeTurnFlag;
    private bool sequenceTurnFlag;
    private bool randomTurnFlag;
    private bool mineMovementOverrideFlag = false;

    private enum QueueSource { None, RandomTurn, LargeTurn }
    private readonly Queue<(float angleDeg, QueueSource src)> movementQueue = new();
    private QueueSource lastQueueSource = QueueSource.None;

    private float currentVelocity_InternalPerFrame = 0f;
    private float requestedSpeed_InternalPerFrame = 0f;
    private float turnTargetAngleDeg;
    private bool moveForwardThisFrame = true;

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
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        turnTargetAngleDeg = getFacingAngleDeg();
        PickNewRandomTurningValue();
    }

    void Update()
    {
        HandleMovementOpportunityCadence();
        ProcessTurnTargetPriority();
        RotateBodyTowardTurnTarget();
        ComputeRequestedSpeed();
        ApplyAccelDecelAndStuns();
        MoveAgentAlongCurrentFacing();
    }

    // ───────────────────────── step 1: cadence ─────────────────────────
    private void HandleMovementOpportunityCadence()
    {
        if (turningTimerFrames >= randomTurningValue)
        {
            PickNewRandomTurningValue();
            turningTimerFrames = 0;

            if (word20_SurvivalModeFlag != 0 && DetectThreatsForSurvival())
            {
                survivalModeFlag = true;
            }
            else
            {
                if (IsThereObstacleAhead())
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
    }

    // ───────────────────────── step 2: decide turn target ─────────────────────────
    private void ProcessTurnTargetPriority()
    {
        if (mineMovementOverrideFlag)
        {
            mineMovementOverrideFlag = false;
            return;
        }

        if (survivalModeFlag)
        {
            Vector2 away = ComputeSurvivalEscapeVector();
            if (away.sqrMagnitude > 0.0001f)
                turnTargetAngleDeg = arctanDeg(away.y, away.x);
            survivalModeFlag = false;
        }
        else if (largeTurnFlag)
        {
            bool overwriteAllowed = (lastQueueSource != QueueSource.LargeTurn);
            if (overwriteAllowed)
            {
                var (leftOpen, rightOpen) = ProbeLeftRightForLargeTurn_NavMesh(getlookDistance());

                if (leftOpen || rightOpen)
                {
                    float chosenDelta = (leftOpen && rightOpen)
                        ? (Random.value < 0.5f ? +90f : -90f)
                        : (leftOpen ? +90f : -90f);
                    EnqueueMiniTurnsRelative(chosenDelta, QueueSource.LargeTurn);
                }
                else
                {
                    turnTargetAngleDeg = getOppositeFacingAngleDeg();
                    movementQueue.Clear();
                    lastQueueSource = QueueSource.None;
                }
            }

            largeTurnFlag = false;
            sequenceTurnFlag = true; // consume next queued mini-turn
        }
        else if (randomTurnFlag)
        {
            float randomDelta = Random.Range(-word13_RandomTurnMaxAngle, word13_RandomTurnMaxAngle);

            if (playerTarget != null && word21_Aggressiveness > 0f)
            {
                makeRandomDegreeMoreBiasTowardsPlayerPosition(ref randomDelta);
            }

            EnqueueMiniTurnsRelative(randomDelta, QueueSource.RandomTurn);
            randomTurnFlag = false;
            sequenceTurnFlag = true;
        }

        if (sequenceTurnFlag)
        {
            if (movementQueue.Count > 0)
            {
                var entry = movementQueue.Dequeue();
                lastQueueSource = entry.src;
                turnTargetAngleDeg = convertToDegree(entry.angleDeg);
            }
            else
                lastQueueSource = QueueSource.None;
            sequenceTurnFlag = false;
        }
        printLog("movementQueue count: " + movementQueue.Count);
    }

    private void makeRandomDegreeMoreBiasTowardsPlayerPosition(ref float randomDelta)
    {
        Vector2 initialDir = DirFromAngle(getFacingAngleDeg() + randomDelta).normalized;
        Vector2 toPlayer = ((Vector2)(playerTarget.position - transform.position)).normalized;
        Vector2 mixed = Vector2.Lerp(initialDir, toPlayer, Mathf.Clamp01(word21_Aggressiveness)).normalized;
        float mixedDelta = Mathf.DeltaAngle(getAngleFromDir(initialDir), getAngleFromDir(mixed));
        randomDelta += mixedDelta;
    }

    // ───────────────────────── step 3: rotate body ─────────────────────────
    private void RotateBodyTowardTurnTarget()
    {
        float facing = getFacingAngleDeg();
        float deltaToTarget = Mathf.DeltaAngle(facing, turnTargetAngleDeg);

        moveForwardThisFrame = Mathf.Abs(deltaToTarget) <= 90f;

        float degPerFrame = word26_TurnSpeedRadPerFrame * Mathf.Rad2Deg;
        float degPerSecond = degPerFrame * simulationFps;
        float step = degPerSecond * Time.deltaTime;

        if (Mathf.Abs(deltaToTarget) <= step)
            SetFacingAngleDeg(turnTargetAngleDeg);
        else
            SetFacingAngleDeg(facing + Mathf.Sign(deltaToTarget) * step);
    }

    // ───────────────────────── step 4: request speed ─────────────────────────
    private void ComputeRequestedSpeed()
    {
        requestedSpeed_InternalPerFrame = Mathf.Max(0f, word23_MaxSpeed);
    }

    // ───────────────────────── step 5: accel/decel/stuns/clamp ─────────────────────────
    private void ApplyAccelDecelAndStuns()
    {
        float requiredTurnNow = Mathf.Abs(Mathf.DeltaAngle(getFacingAngleDeg(), turnTargetAngleDeg));

        if (requiredTurnNow > word27_MaxTurnPivotDeg || requestedSpeed_InternalPerFrame < currentVelocity_InternalPerFrame)
        {
            currentVelocity_InternalPerFrame *= Mathf.Clamp01(word12_DecelMul);
        }
        else if (requestedSpeed_InternalPerFrame > currentVelocity_InternalPerFrame)
        {
            currentVelocity_InternalPerFrame += word11_Accel;
        }

        if (bulletStunTimerFrames > 0 || mineStunTimerFrames > 0)
            currentVelocity_InternalPerFrame = 0f;

        currentVelocity_InternalPerFrame = Mathf.Min(currentVelocity_InternalPerFrame, word23_MaxSpeed);

        if (bulletStunTimerFrames > 0) bulletStunTimerFrames--;
        if (mineStunTimerFrames > 0) mineStunTimerFrames--;
    }

    // ───────────────────────── step 6: move ─────────────────────────
    private void MoveAgentAlongCurrentFacing()
    {
        Vector2 dir2D = transform.up * (moveForwardThisFrame ? 1f : -1f);
        float unitsPerSecond = currentVelocity_InternalPerFrame * simulationFps * internalUnitsToUnity;

        agent.speed = unitsPerSecond;

        Vector3 shortAhead = transform.position + (Vector3)(dir2D);
        agent.SetDestination(shortAhead);
    }

    // ───────────────────────── utilities (unchanged logic) ─────────────────────────

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
        int max = Mathf.Max(a, b) + 1; // exclusive
        randomTurningValue = Random.Range(min, max);
    }

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
                var rb = h.attachedRigidbody;
                if (rb != null)
                {
                    float approaching = Vector2.Dot(rb.linearVelocity.normalized, to.normalized);
                    if (approaching <= 0f) continue;
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
            if (rb == null) return true;
            Vector2 to = (Vector2)(transform.position - h.transform.position);
            if (to.sqrMagnitude < 0.0001f) return true;
            float dot = Vector2.Dot(rb.linearVelocity.normalized, to.normalized);
            if (dot > 0f) return true;
        }
        return false;
    }

    private float getlookDistance()
    {
        float N = Mathf.Max(0f, word28_ObstacleAwareness / 2f);
        float lookDistInternal = Mathf.Max(1f, currentVelocity_InternalPerFrame * N + 2f);
        return lookDistInternal * internalUnitsToUnity;
    }

    bool IsThereObstacleAhead(float lookingDistance)
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * lookingDistance;
        return agent.Raycast(end, out _);
    }

    bool IsThereObstacleAhead()
    {
        return IsThereObstacleAhead(getlookDistance());
    }

    (bool leftOpen, bool rightOpen) ProbeLeftRightForLargeTurn_NavMesh(float distanceUnity)
    {
        Vector3 start = transform.position;
        Vector3 leftEnd = start + (Vector3)Rotate90(transform.up, +1) * distanceUnity;
        Vector3 rightEnd = start + (Vector3)Rotate90(transform.up, -1) * distanceUnity;

        bool leftBlocked = agent.Raycast(leftEnd, out _);
        bool rightBlocked = agent.Raycast(rightEnd, out _);

        return (!leftBlocked, !rightBlocked);
    }

    static Vector2 Rotate90(Vector2 v, int leftPlusOneRightMinusOne) =>
        leftPlusOneRightMinusOne >= 0 ? new Vector2(-v.y, v.x) : new Vector2(v.y, -v.x);

    private void EnqueueMiniTurnsRelative(float deltaAngleDeg, QueueSource src)
    {
        float start = getFacingAngleDeg();
        float target = convertToDegree(start + deltaAngleDeg);

        movementQueue.Clear();
        lastQueueSource = src;

        int count = Mathf.Max(1, word22_QueueCount);
        float step = Mathf.DeltaAngle(start, target) / count;

        float accum = start;
        for (int i = 1; i <= count; i++)
        {
            accum = convertToDegree(start + step * i);
            movementQueue.Enqueue((accum, src));
        }
    }

    private float getFacingAngleDeg()
    {
        return arctanDeg(transform.up.y, transform.up.x);
    }

    private float getOppositeFacingAngleDeg()
    {
        return convertToDegree(getFacingAngleDeg() + 180f);
    }

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

    private static float arctanDeg(float y, float x)
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// converts a number to a degree: between 0 and 360
    /// </summary>
    private float convertToDegree(float num)
    {
        return Mathf.Repeat(num, 360f);
    }

    private static float getAngleFromDir(Vector2 dir) => arctanDeg(dir.y, dir.x);

    private static float SignedAngleDeg(float fromDeg, float toDeg) => Mathf.DeltaAngle(fromDeg, toDeg);

    private int frameCount = 0;
    private void printLog(string message)
    {
        if (frameCount == 0)
            Debug.Log(message);
        else if (frameCount == 60)
            frameCount = -1;
        frameCount++;
    }

    // Public hooks (unchanged)
    public void NotifyMineMovementOverride() => mineMovementOverrideFlag = true;
    public void ForceMovementOpportunityNextFrame() => turningTimerFrames = Mathf.Max(turningTimerFrames, randomTurningValue);
    public void ClearQueue() { movementQueue.Clear(); lastQueueSource = QueueSource.None; }
}
