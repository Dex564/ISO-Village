using UnityEngine;

/// <summary>
/// Приказы поселенцу мышью. По умолчанию ПКМ (ЛКМ занята строительством):
/// на камень значит добывать, на свободную клетку значит идти.
/// </summary>
public class SettlerController : MonoBehaviour
{
    [SerializeField] private Settler settler;
    [SerializeField] private Camera mainCamera;
    [Tooltip("0 = ЛКМ, 1 = ПКМ")]
    [SerializeField] private int orderMouseButton = 1;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (settler == null || !Input.GetMouseButtonDown(orderMouseButton)) return;

        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int pos = map.WorldToCell(mouseWorld);
        Cell cell = map.GetCell(pos);
        if (cell == null) return; // клик за краем карты

        if (cell.Occupant != null)
        {
            ResourceNode node = cell.Occupant.GetComponent<ResourceNode>();
            if (node != null) settler.Mine(node);
            else Debug.Log($"Клетка {pos} занята: {cell.Occupant.name}.");
            return;
        }

        if (cell.Walkable) settler.MoveTo(pos);
    }
}