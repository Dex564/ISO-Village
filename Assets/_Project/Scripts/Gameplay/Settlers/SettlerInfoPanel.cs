using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель справа с информацией о выбранном поселенце: имя, раса, состояние, навыки и особенности расы.
/// Интерфейс создаётся кодом при запуске, настраивать в редакторе ничего не нужно:
/// достаточно повесить скрипт на любой объект сцены. Шрифт встроенный, кириллица работает сразу.
/// </summary>
public class SettlerInfoPanel : MonoBehaviour
{
    [Header("Вид")]
    [SerializeField] private Vector2 panelSize = new Vector2(340f, 440f);
    [SerializeField] private float margin = 16f;
    [SerializeField] private int fontSize = 20;
    [Tooltip("Пусто = встроенный шрифт Unity")]
    [SerializeField] private Font font;
    [SerializeField] private float refreshInterval = 0.2f;

    private GameObject panelRoot;
    private Text label;
    private Settler shownFor;
    private float nextRefresh;
    private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(512);

    private void Awake()
    {
        BuildUi();
    }

    private void Update()
    {
        Settler selected = SelectionManager.Instance != null ? SelectionManager.Instance.Selected : null;

        bool show = selected != null;
        if (panelRoot.activeSelf != show) panelRoot.SetActive(show);
        if (!show)
        {
            shownFor = null;
            return;
        }

        // Новый выбор: обновляем сразу, а не ждём интервала.
        if (selected != shownFor)
        {
            shownFor = selected;
            nextRefresh = 0f;
        }

        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + refreshInterval;

        label.text = BuildText(selected);
    }

    // ---------- Текст панели ----------

    private string BuildText(Settler s)
    {
        sb.Clear();

        sb.Append("<b><size=").Append(fontSize + 6).Append('>').Append(s.DisplayName).Append("</size></b>\n");
        sb.Append(s.Race != null ? s.Race.raceName : "Без расы").Append('\n');
        sb.Append("<color=#AAAAAA>").Append(s.StatusText).Append("</color>\n\n");

        sb.Append("<b>Навыки</b>\n");
        if (s.Skills.All.Count == 0) sb.Append("<color=#AAAAAA>нет</color>\n");

        foreach (SkillState st in s.Skills.All)
        {
            int max = s.GetMaxLevel(st.skill);

            sb.Append(st.skill.DisplayName()).Append(": ")
              .Append(st.level).Append(" / ").Append(max);

            if (st.level >= max)
            {
                sb.Append("  <color=#FFD54F>макс.</color>");
            }
            else
            {
                sb.Append("  <color=#AAAAAA>(")
                  .Append(Mathf.FloorToInt(st.xp)).Append('/')
                  .Append(Mathf.CeilToInt(SkillProgression.XpToNext(st.level)))
                  .Append(" оп.)</color>");
            }
            sb.Append('\n');
        }

        RaceData race = s.Race;
        if (race != null && race.modifiers != null && race.modifiers.Length > 0)
        {
            sb.Append("\n<b>Особенности расы</b>\n");
            foreach (RaceModifier m in race.modifiers)
            {
                if (!string.IsNullOrEmpty(m.label)) sb.Append("• ").Append(m.label).Append('\n');
            }
        }

        return sb.ToString();
    }

    // ---------- Создание интерфейса ----------

    private void BuildUi()
    {
        var canvasGo = new GameObject("SettlerInfoCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>(); // чтобы панель перехватывала клики мыши

        panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvasGo.transform, false);

        var panelRect = (RectTransform)panelRoot.transform;
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-margin, 0f);
        panelRect.sizeDelta = panelSize;
        panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(panelRoot.transform, false);

        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 14f);
        textRect.offsetMax = new Vector2(-14f, -14f);

        label = textGo.GetComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAnchor.UpperLeft;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;

        if (label.font == null)
            Debug.LogWarning("SettlerInfoPanel: не удалось загрузить шрифт. Назначь шрифт в поле Font.");

        panelRoot.SetActive(false);
    }
}