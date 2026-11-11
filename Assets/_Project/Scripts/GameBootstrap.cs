using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private WorldMap worldMap;
    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private BuildingManager buildingManager;

    private void Start()
    {
        worldGenerator.OnWorldGenerated += OnWorldReady;

        var data = SaveManager.Current;

        Debug.Log($"Запуск мира '{data.worldName}', сид = {data.seed}");
        worldGenerator.Generate(data.seed);
    }

    private void OnWorldReady()
    {
        buildingManager.RebuildAll(worldMap);

        worldGenerator.OnWorldGenerated -= OnWorldReady;
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveManager.Save(); }
    private void OnApplicationQuit() { SaveManager.Save(); }
}