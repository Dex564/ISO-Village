using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldItemUI : MonoBehaviour
{
    [Header("Data")]
    public string worldId;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text seedText;
    public Button playButton;
    public Button deleteButton;

    public void Setup(WorldMeta meta)
    {
        worldId = meta.id;
        nameText.text = meta.worldName;
        seedText.text = $"Сид: {meta.seed}";
    }
}