using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string FileName = "save.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData Current { get; private set; } = new SaveData();

    public static event Action OnLoaded;

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Current, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Сохранено: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Ошибка сохранения: {e}");
        }
    }

    public static SaveData Load()
    {
        if (!HasSave())
        {
            Current = new SaveData();
            return Current;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            Current = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            OnLoaded?.Invoke();
            Debug.Log("[Save] Сохранение загружено");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Ошибка загрузки: {e}");
            Current = new SaveData();
        }
        return Current;
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
        Current = new SaveData();
    }
}