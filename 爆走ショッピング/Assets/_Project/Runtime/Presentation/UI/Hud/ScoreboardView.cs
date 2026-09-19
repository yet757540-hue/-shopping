using UnityEngine;
using UnityEngine.UI;

/// <summary>スコア目標の表示だけを担当する、Prefab 側のビューです。</summary>
[DisallowMultipleComponent]
public sealed class ScoreboardView : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Color incompleteColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private ScoreboardManager scoreboard;

    // 目標文字と警告色の設定先となる Text を公開します。
    public Text Text => text;

    // 管理クラスを差し替える時は、古いイベント登録を先に解除します。
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

    // 無効化中の更新を避けるため、スコア管理の通知を解除します。
    private void OnDisable()
    {
        if (scoreboard != null)
        {
            scoreboard.StateChanged -= Refresh;
        }
    }

    // スコア管理の通知を登録し、再表示時の文字を同期します。
    private void OnEnable()
    {
        if (scoreboard != null)
        {
            scoreboard.StateChanged -= Refresh;
            scoreboard.StateChanged += Refresh;
        }

        Refresh();
    }

    // 警告状態に応じて、文字表示の色を切り替えます。
    public void SetWarning(bool warning)
    {
        if (text != null)
        {
            text.color = warning ? incompleteColor : normalColor;
        }
    }

    // スコア管理から現在の表示文字列を取得し、Text に反映します。
    private void Refresh()
    {
        if (text != null)
        {
            text.text = scoreboard != null ? scoreboard.GetDisplayText() : string.Empty;
        }
    }
}
