using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("Buildings")]
    [SerializeField] private BuildingData[] availableBuildings;
    [SerializeField] private int startIndex = 0;

    [Header("Placement Settings")]
    [SerializeField] private Grid grid;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField, Min(0f)] private float minimumBuildingDistance = 2f;

    [Header("Input")]
    [SerializeField] private KeyCode cycleNextKey = KeyCode.E;
    [SerializeField] private KeyCode cyclePrevKey = KeyCode.Q;
    [SerializeField] private KeyCode togglePlacementKey = KeyCode.B;
    [SerializeField] private KeyCode rotateKey = KeyCode.R;

    // runtime
    private bool isPlacing;
    private int currentIndex;
    private BuildingData CurrentBuilding =>
        (availableBuildings != null && availableBuildings.Length > 0)
            ? availableBuildings[Mathf.Clamp(currentIndex, 0, availableBuildings.Length - 1)]
            : null;

    private GameObject ghostInstance;
    private PolygonCollider2D placementCollider;
    private ContactFilter2D filter;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    private Quaternion currentRotation = Quaternion.identity;

    private readonly List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>();
    private readonly List<Color> ghostBaseColors = new List<Color>();
    private bool? lastValidState;

    private void Awake()
    {
        if (availableBuildings == null || availableBuildings.Length == 0)
        {
            Debug.LogWarning("PlacementSystem: не задан список зданий.");
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, availableBuildings.Length - 1);

        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(blockingLayers);

        RebuildGhost();
    }

    // ---------- Ghost management ----------

    private void RebuildGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
        placementCollider = null;

        BuildingData data = CurrentBuilding;
        if (data == null || data.buildingPrefab == null)
        {
            Debug.LogWarning("PlacementSystem: у здания не задан buildingPrefab.");
            return;
        }

        GameObject temp = Instantiate(data.buildingPrefab);
        temp.name = $"{data.buildingName}_Ghost";
        temp.SetActive(false);

        StripForGhost(temp);

        ApplyGhostVisuals(temp);

        ghostRenderers.Clear();
        ghostBaseColors.Clear();
        foreach (var sr in temp.GetComponentsInChildren<SpriteRenderer>(true))
        {
            ghostRenderers.Add(sr);
            ghostBaseColors.Add(sr.color);
        }
        lastValidState = null;

        ghostInstance = temp;
        ghostInstance.transform.SetParent(grid.transform, worldPositionStays: false);
        ghostInstance.transform.localPosition = Vector3.zero;
        ghostInstance.transform.localRotation = Quaternion.identity;
        ghostInstance.transform.localScale = Vector3.one;

        placementCollider = ghostInstance.GetComponent<PolygonCollider2D>();
        if (placementCollider == null)
            placementCollider = ghostInstance.AddComponent<PolygonCollider2D>();

        PolygonCollider2D source = data.buildingPrefab.GetComponent<PolygonCollider2D>();
        if (source != null)
        {
            placementCollider.pathCount = source.pathCount;
            placementCollider.offset = source.offset;
            for (int i = 0; i < source.pathCount; i++)
                placementCollider.SetPath(i, source.GetPath(i));
        }

        placementCollider.isTrigger = true;
        placementCollider.enabled = true;

        ghostInstance.transform.rotation = currentRotation;
    }

    private static void StripForGhost(GameObject root)
    {
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) Destroy(mb);
        }
        foreach (var rb in root.GetComponentsInChildren<Rigidbody2D>(true))
            Destroy(rb);
        foreach (var col in root.GetComponentsInChildren<Collider2D>(true))
        {
            if (col is PolygonCollider2D) continue;
            Destroy(col);
        }
    }

    private static void ApplyGhostVisuals(GameObject root)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = 0.6f;
            sr.color = c;
        }
    }

    private void SetGhostValid(bool valid)
    {
        if (lastValidState == valid) return;
        lastValidState = valid;

        Color tint = valid
            ? Color.white
            : new Color(1f, 0.3f, 0.3f, 1f);

        for (int i = 0; i < ghostRenderers.Count; i++)
        {
            var sr = ghostRenderers[i];
            if (sr == null) continue;

            Color baseColor = ghostBaseColors[i];
            sr.color = new Color(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a
            );
        }
    }

    private void CycleBuilding(int delta)
    {
        if (availableBuildings == null || availableBuildings.Length == 0) return;

        currentIndex = (currentIndex + delta) % availableBuildings.Length;
        if (currentIndex < 0) currentIndex += availableBuildings.Length;

        RebuildGhost();

        if (isPlacing)
        {
            ghostInstance.SetActive(true);
            ghostInstance.transform.position = GetPlacementPosition(GetMouseCellPosition());
            ghostInstance.transform.rotation = currentRotation;
        }
    }

    // ---------- Helpers ----------

    private Vector3Int GetMouseCellPosition()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        return grid.WorldToCell(mouseWorldPos);
    }

    private Vector3 GetPlacementPosition(Vector3Int cellPos)
    {
        Vector3 position = grid.GetCellCenterWorld(cellPos);
        position.z = 0f;
        return position;
    }

    private bool CanPlace(Vector3 position, Quaternion rotation)
    {
        if (ghostInstance == null || placementCollider == null) return false;

        ghostInstance.transform.position = position;
        ghostInstance.transform.rotation = rotation;
        Physics2D.SyncTransforms();

        foreach (Transform child in grid.transform)
        {
            if (child == ghostInstance.transform || child.IsChildOf(ghostInstance.transform))
                continue;
            if (child.name.EndsWith("_Ghost")) continue;

            if ((blockingLayers.value & (1 << child.gameObject.layer)) == 0)
                continue;

            Vector2 a = position;
            Vector2 b = child.position;
            if (Vector2.Distance(a, b) < minimumBuildingDistance)
                return false;
        }

        overlaps.Clear();
        placementCollider.Overlap(filter, overlaps);

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null) continue;
            if (overlap.transform == ghostInstance.transform) continue;
            if (overlap.transform.IsChildOf(ghostInstance.transform)) continue;
            if (overlap.transform.name.EndsWith("_Ghost")) continue;

            return false;
        }

        return true;
    }

    // ---------- Public API ----------

    public void StartPlacing()
    {
        if (grid == null || mainCamera == null || CurrentBuilding == null)
        {
            Debug.LogWarning("PlacementSystem: проверь ссылки в Inspector.");
            return;
        }

        if (ghostInstance == null || placementCollider == null)
        {
            RebuildGhost();
            if (ghostInstance == null || placementCollider == null) return;
        }

        currentRotation = Quaternion.identity;
        ghostInstance.transform.rotation = currentRotation;
        ghostInstance.transform.position = GetPlacementPosition(GetMouseCellPosition());
        isPlacing = true;
        ghostInstance.SetActive(true);
    }

    public void StopPlacing()
    {
        isPlacing = false;
        if (ghostInstance != null)
            ghostInstance.SetActive(false);
    }

    // ---------- Update ----------

    private void Update()
    {
        if (Input.GetKeyDown(togglePlacementKey))
        {
            if (!isPlacing) StartPlacing();
            else StopPlacing();
        }

        if (Input.GetKeyDown(cycleNextKey)) CycleBuilding(+1);
        if (Input.GetKeyDown(cyclePrevKey)) CycleBuilding(-1);

        if (!isPlacing || ghostInstance == null) return;

        if (Input.GetKeyDown(rotateKey))
        {
            currentRotation = Quaternion.Euler(0f, 180f, 0f) * currentRotation;
            ghostInstance.transform.rotation = currentRotation;
            Physics2D.SyncTransforms();
        }

        Vector3 placementPosition = GetPlacementPosition(GetMouseCellPosition());
        ghostInstance.transform.position = placementPosition;

        bool canPlaceHere = CanPlace(placementPosition, currentRotation);
        SetGhostValid(canPlaceHere);

        if (Input.GetMouseButtonDown(0))
        {
            if (!canPlaceHere)
            {
                Debug.Log("Здесь нельзя строить: место занято.");
                return;
            }

            Instantiate(CurrentBuilding.buildingPrefab, placementPosition, currentRotation, grid.transform);
            Physics2D.SyncTransforms();
        }
    }
}