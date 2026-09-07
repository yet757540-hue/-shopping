using UnityEngine;
using UnityEngine.UI;

/// <summary>Prefab-owned visual for the score objectives.</summary>
[DisallowMultipleComponent]
public sealed class ScoreboardView : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Color incompleteColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private ScoreboardManager scoreboard;

    public Text Text => text;

    public void Initialize(ScoreboardManager source)
    {
        if (scoreboard == source)
        {
            Refresh();
            return;
        }

        if (scoreboard != null)
        {
            scoreboard.StateChanged -= Refresh;
        }

        scoreboard = source;

        if (scoreboard != null)
        {
            scoreboard.StateChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (scoreboard != null)
        {
            scoreboard.StateChanged -= Refresh;
        }
    }

    private void OnEnable()
    {
        if (scoreboard != null)
        {
            scoreboard.StateChanged -= Refresh;
            scoreboard.StateChanged += Refresh;
        }

        Refresh();
    }

    public void SetWarning(bool warning)
    {
        if (text != null)
        {
            text.color = warning ? incompleteColor : normalColor;
        }
    }

    private void Refresh()
    {
        if (text != null)
        {
            text.text = scoreboard != null ? scoreboard.GetDisplayText() : string.Empty;
        }
    }
}
