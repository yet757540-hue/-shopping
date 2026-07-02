using UnityEngine;

// 実行時生成 UI で日本語が豆腐にならないよう、OS の日本語フォントを取得する共通ヘルパーです。
// 役割:
// - UnityEngine.UI.Text 用の Font を一度だけ生成してキャッシュします。
// 接続:
// - ControlsGuideUI、InventoryStatusUI、ScoreboardManager、TimerDisplayUI、StartMenuManager が利用します。
// 読むときの要点:
// - 指定フォント名の中から OS に存在するものが使われます。プロジェクトにフォントアセットを置かない簡易方式です。
public static class JapaneseUIFont
{
    private static Font runtimeFont;

    // OS に存在する日本語フォント候補から Text 用 Font を一度だけ作って返します。
    public static Font Get(int fontSize)
    {
        if (runtimeFont != null)
        {
            return runtimeFont;
        }

        runtimeFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Yu Gothic UI",
                "Yu Gothic",
                "Meiryo",
                "MS Gothic",
                "Noto Sans CJK JP",
                "Noto Sans JP"
            },
            Mathf.Max(8, fontSize)
        );

        return runtimeFont;
    }
}
