using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private WorldMap worldMap;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private PlacementSystem placementSystem;

    private void Start()
    {
        SaveManager.Load();
        buildingManager.RebuildAll(worldMap);
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveManager.Save(); }
    private void OnApplicationQuit() { SaveManager.Save(); }
}