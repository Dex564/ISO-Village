using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Поселенец: ходит по клеткам по найденному пути и добывает ресурсы.
/// Физики нет: позиция просто двигается от центра клетки к центру следующей.
/// Ручные приказы: MoveTo(клетка) и Mine(ресурс).
/// Автоматически: когда свободен, берёт у JobBoard ближайший помеченный ресурс, который умеет добывать.
/// </summary>
public class Settler : MonoBehaviour
{
    private enum State { Idle, Moving, Mining }

    private static readonly int XHash = Animator.StringToHash("X");
    private static readonly int YHash = Animator.StringToHash("Y");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    // 8 направлений на экране, в порядке углов 0, 45, 90, ... градусов.
    private static readonly Vector2[] Dirs8 =
    {
        new Vector2( 1,  0), new Vector2( 1,  1), new Vector2( 0,  1), new Vector2(-1,  1),
        new Vector2(-1,  0), new Vector2(-1, -1), new Vector2( 0, -1), new Vector2( 1, -1)
    };

    [Header("Раса и движение")]
    [Tooltip("Раса определяет скорость ходьбы и умения. Если не задана, умеет всё со скоростью 1.")]
    [SerializeField] private RaceData race;
    [Tooltip("Скорость ходьбы, если раса не задана (единиц мира в секунду)")]
    [SerializeField] private float fallbackMoveSpeed = 1.5f;
    [SerializeField] private Vector2Int startCell = Vector2Int.zero;

    [Header("Работа")]
    [SerializeField] private bool autoWork = true;
    [SerializeField] private float jobCheckInterval = 0.75f;
    [Tooltip("Сколько секунд после ручного приказа идти поселенец не ищет работу")]
    [SerializeField] private float manualOverrideSeconds = 3f;

    [Header("Ссылки")]
    [SerializeField] private Animator animator;

    [Header("Отладка")]
    [SerializeField] private bool drawPath = true;

    /// <summary>Последняя клетка, до центра которой поселенец дошёл.</summary>
    public Vector2Int CellPos { get; private set; }
    public RaceData Race => race;

    private float MoveSpeed => race != null ? race.moveSpeed : fallbackMoveSpeed;

    private State state = State.Idle;
    private bool initialized;

    private List<Vector2Int> path = new List<Vector2Int>();
    private int pathIndex;

    private ResourceNode targetNode;
    private float mineTimer;
    private float nextJobCheck;
    private bool holdAutoWork;

    private Vector2 facing = new Vector2(0f, -1f);

    // ---------- Умения ----------

    /// <summary>Может ли этот поселенец добывать такой ресурс (по навыку и его уровню).</summary>
    public bool CanWork(ResourceNodeData resource)
    {
        if (resource == null) return false;
        return race == null || race.CanWork(resource.skill, resource.requiredSkillLevel);
    }

    private float SkillSpeed(ResourceNodeData resource)
    {
        return race != null ? race.GetSpeed(resource.skill) : 1f;
    }

    // ---------- Unity ----------

    private void Update()
    {
        if (!EnsureInitialized()) return;

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Moving: UpdateMoving(); break;
            case State.Mining: UpdateMining(); break;
        }
    }

    private void OnDestroy()
    {
        ReleaseTarget();
    }

    // Карта создаётся в генераторе, поэтому ждём её готовности.
    private bool EnsureInitialized()
    {
        if (initialized) return true;

        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0) return false;

        CellPos = startCell;
        transform.position = map.CellToWorld(CellPos);
        initialized = true;
        UpdateAnimator(false);
        return true;
    }

    // ---------- Ручные приказы ----------

    /// <summary>Идти в клетку goal. Возвращает false, если пути нет (тогда прошлое занятие не прерывается).</summary>
    public bool MoveTo(Vector2Int goal)
    {
        if (!initialized) return false;

        Vector2Int from = GetPlanningStart();
        List<Vector2Int> newPath = Pathfinder.FindPath(WorldMap.Instance, from, goal);
        if (newPath == null)
        {
            Debug.Log($"Settler: пути в клетку {goal} нет.");
            return false;
        }

        if (from != CellPos) newPath.Insert(0, from);

        StopWork();
        holdAutoWork = true;
        BeginPath(newPath);
        return true;
    }

    /// <summary>Подойти к ресурсу и добывать, пока он не кончится (помечает его к добыче).</summary>
    public bool Mine(ResourceNode node)
    {
        if (!initialized || node == null || node.IsDepleted) return false;

        if (!CanWork(node.Data))
        {
            Debug.Log($"Settler: не умеет добывать «{node.Data.displayName}».");
            return false;
        }

        if (node.ClaimedBy != null && node.ClaimedBy != this)
        {
            Debug.Log("Settler: этот ресурс уже добывает другой поселенец.");
            return false;
        }

        Vector2Int from = GetPlanningStart();
        List<Vector2Int> newPath = Pathfinder.FindPathToNeighbour(WorldMap.Instance, from, node.CellPos);
        if (newPath == null)
        {
            Debug.Log("Settler: к ресурсу не подойти.");
            return false;
        }

        if (from != CellPos) newPath.Insert(0, from);

        StopWork();
        if (JobBoard.Instance != null) JobBoard.Instance.Designate(node);
        node.ClaimedBy = this;
        targetNode = node;
        BeginPath(newPath);
        return true;
    }

    // Откуда считать новый путь: посреди шага сначала доходим до клетки, к которой идём.
    private Vector2Int GetPlanningStart()
    {
        if (state == State.Moving && pathIndex < path.Count) return path[pathIndex];
        return CellPos;
    }

    // ---------- Состояния ----------

    private void UpdateIdle()
    {
        if (!autoWork || Time.time < nextJobCheck) return;
        nextJobCheck = Time.time + jobCheckInterval;

        JobBoard board = JobBoard.Instance;
        if (board == null) return;

        if (board.TryClaim(this, out ResourceNode node, out List<Vector2Int> newPath))
        {
            targetNode = node;
            BeginPath(newPath);
        }
    }

    private void BeginPath(List<Vector2Int> newPath)
    {
        path = newPath;
        pathIndex = 0;

        if (path.Count == 0)
        {
            ArriveAtDestination();
            return;
        }

        state = State.Moving;
    }

    private void UpdateMoving()
    {
        if (TargetLost())
        {
            StopWork();
            return;
        }

        WorldMap map = WorldMap.Instance;
        float budget = MoveSpeed * Time.deltaTime; // сколько можем пройти за этот кадр

        while (budget > 0f && pathIndex < path.Count)
        {
            Vector3 target = map.CellToWorld(path[pathIndex]);
            Vector3 delta = target - transform.position;
            float dist = delta.magnitude;

            if (dist > 0.0001f) SetFacing(delta);

            if (dist <= budget)
            {
                transform.position = target;
                CellPos = path[pathIndex];
                pathIndex++;
                budget -= dist;
            }
            else
            {
                transform.position += delta / dist * budget;
                budget = 0f;
            }
        }

        if (pathIndex >= path.Count) ArriveAtDestination();
        else UpdateAnimator(true);
    }

    private void ArriveAtDestination()
    {
        path.Clear();
        pathIndex = 0;

        if (targetNode != null && !TargetLost())
        {
            SetFacing(targetNode.transform.position - transform.position);
            mineTimer = 0f;
            state = State.Mining;
        }
        else
        {
            ReleaseTarget();
            state = State.Idle;

            if (holdAutoWork)
            {
                nextJobCheck = Time.time + manualOverrideSeconds;
                holdAutoWork = false;
            }
        }

        UpdateAnimator(false);
    }

    private void UpdateMining()
    {
        if (TargetLost())
        {
            StopWork();
            return;
        }

        ResourceNodeData resource = targetNode.Data;
        float seconds = resource.GetWorkSeconds(SkillSpeed(resource));

        mineTimer += Time.deltaTime;
        if (mineTimer < seconds) return;
        mineTimer -= seconds;

        int got = targetNode.Harvest(resource.yieldPerAction);
        if (got > 0) ResourceService.Add(resource.yieldType, got);

        if (targetNode.IsDepleted) StopWork();
    }

    // Цель пропала: исчерпана или игрок снял метку.
    private bool TargetLost()
    {
        return targetNode != null && (targetNode.IsDepleted || !targetNode.IsDesignated);
    }

    private void StopWork()
    {
        ReleaseTarget();
        mineTimer = 0f;
        path.Clear();
        pathIndex = 0;
        state = State.Idle;
        UpdateAnimator(false);
    }

    private void ReleaseTarget()
    {
        if (targetNode != null && JobBoard.Instance != null)
            JobBoard.Instance.Release(targetNode, this);

        targetNode = null;
    }

    // ---------- Анимация ----------

    // Из вектора движения получаем одно из 8 экранных направлений (X и Y от -1 до 1).
    private void SetFacing(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f) return;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt(angle / 45f);
        index = (index % 8 + 8) % 8;
        facing = Dirs8[index];
    }

    private void UpdateAnimator(bool moving)
    {
        if (animator == null) return;

        animator.SetFloat(XHash, facing.x);
        animator.SetFloat(YHash, facing.y);
        animator.SetBool(IsMovingHash, moving);
    }

    // ---------- Отладка ----------

    private void OnDrawGizmos()
    {
        if (!drawPath || !Application.isPlaying || WorldMap.Instance == null) return;

        Gizmos.color = Color.yellow;
        Vector3 prev = transform.position;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Vector3 p = WorldMap.Instance.CellToWorld(path[i]);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, state.ToString());
#endif
    }
}