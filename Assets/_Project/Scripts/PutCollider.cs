using UnityEngine;
using UnityEngine.Tilemaps;

public class PutCollider : MonoBehaviour
{
    public Tilemap targetTilemap;
    public TileBase tileToPlace;
    public Transform player;
    public int offsetX = 5;
    public int offsetY = -8;
    public int offsetZ = 0;

    private void Awake()
    {
        PlaceTile(new Vector3Int(0, 0, 1));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            PlayerPlace();
    }

    public void PlaceTile(Vector3Int cell)
    {
        targetTilemap.SetTile(cell, tileToPlace);

        Debug.Log($"Created collision on {cell}");
    }

    public void PlayerPlace()
    {
        Vector3Int cell = targetTilemap.WorldToCell(player.position);
        cell += new Vector3Int(offsetX, offsetY, offsetZ);
        PlaceTile(cell);
    }
}