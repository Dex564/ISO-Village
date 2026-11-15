using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Поселенец: ходит по клеткам по найденному пути и добывает ресурсы.
/// У него есть имя, раса и навыки с уровнями, которые растут от работы.
/// Ручные приказы: MoveTo(клетка) и Mine(ресурс).
/// Автоматически: когда свободен, берёт у JobBoard ближайший помеченный ресурс, который умеет добывать.
/// </summary>
public class Settler : MonoBehaviour
{
    private enum State { Idle, Moving, Mining }

    /// <summary>Все поселенцы в сцене (для выбора мышью и списков).</summary>
    public static readonly List<Settler> All = new List<Settler>();

    private const int FallbackMaxLevel = 3; // потолок навыков, если раса не задана

    private static readonly int XHash = Animator.StringToHash("X");
    private static readonly int YHash = Animator.StringToHash("Y");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    // 8 направлений на экране, в порядке углов 0, 45, 90, ... градусов.
    private static readonly Vector2[] Dirs8 =
    {
        new Vector2( 1,  0), new Vector2( 1,  1), new Vector2( 0,  1), new Vector2(-1,  1),
        new Vector2(-1,  0), new Vector2(-1, -1), new Vector2( 0, -1), new Vector2( 1, -1)
    };

    [Header("Личность")]
    [Tooltip("Если пусто, берётся имя объекта")]
    [SerializeField] private string settlerName;
    [SerializeField] private RaceData race;
    [SerializeField] private SettlerSkills skills = new SettlerSkills();

    [Header("Движение")]
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

    // ----- Публичные данные (для интерфейса и других систем) -----

    public string DisplayName => string.IsNullOrEmpty(settlerName) ? gameObject.name : settlerName;
    public RaceData Race => race;
    public SettlerSkills Skills => skills;

    /// <summary>Последняя клетка, до центра которой поселенец дошёл.</summary>
    public Vector2Int CellPos { get; private set; }

    /// <summary>Текст состояния для панели информации.</summary>
    public string StatusText
    {
        get
        {
            switch (state)
            {
                case State.Moving:
                    return targetNode != null ? $"Идёт к ресурсу: {targetNode.Data.displayName}" : "Идёт";
                case State.Mining:
                    return targetNode != null
                        ? $"Добывает: {targetNode.Data.displayName} (осталось {targetNode.Remaining})"
                        : "Работает";
                default:
                    return "Свободен";
            }
        }
    }

    private float MoveSpeed => race != null ? race.moveSpeed : fallbackMoveSpeed;

    private State state = State.Idle;
    private bool initialized;

    private List<Vector2Int> path = new List<Vector2Int>();
    private int pathIndex;

    private ResourceNode targetNode;
    private float mineTimer;
    private float nextJobCheck;
    private bool holdAutoWork;

    private SpriteRenderer bodyRenderer;
    private SpriteRenderer marker;

    private Vector2 facing = new Vector2(0f, -1f);

    // ---------- Умения ----------

    /// <summary>Потолок уровня навыка для этого поселенца (задаёт раса).</summary>
    public int GetMaxLevel(WorkSkill skill)
    {
        return race != null ? race.GetMaxLevel(skill) : FallbackMaxLevel;
    }

    /// <summary>Может ли добывать такой ресурс: хватает ли уровня нужного навыка.</summary>
    public bool CanWork(ResourceNodeData resource)
    {
        return skills.CanWork(resource);
    }

    // Скорость = эффект уровня навыка * особенности расы.
    private float SkillSpeed(ResourceNodeData resource)
    {
        float levelBonus = SkillProgression.SpeedMultiplier(skills.GetLevel(resource.skill));
        float raceBonus = race != null ? race.GetWorkSpeedMultiplier(resource.skill) : 1f;
        return levelBonus * raceBonus;
    }

    private void GainXp(ResourceNodeData resource)
    {
        float multiplier = race != null ? race.GetXpMultiplier(resource.skill) : 1f;
        float xp = resource.xpPerAction * multiplier;

        if (skills.AddXp(resource.skill, xp, GetMaxLevel(resource.skill)))
        {
            Debug.Log($"{DisplayName}: навык «{resource.skill.DisplayName()}» повышен до {skills.GetLevel(resource.skill)} уровня.");
        }
    }

    // ---------- Unity ----------

    private void Awake()
    {
        if (race == null) Debug.LogWarning($"Settler {name}: не назначена раса, навыки будут по умолчанию.");
        skills.EnsureFor(race);

        bodyRenderer = animator != null
            ? animator.GetComponentInChildren<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

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

    // ---------- Выбор мышью ----------

    /// <summary>Попадает ли точка мира (с запасом padding) в спрайт поселенца.</summary>
    public bool ContainsPoint(Vector2 world, float padding)
    {
        if (bodyRenderer == null) return false;

        Bounds b = bodyRenderer.bounds;
        b.Expand(padding * 2f);
        return world.x >= b.min.x && world.x <= b.max.x && world.y >= b.min.y && world.y <= b.max.y;
    }

    public void SetSelected(bool selected)
    {
        if (selected && marker == null) CreateMarker();
        if (marker != null) marker.enabled = selected;
    }

    // Зелёный ромб под ногами выбранного поселенца.
    private void CreateMarker()
    {
        var go = new GameObject("SelectionMarker");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        marker = go.AddComponent<SpriteRenderer>();
        marker.sprite = CellSprites.Diamond;
        marker.color = new Color(0.4f, 1f, 0.5f, 0.9f);

        if (System.Array.Exists(SortingLayer.layers, l => l.name == "Overlay"))
            marker.sortingLayerName = "Overlay";
        marker.sortingOrder = -1;
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
            Debug.Log($"{DisplayName}: пути в клетку {goal} нет.");
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
            Debug.Log($"{DisplayName}: не умеет добывать «{node.Data.displayName}» " +
                      $"(нужен навык «{node.Data.skill.DisplayName()}» уровня {node.Data.requiredSkillLevel}).");
            return false;
        }

        if (node.ClaimedBy != null && node.ClaimedBy != this)
        {
            Debug.Log($"{DisplayName}: этот ресурс уже добывает другой поселенец.");
            return false;
        }

        Vector2Int from = GetPlanningStart();
        List<Vector2Int> newPath = Pathfinder.FindPathToNeighbour(WorldMap.Instance, from, node.CellPos);
        if (newPath == null)
        {
            Debug.Log($"{DisplayName}: к ресурсу не подойти.");
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

        int taken = targetNode.Harvest(resource.yieldPerAction);
        if (taken > 0)
        {
            // Бонус расы (например, шанс удвоить руду) добавляется сверх запаса ресурса.
            float bonus = race != null ? race.RollYieldMultiplier(resource.skill) : 1f;
            int amount = Mathf.Max(taken, Mathf.RoundToInt(taken * bonus));

            ResourceService.Add(resource.yieldType, amount);
            GainXp(resource);
        }

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
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"{DisplayName}: {state}");
#endif
    }
}