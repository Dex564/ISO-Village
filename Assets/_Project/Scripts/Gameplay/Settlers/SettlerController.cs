using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Приказы выбранному поселенцу мышью. По умолчанию ПКМ (ЛКМ занята выбором и строительством):
/// на ресурс значит добывать, на свободную клетку значит идти.
/// Кого слушаться, определяет SelectionManager.
/// </summary>
public class SettlerController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [Tooltip("0 = ЛКМ, 1 = ПКМ")]
    [SerializeField] private int orderMouseButton = 1;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(orderMouseButton)) return;

        Settler settler = SelectionManager.Instance != null ? SelectionManager.Instance.Selected : null;
        if (settler == null) return; // никто не выбран

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

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