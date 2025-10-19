using Assets.Scripts.Core;
using Assets.Scripts.Gameplay.Tanks.Enemy;
using NUnit.Framework.Internal;
using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TankMovementAI : EnemyAI
{
    [Header("NavMesh / 2D Setup")]
    [Tooltip("Assign your NavMeshAgent (NavMeshPlus 2D: set updateRotation=false, updateUpAxis=false).")]
    public NavMeshAgent agent;
    public Transform hull;

    [Tooltip("Units: internal-units → Unity units scale. (PDF uses internal units; a block = 35.0 units).")]
    public float internalUnitsToUnity = 1f;
    private float offset = 60f;

    [Header("Words 11–15 (Movement Timers & Turn Randomness)")]
    [Tooltip("Word 11 – Tank Acceleration Value (internal units per frame).")]
    public float Acceleration = 0.3f;

    [Tooltip("Word 12 – Tank Deceleration Multiplier (0..1; 0 = instant stop).")]
    public float DecelerationMultiplier = 0.6f;

    [Tooltip("Word 13 – Random Turn Max Angle (degrees).")]
    public float RandomTurnMaxAngle = 30f;

    [Tooltip("Word 14 – Random Timer Boundary A (Movement) (frames).")]
    public int TimerA = 15;

    [Tooltip("Word 15 – Random Timer Boundary B (Movement) (frames).")]
    public int TimerB;

    [Header("Words 16–21 (Survival & Aggressiveness)")]
    [Tooltip("Word 16 – AI Mine Awareness (radius, internal units).")]
    public float EnemyMineAwareness = 120f;

    [Tooltip("Word 17 – AI Bullet Awareness (radius, internal units).")]
    public float EnemyBulletAwareness = 120f;

    [Tooltip("Word 18 – Player Mine Awareness (radius, internal units).")]
    public float PlayerMineAwareness = 0f;

    [Tooltip("Word 19 – Player Bullet Awareness (radius, internal units).")]
    public float PlayerBulletAwareness = 40f;

    [Tooltip("Word 20 – Survival Mode Activity Flag (nonzero enables).")]
    public bool SurvivalModeFlag = true;

    [Tooltip("Word 21 – Tank Aggressiveness Bias (0..1; 0=no bias, 1=always bias towards player).")]
    [Range(-1f, 1f)] public float Aggressiveness = 0.03f;

    [Header("Words 22–28 (Queueing, Speed, Turn, Obstacle Awareness)")]
    [Tooltip("Word 22 – Total Queueing Movements Value (1..10 typical).")]
    [Range(1, 10)] public int QueueCapacity = 4;

    [Tooltip("Word 23 – Tank Max Speed (internal units per frame).")]
    public float MaxSpeed = 1.2f;

    [Tooltip("Word 26 – Tank Turn Speed (degrees per frame).")]
    public float TurnSpeedDegPerFrame = 0.08f;

    [Tooltip("Word 27 – Tank Max Turn Pivot Angle (degrees) above which we decelerate.")]
    public float MaxTurnPivotDeg = 10f;

    [Tooltip("Word 28 – Obstacle Awareness (Movement) look-ahead factor (frames). N = Word28 / 2.")]
    public int ObstacleAwareness = 30;

    private bool enable;
    public override bool Enable
    {
        get => enable;
        set
        {
            enable = value;
            agent.enabled = value;
            if (enable)
            {
                StopCoroutine(movementOpportunityRoutine());
                StartCoroutine(movementOpportunityRoutine());
            }
        }
    }

    [Header("Layers / Detection (2D)")]
    public LayerMask aiBulletMask;
    public LayerMask playerBulletMask;
    public LayerMask aiMineMask;
    public LayerMask playerMineMask;

    [Header("Optional: Player Target (for aggressiveness bias)")]
    public Transform playerTarget;


    private MovementQueue movementQueue;

    private float currentVelocity = 0f;
    private float requestedSpeed = 0f;
    private float turnTargetAngleDeg;
    private bool moveForwardThisFrame = true;
    private Vector2 ForwardDirection => hull.up.normalized;

    [Header("Stun Timers (optional, set by shooter/mine systems)")]
    public int bulletStunTimerFrames = 0; // Word 42
    public int mineStunTimerFrames = 0;   // Word 10

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        movementQueue = new MovementQueue(QueueCapacity);
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        turnTargetAngleDeg = getFacingAngleDeg();
        pickNewRandomTurningValue();
    }

    void Start()
    {
        StartCoroutine(movementOpportunityRoutine());
    }

    void Update()
    {
        if (!Enable) return;
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
        while (Enable)
        {
            int randomTimer = pickNewRandomTurningValue();
            for (int i = 0; i < randomTimer; i++)
                yield return null;
            calculateNewTurn();

            Debug.DrawLine(hull.position, agent.destination);
        }
    }

    private void calculateNewTurn()
    {
        handleMovementOpportunityCadence();
        turnTargetAngleDeg = generateAngleToTurnToBasedOnRequested();
    }

    // ───────────────────────── step 1: cadence ─────────────────────────
    private void handleMovementOpportunityCadence()
    {
        if (SurvivalModeFlag && DetectThreatsForSurvival())
            wantedMove = MoveRequest.Survival;
        else if (IsThereObstacleAhead())
            wantedMove = MoveRequest.Large;
        else
            wantedMove = MoveRequest.None;
        //Debug.Log(wantedMove);
    }

    // ───────────────────────── step 2: decide turn target ─────────────────────────
    private float generateAngleToTurnToBasedOnRequested()
    {
        if (SurvivalTurnRequested)
        {
            Debug.Log("Survival");
            movementQueue.ClearQueue();
            return computeSurvivalEscapeAngle();
        }
        if (LargeTurnRequested && movementQueue.CanMakeLargeTurn())
            enterAnglesForLargeTurnToQueue();
        while (movementQueue.Empty)
            enterAnglesForRandomTurnToQueue();

        wantedMove = MoveRequest.None;
        return movementQueue.NextAngle;
    }

    private float computeSurvivalEscapeAngle()
    {
        Vector2 away = ComputeSurvivalEscapeVector();
        return Utils.VectorToAngle(away);
    }

    private void enterAnglesForRandomTurnToQueue()
    {
        Debug.Log("Random");
        float randomTurnAngle = Random.Range(-RandomTurnMaxAngle, RandomTurnMaxAngle);

        if (playerTarget && Aggressiveness > 0f)
            makeRandomAngleMoreBiasTowardsPlayerPosition(ref randomTurnAngle);

        enqueueMiniTurnsRelative(randomTurnAngle, MovementQueue.QueueSource.RandomTurn);
    }

    private void enterAnglesForLargeTurnToQueue()
    {
        Debug.Log("Large");
        var (leftOpen, rightOpen) = ProbeLeftRightForLargeTurn(getlookDistance());

        if (leftOpen || rightOpen)
        {
            float chosenDelta = (leftOpen && rightOpen)
                ? (Random.value < 0.5f ? +90f : -90f)
                : (leftOpen ? +90f : -90f);
            enqueueMiniTurnsRelative(chosenDelta, MovementQueue.QueueSource.LargeTurn);
        }
        else
        {
            enqueueMiniTurnsRelative(getOppositeFacingAngleDeg(),
                MovementQueue.QueueSource.LargeTurn);
        }
    }

    private void enqueueMiniTurnsRelative(float chosenDelta, MovementQueue.QueueSource src)
    {
        movementQueue.EnqueueMiniTurnsRelative(getFacingAngleDeg(), chosenDelta, src);
    }

    private void makeRandomAngleMoreBiasTowardsPlayerPosition(ref float randomAngle)
    {
        Vector2 randomDirectionVector = Utils.RotateVector(ForwardDirection, randomAngle).normalized;
        Vector2 toPlayer = Utils.VectorFromOnePointToAnother(hull, playerTarget);
        Vector2 aggressivenessBiasVector = Utils.SetMagnitude(toPlayer, Aggressiveness);
        Vector2 adjustedDirectionVector = randomDirectionVector + aggressivenessBiasVector;
        randomAngle = Utils.VectorToAngle(adjustedDirectionVector.normalized);
    }

    // ───────────────────────── step 3: rotate body ─────────────────────────
    private void RotateBodyTowardTurnTarget()
    {
        float facing = getFacingAngleDeg();
        float deltaToTarget = Mathf.DeltaAngle(facing, turnTargetAngleDeg);

        moveForwardThisFrame = Mathf.Abs(deltaToTarget) <= 90f;

        float degPerSecond = TurnSpeedDegPerFrame * offset;
        float step = degPerSecond * Time.deltaTime;

        if (Mathf.Abs(deltaToTarget) <= step)
            SetFacingAngleDeg(turnTargetAngleDeg);
        else
            SetFacingAngleDeg(facing + Mathf.Sign(deltaToTarget) * step);
    }

    // ───────────────────────── step 4: request speed ─────────────────────────
    private void ComputeRequestedSpeed()
    {
        requestedSpeed = Mathf.Max(0f, MaxSpeed);
    }

    // ───────────────────────── step 5: accel/decel/stuns/clamp ─────────────────────────
    private void ApplyAccelDecelAndStuns()
    {
        float requiredTurnNow = Mathf.Abs(Mathf.DeltaAngle(getFacingAngleDeg(), turnTargetAngleDeg));

        if (requiredTurnNow > MaxTurnPivotDeg || requestedSpeed < currentVelocity)
        {
            currentVelocity *= Mathf.Clamp01(DecelerationMultiplier);
        }
        else if (requestedSpeed > currentVelocity)
        {
            currentVelocity += Acceleration;
        }

        if (bulletStunTimerFrames > 0 || mineStunTimerFrames > 0)
            currentVelocity = 0f;

        currentVelocity = Mathf.Min(currentVelocity, MaxSpeed);

        if (bulletStunTimerFrames > 0) bulletStunTimerFrames--;
        if (mineStunTimerFrames > 0) mineStunTimerFrames--;
    }

    // ───────────────────────── step 6: move ─────────────────────────
    private void MoveAgentAlongCurrentFacing()
    {
        Vector2 dir2D = hull.up * (moveForwardThisFrame ? 1f : -1f);
        float unitsPerSecond = currentVelocity * offset;

        agent.speed = unitsPerSecond;

        Vector3 shortAhead = hull.position + (Vector3)(dir2D * getlookDistance());
        agent.SetDestination(shortAhead);
        //agent.Move(shortAhead);
    }

    // ───────────────────────── utilities (unchanged logic) ─────────────────────────

    private int pickNewRandomTurningValue()
    {
        int a = TimerA;
        int b = TimerB;
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
        bool aiMine = EnemyMineAwareness > 0f && Physics2D.OverlapCircle(hull.position, EnemyMineAwareness * internalUnitsToUnity, aiMineMask);
        bool aiBullet = EnemyBulletAwareness > 0f && AnyBulletHeadingTowardsMe(aiBulletMask, EnemyBulletAwareness, "Enemy");
        bool playerMine = PlayerMineAwareness > 0f && Physics2D.OverlapCircle(hull.position, PlayerMineAwareness * internalUnitsToUnity, playerMineMask);
        bool playerBullet = PlayerBulletAwareness > 0f && AnyBulletHeadingTowardsMe(playerBulletMask, PlayerBulletAwareness, "Player");
        return aiMine || aiBullet || playerMine || playerBullet;
    }

    private Vector2 ComputeSurvivalEscapeVector()
    {
        Vector2 sum = Vector2.zero;
        AccumulateInverseVectors(EnemyMineAwareness, ref sum, aiMineMask, "Enemy");
        AccumulateInverseVectors(PlayerMineAwareness, ref sum, playerMineMask, "Player");
        AccumulateInverseVectors(EnemyBulletAwareness, ref sum, aiBulletMask, "Enemy", checkHeadingTowardsMe: true);
        AccumulateInverseVectors(PlayerBulletAwareness, ref sum, playerBulletMask, "Player", checkHeadingTowardsMe: true);
        //AccumulateInverseVectors(getlookDistance(), ref sum, LayerMask.GetMask("Walls"));
        return sum.normalized;
    }

    private void AccumulateInverseVectors(float radiusInternal, ref Vector2 sum, LayerMask? mask = null
        , string tag = null, bool checkHeadingTowardsMe = false)
    {
        if (radiusInternal <= 0f) return;
        float r = radiusInternal * internalUnitsToUnity;
        Collider2D[] objectsInArea;
        if (mask.HasValue)
            objectsInArea = Physics2D.OverlapCircleAll(hull.position, r, mask.Value);
        else
            objectsInArea = Physics2D.OverlapCircleAll(hull.position, r);

        foreach (var objectInArea in objectsInArea)
        {
            if (tag != null && !objectInArea.CompareTag(tag))
                continue;

            Vector2 to = Utils.VectorFromOnePointToAnother(hull, objectInArea.transform);
            if (checkHeadingTowardsMe)
            {
                var rb = objectInArea.attachedRigidbody;
                if (rb)
                {
                    if (!Utils.AreVectorsHeadingTheSameDirection(rb.linearVelocity, to))
                        continue;
                }
            }
            if (to.sqrMagnitude > 0.0001f)
            {
                Vector2 away = (-to).normalized;
                //away = steerAwayFromWallIfBlocked(away);
                sum += away;
            }
        }
    }

    private Vector2 steerAwayFromWallIfBlocked(Vector2 desiredAwayDir)
    {
        float look = getlookDistance();
        Vector3 start = hull.position;
        Vector3 end = start + (Vector3)(desiredAwayDir * look);

        if (agent.Raycast(end, out _))
        {
            Vector2 left = new Vector2(-desiredAwayDir.y, desiredAwayDir.x);
            Vector2 right = new Vector2(desiredAwayDir.y, -desiredAwayDir.x);

            bool leftBlocked = agent.Raycast(start + (Vector3)(left * look), out _);
            bool rightBlocked = agent.Raycast(start + (Vector3)(right * look), out _);

            if (leftBlocked && rightBlocked) return -desiredAwayDir; // boxed in → fallback
            if (!leftBlocked && rightBlocked) return left;
            if (leftBlocked && !rightBlocked) return right;

            // both open: prefer the side closer to current forward to reduce sharp pivots
            return Vector2.Dot(left, ForwardDirection) >= Vector2.Dot(right, ForwardDirection) ? left : right;
        }

        return desiredAwayDir; // clear path along the desired away vector
    }

    private bool AnyBulletHeadingTowardsMe(LayerMask bulletMask, float radiusInternal, string tag)
    {
        float radius = radiusInternal * internalUnitsToUnity;
        var detectedBullets = Physics2D.OverlapCircleAll(hull.position, radius, bulletMask);
        foreach (var bullet in detectedBullets)
        {
            if (!bullet.CompareTag(tag))
                continue;
            var rb = bullet.attachedRigidbody;
            if (rb == null)
                continue;
            Vector2 bulletToTankVector = Utils.VectorFromOnePointToAnother(bullet.transform, hull);
            if (bulletToTankVector.sqrMagnitude < 0.0001f)
                continue;
            if (Utils.AreVectorsHeadingTheSameDirection(rb.linearVelocity, bulletToTankVector)) 
                return true;
        }
        return false;
    }

    private float getlookDistance()
    {
        float N = Mathf.Max(0f, ObstacleAwareness / 2f);
        float lookDistInternal = Mathf.Max(1f, currentVelocity * N + 2f);
        return lookDistInternal;
    }

    bool IsThereObstacleAhead(float lookingDistance)
    {
        Vector3 start = hull.position;
        Vector3 end = start + hull.up * lookingDistance;
        return Physics2D.Raycast(start, hull.up, lookingDistance, LayerMask.GetMask("Walls"));
    }

    bool IsThereObstacleAhead()
    {
        return IsThereObstacleAhead(getlookDistance());
    }

    (bool leftOpen, bool rightOpen) ProbeLeftRightForLargeTurn(float distance)
    {
        Vector3 start = hull.position;
        Vector3 leftEnd = start + (Vector3)Rotate90(hull.up, +1) * distance;
        Vector3 rightEnd = start + (Vector3)Rotate90(hull.up, -1) * distance;

        bool leftBlocked = agent.Raycast(leftEnd, out _);
        bool rightBlocked = agent.Raycast(rightEnd, out _);

        return (!leftBlocked, !rightBlocked);
    }

    static Vector2 Rotate90(Vector2 v, int leftPlusOneRightMinusOne) =>
        leftPlusOneRightMinusOne >= 0 ? new Vector2(-v.y, v.x) : new Vector2(v.y, -v.x);

    private float getFacingAngleDeg()
    {
        return Utils.VectorToAngle(hull.up);
    }

    private float getOppositeFacingAngleDeg()
    {
        return Utils.ConvertToAngle(getFacingAngleDeg() + 180f);
    }

    private void SetFacingAngleDeg(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        hull.up = dir.normalized;
    }

    private int frameCount = 0;
    private void printLog(string message)
    {
        if (frameCount == 0)
            Debug.Log(message);
        else if (frameCount == 60)
            frameCount = -1;
        frameCount++;
    }

    private class MovementQueue
    {
        private readonly Queue<float> movementQueue;
        private readonly int capacity;
        public enum QueueSource { None, RandomTurn, LargeTurn }
        public QueueSource CurrentQueueSource { get; private set; } = QueueSource.None;
        public bool Empty => movementQueue.Count == 0;
        public int Count => movementQueue.Count;
        public float NextAngle
        {
            get
            {
                if (movementQueue.Count == 1)
                    CurrentQueueSource = QueueSource.None;
                return movementQueue.Dequeue();
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
            float target = Utils.ConvertToAngle(startingAngle + deltaAngle);
            float step = Mathf.DeltaAngle(startingAngle, target) / capacity;

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
}
