using System;

public static class WorldManager
{
    public static int Seed
    {
        get => SaveManager.Current.seed;
        set { SaveManager.Current.seed = value; OnChanged?.Invoke(); }
    }
    public static event Action OnChanged;
}