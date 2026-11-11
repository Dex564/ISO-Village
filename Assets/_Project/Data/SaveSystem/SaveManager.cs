using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFolder = "Saves";
    private const string IndexFile  = "worlds_index.json";

    public static SaveData Current { get; private set; } = new SaveData();
    public static string CurrentWorldId { get; private set; } = null;

    public static event Action OnLoaded;

    private static string SavesDir => Path.Combine(Application.persistentDataPath, SaveFolder);
    private static string IndexPath => Path.Combine(SavesDir, IndexFile);
    private static string WorldPath(string id) => Path.Combine(SavesDir, $"world_{id}.json");

    public static List<WorldMeta> GetWorlds()
    {
        if (!Directory.Exists(SavesDir)) return new List<WorldMeta>();
        if (!File.Exists(IndexPath)) return new List<WorldMeta>();

        try
        {
            string json = File.ReadAllText(IndexPath);
            var wrapper = JsonUtility.FromJson<WorldMetaList>(json);
            return wrapper?.worlds ?? new List<WorldMeta>();
        }
        catch { return new List<WorldMeta>(); }
    }

    private static void SaveIndex(List<WorldMeta> worlds)
    {
        Directory.CreateDirectory(SavesDir);
        var wrapper = new WorldMetaList { worlds = worlds };
        File.WriteAllText(IndexPath, JsonUtility.ToJson(wrapper, true));
    }

    [Serializable] private class WorldMetaList { public List<WorldMeta> worlds = new(); }

    public static string CreateNewWorld(string worldName, int seed)
    {
        Directory.CreateDirectory(SavesDir);

        string id = Guid.NewGuid().ToString("N").Substring(0, 8);
        var data = new SaveData
        {
            worldName = worldName,
            seed = seed,
            buildings = new(),
        };

        File.WriteAllText(WorldPath(id), JsonUtility.ToJson(data, true));

        var worlds = GetWorlds();
        worlds.Add(new WorldMeta
        {
            id = id,
            worldName = worldName,
            seed = seed,
            lastPlayed = DateTime.UtcNow.ToString("o")
        });
        SaveIndex(worlds);

        CurrentWorldId = id;
        Current = data;
        return id;
    }

    public static bool LoadWorld(string id)
    {
        string path = WorldPath(id);
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            Current = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            CurrentWorldId = id;

            var worlds = GetWorlds();
            var meta = worlds.Find(w => w.id == id);
            if (meta != null) { meta.lastPlayed = DateTime.UtcNow.ToString("o"); SaveIndex(worlds); }

            OnLoaded?.Invoke();
            Debug.Log($"[Save] Мир загружен: {id}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Ошибка загрузки: {e}");
            return false;
        }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(CurrentWorldId)) return;
        try
        {
            Directory.CreateDirectory(SavesDir);
            File.WriteAllText(WorldPath(CurrentWorldId), JsonUtility.ToJson(Current, true));
            Debug.Log("[Save] Сохранено");
        }
        catch (Exception e) { Debug.LogError($"[Save] Ошибка: {e}"); }
    }

    public static void DeleteWorld(string id)
    {
        if (File.Exists(WorldPath(id))) File.Delete(WorldPath(id));
        var worlds = GetWorlds();
        worlds.RemoveAll(w => w.id == id);
        SaveIndex(worlds);
    }
}