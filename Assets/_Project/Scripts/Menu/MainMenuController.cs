using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject createPanel;

    [Header("World List")]
    public Transform worldListContent;
    public GameObject worldItemPrefab;

    [Header("Create Panel")]
    public TMP_InputField nameInput;
    public TMP_InputField seedInput;
    public Button createButton;
    public Button backButton;

    [Header("Main Panel")]
    public Button openCreateButton;
    public Button quitButton;

    private const string GameSceneName = "World";

    void Start()
    {
        openCreateButton.onClick.AddListener(ShowCreatePanel);
        quitButton.onClick.AddListener(Application.Quit);
        backButton.onClick.AddListener(ShowMainPanel);
        createButton.onClick.AddListener(CreateWorld);

        ShowMainPanel();
        RefreshWorldList();
    }

    void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        createPanel.SetActive(false);
        RefreshWorldList();
    }

    void ShowCreatePanel()
    {
        mainPanel.SetActive(false);
        createPanel.SetActive(true);
        nameInput.text = $"Мир {System.DateTime.Now:HH:mm}";
        seedInput.text = "";
    }

    void RefreshWorldList()
    {
        foreach (Transform child in worldListContent) Destroy(child.gameObject);

        var worlds = SaveManager.GetWorlds();
        worlds.Sort((a, b) => string.Compare(b.lastPlayed, a.lastPlayed)); // новые сверху

        foreach (var w in worlds)
        {
            var go = Instantiate(worldItemPrefab, worldListContent);
            var ui = go.GetComponent<WorldItemUI>();
            ui.worldId = w.id;
            ui.nameText.text = w.worldName;
            ui.seedText.text = $"(сид: {w.seed})";

            ui.playButton.onClick.AddListener(() =>
            {
                if (SaveManager.LoadWorld(w.id))
                    SceneManager.LoadScene(GameSceneName);
            });

            ui.deleteButton.onClick.AddListener(() =>
            {
                SaveManager.DeleteWorld(w.id);
                RefreshWorldList();
            });
        }
    }

    void CreateWorld()
    {
        string worldName = string.IsNullOrWhiteSpace(nameInput.text)
            ? "Новый мир" : nameInput.text;

        int seed;
        if (string.IsNullOrWhiteSpace(seedInput.text))
            seed = Random.Range(int.MinValue, int.MaxValue);
        else if (!int.TryParse(seedInput.text, out seed))
        {
            Debug.LogWarning("Неверный сид — используется случайный");
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        SaveManager.CreateNewWorld(worldName, seed);
        SceneManager.LoadScene(GameSceneName);
    }
}