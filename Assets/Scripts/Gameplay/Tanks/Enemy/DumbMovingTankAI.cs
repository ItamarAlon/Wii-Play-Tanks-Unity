using Assets.Scripts.Core;
using Assets.Scripts.Gameplay.Tanks.Enemy;
using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wii Play Tanks - Dumbest Moving Tank (Movement-Only AI) – refactor only (behavior unchanged)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DumbestMovingTankAI : EnemyAI
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

    //private bool survivalModeFlag;
    //private bool largeTurnFlag;
    //private bool sequenceTurnFlag;
    //private bool randomTurnFlag;
    //private bool mineMovementOverrideFlag = false;

    private class MovementQueue
    {
        private readonly Queue<float> movementQueue;
        private readonly int capacity;
        public enum QueueSource { None, RandomTurn, LargeTurn }
        public QueueSource CurrentQueueSource { get; private set; } = QueueSource.None;
        public bool Empty => movementQueue.Count == 0;
        public float? NextAngle
        {
            get
            {
                if (Empty) 
                    return null;
                float returnValue = movementQueue.Dequeue();
                if (Empty)
                    CurrentQueueSource = QueueSource.None;
                return returnValue;
            }
        }

        public MovementQueue(int capacity)
        {
            this.capacity = Mathf.Max(1, capacity);
            movementQueue = new Queue<float>(capacity);
        }
        public void ClearQueue() 
        { 
            movementQueue.Clear();
            CurrentQueueSource = QueueSource.None;
        }
        public void EnqueueMiniTurnsRelative(float startingAngle, float deltaAngle, QueueSource src)
        {
            if (!CanEnterNewValues(src)) return;
            ClearQueue();
            CurrentQueueSource = src;
            float step = deltaAngle / capacity;

            float nextAngle;
            for (int i = 1; i <= capacity; i++)
            {
                nextAngle = Utils.ConvertToAngle(startingAngle + step * i);
                movementQueue.Enqueue(nextAngle);
            }
        }
        public bool CanEnterNewValues(QueueSource src)
        {
            switch (CurrentQueueSource)
            {
                default:
                case QueueSource.None: //if empty
                    return true;
                case QueueSource.LargeTurn:
                    return false;
                case QueueSource.RandomTurn:
                    return src == QueueSource.LargeTurn;
            }
        }
        public bool CanMakeLargeTurn() => CanEnterNewValues(QueueSource.LargeTurn);
    }
    private MovementQueue movementQueue;

    private float currentVelocity_InternalPerFrame = 0f;
    private float requestedSpeed_InternalPerFrame = 0f;
    private float turnTargetAngleDeg;
    private bool moveForwardThisFrame = true;
    private Vector2 ForwardDirection => transform.up.normalized;

    [Header("Stun Timers (optional, set by shooter/mine systems)")]
    public int bulletStunTimerFrames = 0; // Word 42
    public int mineStunTimerFrames = 0;   // Word 10

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        movementQueue = new MovementQueue(word22_QueueCount);
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        turnTargetAngleDeg = getFacingAngleDeg();
        pickNewRandomTurningValue();
    }

    void Start()
    {
        StartCoroutine(continuesMovementOpportunityRoutine());
    }

    void Update()
    {
        //StartCoroutine(movementOpportunityRoutine());
        //calculateNewTurn();
        RotateBodyTowardTurnTarget();
        ComputeRequestedSpeed();
        ApplyAccelDecelAndStuns();
        MoveAgentAlongCurrentFacing();
    }

    private enum MoveRequest { Large, Survival, None};
    private MoveRequest wantedMove = MoveRequest.None;
    private bool SurvivalTurnRequested => wantedMove == MoveRequest.Survival;
    private bool LargeTurnRequested => wantedMove == MoveRequest.Large;

    private IEnumerator movementOpportunityRoutine()
    {
        int randomTimer = pickNewRandomTurningValue();
        for (int i = 0; i < randomTimer; i++)
            yield return null;
        calculateNewTurn();
    }

    private IEnumerator continuesMovementOpportunityRoutine()
    {
        while (true)
        {
            int randomTimer = pickNewRandomTurningValue();
            for (int i = 0; i < randomTimer; i++)
                yield return null;
            calculateNewTurn();
        }
    }

    private void calculateNewTurn()
    {
        handleMovementOpportunityCadence();
        turnTargetAngleDeg = generateAngleToMoveToBasedOnRequested();
    }

    // ───────────────────────── step 1: cadence ─────────────────────────
    private void handleMovementOpportunityCadence()
    {
        if (word20_SurvivalModeFlag != 0 && DetectThreatsForSurvival())
            wantedMove = MoveRequest.Survival;
        else if (IsThereObstacleAhead())
            wantedMove = MoveRequest.Large;
        else
            wantedMove = MoveRequest.None;
        Debug.Log(wantedMove);
    }

    // ───────────────────────── step 2: decide turn target ─────────────────────────
    private float generateAngleToMoveToBasedOnRequested()
    {
        if (SurvivalTurnRequested)
        {
            //Debug.Log("Survival");
            movementQueue.ClearQueue();
            return computeSurvivalEscapeAngle();
        }
        if (LargeTurnRequested && movementQueue.CanMakeLargeTurn())
            enterAnglesForLargeTurnToQueue();
        else if (movementQueue.Empty)
            enterAnglesForRandomTurnToQueue();

        wantedMove = MoveRequest.None;
        return movementQueue.NextAngle.Value;
    }
    //private void ProcessTurnTargetPriority()
    //{
    //    if (mineMovementOverrideFlag)
    //    {
    //        mineMovementOverrideFlag = false;
    //        return;
    //    }
    //
    //    if (survivalModeFlag)
    //    {
    //        turnTargetAngleDeg = computeSurvivalEscapeAngle();
    //        survivalModeFlag = false;
    //    }
    //    else if (largeTurnFlag)
    //    {
    //        MovementQueue.QueueSource src = MovementQueue.QueueSource.LargeTurn;
    //        if (movementQueue.CanEnterNewValues(src))
    //        {
    //            enterAnglesForLargeTurnToQueue();
    //        }
    //
    //        largeTurnFlag = false;
    //        sequenceTurnFlag = true; // consume next queued mini-turn
    //    }
    //    else if (randomTurnFlag)
    //    {
    //        MovementQueue.QueueSource src = MovementQueue.QueueSource.RandomTurn;
    //        enterAnglesForRandomTurnToQueue();
    //        randomTurnFlag = false;
    //        sequenceTurnFlag = true;
    //    }
    //
    //    if (sequenceTurnFlag)
    //    {
    //        turnTargetAngleDeg = movementQueue.NextAngle.Value;
    //        sequenceTurnFlag = false;
    //    }
    //    //printLog("movementQueue count: " + movementQueue.Count);
    //}

    private float computeSurvivalEscapeAngle()
    {
        Vector2 away = ComputeSurvivalEscapeVector();
        //if (away.sqrMagnitude > 0.0001f)
        //    return arctanDeg(away.y, away.x);
        return arctanDeg(away.y, away.x);
    }

    private void enterAnglesForRandomTurnToQueue()
    {
        Debug.Log("Random");
        float randomTurnAngle = Random.Range(-word13_RandomTurnMaxAngle, word13_RandomTurnMaxAngle);

        if (playerTarget && word21_Aggressiveness > 0f)
            makeRandomAngleMoreBiasTowardsPlayerPosition(ref randomTurnAngle);

        enqueueMiniTurnsRelative(randomTurnAngle, MovementQueue.QueueSource.RandomTurn);
    }

    private void enterAnglesForLargeTurnToQueue()
    {
        Debug.Log("Large");
        var (leftOpen, rightOpen) = ProbeLeftRightForLargeTurn_NavMesh(getlookDistance());

        if (leftOpen || rightOpen)
        {
            float chosenDelta = (leftOpen && rightOpen)
                ? (Random.value < 0.5f ? +90f : -90f)
                : (leftOpen ? +90f : -90f);
            enqueueMiniTurnsRelative(chosenDelta, MovementQueue.QueueSource.LargeTurn);
        }
        else
        {
            turnTargetAngleDeg = getOppositeFacingAngleDeg();
            movementQueue.ClearQueue();
        }
    }

    private void enqueueMiniTurnsRelative(float chosenDelta, MovementQueue.QueueSource src)
    {
        movementQueue.EnqueueMiniTurnsRelative(getFacingAngleDeg(), chosenDelta, src);
    }

    private void makeRandomAngleMoreBiasTowardsPlayerPosition(ref float randomAngle)
    {
        Vector2 randomDirectionVector = Utils.RotateVector(ForwardDirection, randomAngle).normalized;
        Vector2 toPlayer = Utils.VectorFromOnePointToAnother(transform, playerTarget);
        Vector2 aggressivenessBiasVector = Utils.SetMagnitude(toPlayer, word21_Aggressiveness);
        Vector2 adjustedDirectionVector = randomDirectionVector + aggressivenessBiasVector;
        randomAngle = Utils.VectorToAngle(adjustedDirectionVector.normalized);
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

    private int pickNewRandomTurningValue()
    {
        int a = word14_TimerA;
        int b = word15_TimerB;
        if (a == b)
        {
            return a;
        }
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b) + 1; // exclusive
        return Random.Range(min, max);
    }

    private bool DetectThreatsForSurvival()
    {
        bool aiMine = word16_AIMineAwareness > 0f && Physics2D.OverlapCircle(transform.position, word16_AIMineAwareness * internalUnitsToUnity, aiMineMask);
        bool aiBullet = word17_AIBulletAwareness > 0f && AnyBulletHeadingTowardsMe(aiBulletMask, word17_AIBulletAwareness, "Enemy");
        bool playerMine = word18_PlayerMineAwareness > 0f && Physics2D.OverlapCircle(transform.position, word18_PlayerMineAwareness * internalUnitsToUnity, playerMineMask);
        bool playerBullet = word19_PlayerBulletAwareness > 0f && AnyBulletHeadingTowardsMe(playerBulletMask, word19_PlayerBulletAwareness, "Player");
        return aiMine || aiBullet || playerMine || playerBullet;
    }

    private Vector2 ComputeSurvivalEscapeVector()
    {
        Vector2 sum = Vector2.zero;
        AccumulateInverseVectors(aiMineMask, "Enemy", word16_AIMineAwareness, ref sum);
        AccumulateInverseVectors(playerMineMask, "Player", word18_PlayerMineAwareness, ref sum);
        AccumulateInverseVectors(aiBulletMask, "Enemy", word17_AIBulletAwareness, ref sum, onlyTowardMe: true);
        AccumulateInverseVectors(playerBulletMask, "Player", word19_PlayerBulletAwareness, ref sum, onlyTowardMe: true);
        return sum.normalized;
    }

    private void AccumulateInverseVectors(LayerMask mask, string tag, float radiusInternal, ref Vector2 sum, bool onlyTowardMe = false)
    {
        if (radiusInternal <= 0f) return;
        float r = radiusInternal * internalUnitsToUnity;
        var hits = Physics2D.OverlapCircleAll(transform.position, r, mask);
        foreach (var h in hits)
        {
            if (!h.CompareTag(tag)) 
                continue;

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

    private bool AnyBulletHeadingTowardsMe(LayerMask mask, float radiusInternal, string tag)
    {
        float r = radiusInternal * internalUnitsToUnity;
        var hits = Physics2D.OverlapCircleAll(transform.position, r, mask);
        foreach (var h in hits)
        {
            if (!h.CompareTag(tag)) continue;
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

    private float getFacingAngleDeg()
    {
        return arctanDeg(transform.up.y, transform.up.x);
    }

    private float getOppositeFacingAngleDeg()
    {
        return Utils.ConvertToAngle(getFacingAngleDeg() + 180f);
    }

    private void SetFacingAngleDeg(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        transform.up = dir.normalized;
    }

    private static float arctanDeg(float y, float x)
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
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
    //public void NotifyMineMovementOverride() => mineMovementOverrideFlag = true;
    public void ForceMovementOpportunityNextFrame() => turningTimerFrames = Mathf.Max(turningTimerFrames, randomTurningValue);
}
