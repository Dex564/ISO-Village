using System;
using UnityEngine;

public static class ResourceService
{
    public static int Coins
    {
        get => SaveManager.Current.coins;
        set { SaveManager.Current.coins = Mathf.Max(0, value); OnChanged?.Invoke(); }
    }
    public static int Wood
    {
        get => SaveManager.Current.wood;
        set { SaveManager.Current.wood = Mathf.Max(0, value); OnChanged?.Invoke(); }
    }
    public static int Stone
    {
        get => SaveManager.Current.stone;
        set { SaveManager.Current.stone = Mathf.Max(0, value); OnChanged?.Invoke(); }
    }

    public static event Action OnChanged;

    public static void Add(int coins = 0, int wood = 0, int stone = 0)
    {
        SaveManager.Current.coins += coins;
        SaveManager.Current.wood  += wood;
        SaveManager.Current.stone += stone;
        OnChanged?.Invoke();
    }

    public static bool TrySpend(int coins = 0, int wood = 0, int stone = 0)
    {
        var d = SaveManager.Current;
        if (d.coins < coins || d.wood < wood || d.stone < stone) return false;
        d.coins -= coins;
        d.wood  -= wood;
        d.stone -= stone;
        OnChanged?.Invoke();
        return true;
    }
}