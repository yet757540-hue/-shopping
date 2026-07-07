using UnityEngine;

public static class JapaneseUIFont
{
    private static Font runtimeFont;

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
