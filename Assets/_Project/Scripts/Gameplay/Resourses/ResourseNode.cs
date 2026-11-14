using UnityEngine;

/// <summary>
/// Конкретный ресурс на карте (одна груда камней). Вешается на префаб.
/// Что это за ресурс, говорит Data (общий для всех камней), а запас у каждого свой.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    private static readonly Color DesignatedTint = new Color(1f, 0.85f, 0.45f, 1f);

    [SerializeField] private ResourceNodeData data;
    [SerializeField, Min(0)] private int remaining;   // виден в инспекторе для отладки

    public ResourceNodeData Data => data;
    public int Remaining => remaining;
    public int InitialPool { get; private set; }
    public bool IsDepleted => remaining <= 0;

    /// <summary>Клетка, которую занимает ресурс. Задаётся в Init.</summary>
    public Vector2Int CellPos { get; private set; }

    /// <summary>Игрок пометил этот ресурс к добыче.</summary>
    public bool IsDesignated { get; private set; }

    /// <summary>Поселенец, который сейчас занят этим ресурсом.</summary>
    public Settler ClaimedBy { get; set; }

    /// <summary>До этого времени не пробуем искать путь (если к ресурсу не подойти).</summary>
    public float NextPathAttemptTime { get; set; }

    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    /// <summary>Вызывается генератором сразу после создания.</summary>
    public void Init(ResourceNodeData nodeData, Vector2Int cell, int pool)
    {
        data = nodeData;
        CellPos = cell;
        InitialPool = pool;
        remaining = pool;
    }

    /// <summary>Для загрузки сохранения: выставить остаток. 0 значит «уже добыт».</summary>
    public void SetRemaining(int value)
    {
        remaining = Mathf.Clamp(value, 0, InitialPool);
        if (IsDepleted) Deplete();
    }

    public void SetDesignated(bool value)
    {
        IsDesignated = value;
        if (sr != null) sr.color = value ? baseColor * DesignatedTint : baseColor;
    }

    /// <summary>Забирает до requested единиц и возвращает, сколько реально взято.</summary>
    public int Harvest(int requested)
    {
        if (IsDepleted || requested <= 0) return 0;

        int taken = Mathf.Min(requested, remaining);
        remaining -= taken;

        ResourceNodeSave.Record(CellPos, remaining);

        if (IsDepleted) Deplete();
        return taken;
    }

    private void Deplete()
    {
        WorldMap map = WorldMap.Instance;
        if (map != null) map.Free(CellPos, Vector2Int.one, gameObject);

        if (JobBoard.Instance != null) JobBoard.Instance.Remove(this);

        Destroy(gameObject);
    }
}