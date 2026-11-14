using System;
using UnityEngine;

/// <summary>
/// Выделение области мышью и назначение работ.
/// Нажми клавишу инструмента (по умолчанию M = добыча), затем выдели область ЛКМ:
/// все подходящие ресурсы в ней получают метку и попадают на доску задач (JobBoard).
/// Ctrl при отпускании кнопки снимает метки. Esc выходит из режима.
/// Область считается в клетках, поэтому на экране она выглядит как изометрический ромб.
/// </summary>
public class AreaSelector : MonoBehaviour
{
    [Serializable]
    public class DesignationTool
    {
        public string label = "Добыча";
        public KeyCode key = KeyCode.M;
        [Tooltip("Какие ресурсы этот инструмент помечает (по навыку)")]
        public WorkSkill skill = WorkSkill.Mining;
        public Color color = new Color(0.3f, 0.65f, 1f, 0.55f);
    }

    [Header("Ссылки")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SelectionOverlay overlay;
    [SerializeField] private PlacementSystem placement;

    [Header("Инструменты")]
    [SerializeField] private DesignationTool[] tools;

    [Header("Управление")]
    [SerializeField] private int selectMouseButton = 0;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    [SerializeField] private KeyCode removeModifier = KeyCode.LeftControl;
    [SerializeField] private int maxAreaCells = 3000;

    [Header("Цвета")]
    [SerializeField] private Color targetColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color removeColor = new Color(1f, 0.35f, 0.35f, 0.55f);

    private DesignationTool active;
    private bool dragging;
    private Vector2Int dragStart;
    private Vector2Int lastMin, lastMax;
    private bool lastRemove;
    private bool hasLast;
    private int targetCount;
    private bool tooLarge;

    private void Reset()
    {
        tools = new[] { new DesignationTool() };
    }

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0 || overlay == null) return;

        // Режим строительства и выделение не должны работать одновременно (обе используют ЛКМ).
        if (placement != null && placement.IsPlacing)
        {
            if (active != null) ExitMode();
            return;
        }

        HandleToolKeys();
        if (active == null) return;

        if (Input.GetKeyDown(cancelKey))
        {
            if (dragging) EndDrag();
            else ExitMode();
            return;
        }

        Vector2Int raw = map.WorldToCell(mainCamera.ScreenToWorldPoint(Input.mousePosition));

        if (!dragging && Input.GetMouseButtonDown(selectMouseButton) && map.InBounds(raw))
        {
            dragging = true;
            dragStart = raw;
            hasLast = false;
        }

        if (!dragging) return;

        Vector2Int end = ClampToMap(map, raw);
        Vector2Int min = Vector2Int.Min(dragStart, end);
        Vector2Int max = Vector2Int.Max(dragStart, end);
        bool remove = Input.GetKey(removeModifier);

        if (!hasLast || min != lastMin || max != lastMax || remove != lastRemove)
        {
            Redraw(map, min, max, remove);
            lastMin = min;
            lastMax = max;
            lastRemove = remove;
            hasLast = true;
        }

        if (Input.GetMouseButtonUp(selectMouseButton))
        {
            Apply(map, min, max, remove);
            EndDrag();
        }
    }

    // ---------- Режимы ----------

    private void HandleToolKeys()
    {
        if (tools == null) return;

        foreach (DesignationTool tool in tools)
        {
            if (!Input.GetKeyDown(tool.key)) continue;

            if (active == tool) ExitMode();
            else active = tool;
            return;
        }
    }

    private void ExitMode()
    {
        active = null;
        EndDrag();
    }

    private void EndDrag()
    {
        dragging = false;
        hasLast = false;
        tooLarge = false;
        overlay.Clear();
    }

    // ---------- Подсветка и применение ----------

    private void Redraw(WorldMap map, Vector2Int min, Vector2Int max, bool remove)
    {
        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;

        targetCount = 0;
        tooLarge = width * height > maxAreaCells;

        overlay.Begin();
        if (!tooLarge)
        {
            Color areaColor = remove ? removeColor : active.color;

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    var p = new Vector2Int(x, y);
                    overlay.Add(p, areaColor, true);

                    if (IsTarget(GetNode(map, p), remove))
                    {
                        targetCount++;
                        overlay.Add(p, targetColor, false);
                    }
                }
            }
        }
        overlay.End();
    }

    private void Apply(WorldMap map, Vector2Int min, Vector2Int max, bool remove)
    {
        if (tooLarge)
        {
            Debug.LogWarning($"AreaSelector: область слишком большая (максимум {maxAreaCells} клеток).");
            return;
        }

        JobBoard board = JobBoard.Instance;
        if (board == null)
        {
            Debug.LogWarning("AreaSelector: в сцене нет JobBoard.");
            return;
        }

        int changed = 0;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                ResourceNode node = GetNode(map, new Vector2Int(x, y));
                if (!IsTarget(node, remove)) continue;

                if (remove) board.Cancel(node);
                else board.Designate(node);
                changed++;
            }
        }

        Debug.Log($"{active.label}: {(remove ? "снято меток" : "помечено ресурсов")}: {changed}");
    }

    // Подходит ли ресурс текущему инструменту. При снятии учитываются только уже помеченные.
    private bool IsTarget(ResourceNode node, bool remove)
    {
        if (node == null || node.Data == null || node.Data.skill != active.skill) return false;
        return !remove || node.IsDesignated;
    }

    private static ResourceNode GetNode(WorldMap map, Vector2Int pos)
    {
        Cell cell = map.GetCell(pos);
        if (cell == null || cell.Occupant == null) return null;
        return cell.Occupant.GetComponent<ResourceNode>();
    }

    private static Vector2Int ClampToMap(WorldMap map, Vector2Int p)
    {
        int minX = map.Origin.x;
        int minY = map.Origin.y;
        int maxX = minX + map.Size - 1;
        int maxY = minY + map.Size - 1;
        return new Vector2Int(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY));
    }

    // ---------- Подсказка на экране ----------

    private void OnGUI()
    {
        if (active == null) return;

        string text = $"Режим: {active.label}. Выделяй область ЛКМ, Ctrl снимает метки, Esc выход.";
        if (dragging)
        {
            text += tooLarge
                ? "\nОбласть слишком большая."
                : $"\nРесурсов в области: {targetCount}";
        }

        GUI.Label(new Rect(12f, Screen.height - 64f, 800f, 56f), text);
    }
}