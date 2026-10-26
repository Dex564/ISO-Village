using UnityEngine;
using UnityEngine.Tilemaps;

public class GetPosition : MonoBehaviour
{
    [SerializeField] private Tilemap map;
    [SerializeField] private Transform player;

    void FixedUpdate()
    {
        Vector3Int cell = map.WorldToCell(player.position);
        Debug.Log($"Я на клетке {cell}", map);
    }
}
