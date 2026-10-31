using UnityEngine;

[DisallowMultipleComponent]
public class GetCord : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Трансформ игрока. Если пусто — берётся трансформ объекта, на котором висит скрипт.")]
    [SerializeField] private Transform player;

    [Tooltip("Grid (Isometric Z as Y). Если пусто — ищется в родителях/на сцене.")]
    [SerializeField] private Grid grid;

    private Vector3Int _lastCell;
    private int _lastZ;
    private bool _isFirstLog = true;

    private void Awake()
    {
        if (player == null) player = transform;

        if (grid == null)
        {
            grid = GetComponentInParent<Grid>();
            if (grid == null) grid = FindFirstObjectByType<Grid>();
        }

        if (grid == null)
            Debug.LogError("[GetCord] Не найден Grid. Назначь его в инспекторе.");
    }

    private void Update()
    {
        if (grid == null || player == null) return;

        Vector3 world = player.position;

        Vector3Int cell = grid.WorldToCell(world);

        Vector3 cellWorld = grid.CellToWorld(cell);
        Vector3 zStep = grid.CellToWorld(new Vector3Int(0, 0, 1)) - grid.CellToWorld(Vector3Int.zero);

        float zExact = Vector3.Dot(world - cellWorld, zStep) / zStep.sqrMagnitude;
        int z = Mathf.RoundToInt(zExact);

        if (_isFirstLog || cell.x != _lastCell.x || cell.y != _lastCell.y || z != _lastZ)
        {
            LogCoordinates(world, cell, z);

            _lastCell = cell;
            _lastZ = z;
            _isFirstLog = false;
        }
    }

    private void LogCoordinates(Vector3 world, Vector3Int cell, int z)
    {
        Debug.Log(
            $"[Player] World: ({world.x:F2}, {world.y:F2}, {world.z:F2}) | " +
            $"Cell XYZ: ({cell.x}, {cell.y}, {z}) | " +
            $"Изменение позиции!"
        );
    }
}