using UnityEngine;

/// <summary>実行時生成 UI で使う、日本語表示可能な OS フォントを一度だけ取得します。</summary>
public static class JapaneseUIFont
{
    private static Font runtimeFont;

    // 取得済みフォントを再利用し、初回だけ候補の OS フォントから日本語用フォントを作ります。
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
