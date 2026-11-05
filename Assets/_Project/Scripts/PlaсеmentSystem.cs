using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Строительство: призрак здания следует за мышью, проверка и занятие клеток идёт через WorldMap.
/// Никаких коллайдеров и расстояний: можно ли строить, решают данные клеток.
/// </summary>
public class PlacementSystem : MonoBehaviour
{
    [Header("Buildings")]
    [SerializeField] private BuildingData[] availableBuildings;
    [SerializeField] private int startIndex = 0;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform buildingsParent;
    [SerializeField] private BuildingManager buildingManager;

    [Header("Ghost")]
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.6f;
    [SerializeField] private Color validTint = Color.white;
    [SerializeField] private Color invalidTint = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Input")]
    [SerializeField] private KeyCode togglePlacementKey = KeyCode.B;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    [SerializeField] private KeyCode cycleNextKey = KeyCode.E;
    [SerializeField] private KeyCode cyclePrevKey = KeyCode.Q;
    [SerializeField] private KeyCode flipKey = KeyCode.R;

    // runtime
    private bool isPlacing;
    private bool flipped;
    private int currentIndex;

    private GameObject ghost;
    private readonly List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();
    private readonly List<Color> ghostBaseColors = new List<Color>();
    private bool? lastValid;

    private BuildingData CurrentBuilding => availableBuildings[currentIndex];

    // ---------- Unity ----------

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (availableBuildings == null || availableBuildings.Length == 0)
        {
            Debug.LogWarning("PlacementSystem: не задан список зданий.");
            enabled = false;
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, availableBuildings.Length - 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(cycleNextKey)) CycleBuilding(+1);
        if (Input.GetKeyDown(cyclePrevKey)) CycleBuilding(-1);

        if (Input.GetKeyDown(togglePlacementKey))
        {
            if (isPlacing) StopPlacing();
            else StartPlacing();
        }

        if (!isPlacing) return;

        if (Input.GetKeyDown(cancelKey))
        {
            StopPlacing();
            return;
        }

        if (Input.GetKeyDown(flipKey))
        {
            flipped = !flipped;
            ApplyFlip(ghost);
        }

        WorldMap map = WorldMap.Instance;
        if (ghost == null || map == null) return;

        Vector2Int footprint = GetFootprint();
        Vector2Int origin = GetOrigin(map, footprint);

        ghost.transform.position = GetCenter(map, origin, footprint);

        bool canPlace = map.CanPlace(origin, footprint);
        SetGhostValid(canPlace);

        if (Input.GetMouseButtonDown(0))
        {
            if (canPlace) PlaceBuilding(map, origin, footprint);
            else Debug.Log("Здесь нельзя строить.");
        }
    }

    // ---------- Режим строительства ----------

    public void StartPlacing()
    {
        if (isPlacing) return;
        isPlacing = true;
        flipped = false;
        BuildGhost();
    }

    public void StopPlacing()
    {
        isPlacing = false;
        DestroyGhost();
    }

    private void CycleBuilding(int delta)
    {
        currentIndex = (currentIndex + delta) % availableBuildings.Length;
        if (currentIndex < 0) currentIndex += availableBuildings.Length;

        if (isPlacing) BuildGhost();
    }

    // ---------- Клетки и позиции ----------

    // Размер основания с учётом отражения: в изометрии зеркало меняет оси клеток местами.
    private Vector2Int GetFootprint()
    {
        Vector2Int fp = CurrentBuilding.footprint;
        fp.x = Mathf.Max(1, fp.x);
        fp.y = Mathf.Max(1, fp.y);
        return flipped ? new Vector2Int(fp.y, fp.x) : fp;
    }

    // Клетка под мышью считается центром основания (для 2x2 это его нижний угол).
    private Vector2Int GetOrigin(WorldMap map, Vector2Int footprint)
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int mouseCell = map.WorldToCell(mouseWorld);
        return mouseCell - new Vector2Int((footprint.x - 1) / 2, (footprint.y - 1) / 2);
    }

    // Середина между крайними клетками основания.
    private static Vector3 GetCenter(WorldMap map, Vector2Int origin, Vector2Int footprint)
    {
        Vector3 a = map.CellToWorld(origin);
        Vector3 b = map.CellToWorld(origin + footprint - Vector2Int.one);
        return (a + b) * 0.5f;
    }

    // ---------- Постройка ----------

    private void PlaceBuilding(WorldMap map, Vector2Int origin, Vector2Int footprint)
    {
        BuildingData data = CurrentBuilding;
        Vector3 center = GetCenter(map, origin, footprint);

        GameObject building = Instantiate(data.buildingPrefab, center, Quaternion.identity, buildingsParent);
        building.name = data.buildingName;
        ApplyFlip(building);

        map.Occupy(origin, footprint, building);

        buildingManager.RegisterPlaced(data, origin, footprint, flipped);

        // Здесь позже: проверка и списание data.cost через ResourceStock.
    }

    // ---------- Призрак ----------

    private void BuildGhost()
    {
        DestroyGhost();

        BuildingData data = CurrentBuilding;
        if (data == null || data.buildingPrefab == null)
        {
            Debug.LogWarning("PlacementSystem: у здания не задан buildingPrefab.");
            return;
        }

        ghost = Instantiate(data.buildingPrefab);
        ghost.name = data.buildingName + "_Ghost";

        // Призраку нужна только картинка.
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
        foreach (var rb in ghost.GetComponentsInChildren<Rigidbody2D>(true)) Destroy(rb);
        foreach (var col in ghost.GetComponentsInChildren<Collider2D>(true)) Destroy(col);

        ghostRenderers.Clear();
        ghostBaseColors.Clear();
        foreach (var sr in ghost.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = ghostAlpha;
            sr.color = c;

            ghostRenderers.Add(sr);
            ghostBaseColors.Add(c);
        }

        lastValid = null;
        ApplyFlip(ghost);
    }

    private void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostRenderers.Clear();
        ghostBaseColors.Clear();
        lastValid = null;
    }

    // Отражение делаем через масштаб по X (нужен pivot в центре основания).
    private void ApplyFlip(GameObject go)
    {
        if (go == null) return;

        Vector3 scale = CurrentBuilding.buildingPrefab.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipped ? -1f : 1f);
        go.transform.localScale = scale;
    }

    private void SetGhostValid(bool valid)
    {
        if (lastValid == valid) return;
        lastValid = valid;

        Color tint = valid ? validTint : invalidTint;

        for (int i = 0; i < ghostRenderers.Count; i++)
        {
            if (ghostRenderers[i] == null) continue;

            Color baseColor = ghostBaseColors[i];
            ghostRenderers[i].color = new Color(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a);
        }
    }
}