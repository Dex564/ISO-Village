using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private GameObject ghostObject;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Grid grid;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField, Min(0f)] private float minimumBuildingDistance = 2f;

    [Header("Visual Settings")]
    [SerializeField] private Vector3 visualOffset = Vector3.zero;

    private bool isPlacing;
    private PolygonCollider2D placementCollider;
    private ContactFilter2D filter;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    private void Awake()
    {
        if (ghostObject != null)
            ghostObject.SetActive(false);

        if (ghostObject == null || buildingPrefab == null)
            return;

        PolygonCollider2D source = buildingPrefab.GetComponent<PolygonCollider2D>();
        if (source == null)
            return;

        placementCollider = ghostObject.GetComponent<PolygonCollider2D>();
        if (placementCollider == null)
            placementCollider = ghostObject.AddComponent<PolygonCollider2D>();

        placementCollider.pathCount = source.pathCount;
        placementCollider.offset = source.offset;
        placementCollider.isTrigger = true;
        placementCollider.enabled = true;

        for (int i = 0; i < source.pathCount; i++)
            placementCollider.SetPath(i, source.GetPath(i));

        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(blockingLayers);
    }

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
        return position + visualOffset;
    }

    private bool CanPlace(Vector3 position)
    {
        ghostObject.transform.position = position;
        Physics2D.SyncTransforms();

        foreach (Transform child in grid.transform)
        {
            if (child == ghostObject.transform)
                continue;

            if ((blockingLayers.value & (1 << child.gameObject.layer)) == 0)
                continue;

            if (Vector2.Distance(position, child.position) < minimumBuildingDistance)
                return false;

            if (!child.TryGetComponent<PolygonCollider2D>(out _))
                continue;
        }

        overlaps.Clear();
        placementCollider.Overlap(filter, overlaps);

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.transform == ghostObject.transform || overlap.transform.IsChildOf(ghostObject.transform))
                continue;
            return false;
        }

        return true;
    }

    public void StartPlacing()
    {
        if (ghostObject == null || buildingPrefab == null || grid == null || mainCamera == null)
        {
            Debug.LogWarning("PlacementSystem: проверь ссылки в Inspector.");
            return;
        }

        if (placementCollider == null)
        {
            Debug.LogWarning("PlacementSystem: добавь PolygonCollider2D на корень префаба Building.");
            return;
        }

        ghostObject.transform.position = GetPlacementPosition(GetMouseCellPosition());
        isPlacing = true;
        ghostObject.SetActive(true);
    }

    public void StopPlacing()
    {
        isPlacing = false;
        if (ghostObject != null)
            ghostObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!isPlacing) StartPlacing();
            else StopPlacing();
        }

        if (!isPlacing) return;

        Vector3 placementPosition = GetPlacementPosition(GetMouseCellPosition());
        ghostObject.transform.position = placementPosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (!CanPlace(placementPosition))
            {
                Debug.Log("Здесь нельзя строить: место занято.");
                return;
            }

            Instantiate(buildingPrefab, placementPosition, ghostObject.transform.rotation, grid.transform);
            Physics2D.SyncTransforms();
        }
    }
}