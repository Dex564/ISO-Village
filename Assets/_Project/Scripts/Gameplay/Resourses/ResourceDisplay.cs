using System.Text;
using UnityEngine;
using TMPro;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private readonly StringBuilder _sb = new StringBuilder(64);

    private void OnEnable()
    {
        ResourceService.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ResourceService.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (label == null) return;

        _sb.Clear();

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _sb.Append(type)
               .Append(": ")
               .Append(ResourceService.Get(type))
               .Append('\n');
        }

        label.text = _sb.ToString();
    }
}