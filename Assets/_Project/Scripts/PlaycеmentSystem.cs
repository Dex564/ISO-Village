using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    [SerializeField] private InputActionReference placeAction;

    private void OnEnable()
    {
        if (placeAction != null && placeAction.action != null)
        {
            placeAction.action.performed += OnPlacePerformed;
            placeAction.action.Enable();
        }
        else
        {
            Debug.LogError("[TilePlacer] Input Action Reference не назначен!");
        }
    }

    private void OnDisable()
    {
        if (placeAction != null && placeAction.action != null)
        {
            placeAction.action.performed -= OnPlacePerformed;
            placeAction.action.Disable();
        }
    }

    private void OnPlacePerformed(InputAction.CallbackContext context)
    {
        StartPlacing();
    }

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
    }

    public Vector3Int GetMouseCellPosition()
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
            PolygonCollider2D houseCollider = child.GetComponent<PolygonCollider2D>();

            if (houseCollider == null || child == ghostObject.transform)
                continue;

            if ((blockingLayers.value & (1 << child.gameObject.layer)) == 0)
                continue;

            float distance = Vector2.Distance(position, child.position);

            if (distance < minimumBuildingDistance)
                return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(blockingLayers);
        filter.useTriggers = true;

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
            StartPlacing();

        if (!isPlacing)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            StopPlacing();
            return;
        }

        Vector3Int cellPos = GetMouseCellPosition();
        Vector3 placementPosition = GetPlacementPosition(cellPos);

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