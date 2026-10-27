using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class PutCollider : MonoBehaviour
{
    public Tilemap targetTilemap;
    public TileBase tileToPlace;
    public Transform player;
    public int offsetX = 5;
    public int offsetY = -8;
    public int offsetZ = 0;
    private InputAction placeAction;


    private void Awake()
    {
        PlaceTile(new Vector3Int(0, 0, 1));
        placeAction = new InputAction("PlaceTile", InputActionType.Button);
        placeAction.AddBinding("<Keyboard>/z");
        placeAction.performed += ctx => PlayerPlace();
    }

    private void OnEnable() => placeAction.Enable();
    private void OnDisable() => placeAction.Disable();

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
