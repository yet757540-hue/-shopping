using System;
using UnityEngine;

// 画面の角に沿って流れるフィルム 1 本分の設定です。
// 数値はタイトル画面の基準解像度 1920x1080 を基準にしています。
[Serializable]
public sealed class FilmStripStyle
{
    [Tooltip("このフィルムを表示するかどうか。")]
    public bool enabled = true;

    [Tooltip("フィルムの角度（度）。正の値で右肩上がりになります。")]
    public float angleDegrees = 10.6f;

    [Tooltip("フィルム内側の縁が通る点（基準解像度・左下原点）。")]
    public Vector2 innerEdgePoint = new Vector2(0f, 845f);

    [Tooltip("内側の縁の点から、フィルム中央までの距離。")]
    public float edgeOffset = 550f;

    [Tooltip("フィルムの長さ（基準解像度ピクセル）。長くすると両端が画面外へ出ます。")]
    public float length = 1834f;

    [Tooltip("フィルムの太さ（基準解像度ピクセル）。")]
    public float thickness = 250f;

    [Tooltip("+1 で内側の縁より上へ、-1 で下へ伸びます。")]
    public float outwardSign = 1f;

    [Tooltip("送り穴の大きさ（基準解像度ピクセル）。")]
    public float holeSize = 30f;

    [Tooltip("送り穴の間隔（基準解像度ピクセル）。")]
    public float holePitch = 75f;

    [Tooltip("内側の縁から送り穴の中心までの距離。")]
    public float holeInset = 30f;

    public Color bandColor = new Color32(36, 20, 7, 255);
    public Color holeColor = Color.white;

    [Tooltip("流れる速さ（基準解像度ピクセル／秒）。")]
    public float scrollSpeed = 340f;

    [Tooltip("+1 で終端方向へ、-1 で逆方向へ流れます。")]
    public float scrollDirection = 1f;

    [Range(0f, 1f)]
    [Tooltip("流れる穴の開始位置（タイル単位）。")]
    public float startPhase;

    public Vector2 Direction
    {
        get
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }

    public Vector2 PivotPosition => innerEdgePoint + Direction * edgeOffset;

    public bool BodyIsAboveEdge => outwardSign >= 0f;

    public FilmStripStyle Clone()
    {
        return (FilmStripStyle)MemberwiseClone();
    }
}

// タイトルロゴやキャラクターなど、基準座標で位置を決める 1 枚絵の設定です。
[Serializable]
public sealed class MenuLayerStyle
{
    public bool enabled = true;

    [Tooltip("1920x1080 の基準座標での、左下の位置。")]
    public Vector2 offset;

    [Tooltip("絵の大きさ（基準解像度ピクセル）。")]
    public Vector2 size = new Vector2(100f, 100f);

    public float rotation;
    public Color tint = Color.white;

    [Header("ステッカー風の縁取り")]
    [Tooltip("フィルムの上でも見えるように、薄い縁取りを後ろへ重ねます。")]
    public bool outlineEnabled;

    public float outlineWidth = 8f;
    public Color outlineColor = Color.white;

    [Range(6, 32)]
    public int outlineSteps = 16;
}

// 画面右から飛び込んでくる演出の設定です。
[Serializable]
public sealed class RushInStyle
{
    public bool enabled = true;

    [Tooltip("開始位置を画面右の外まで自動的に広げます。")]
    public bool startOutsideCanvas = true;

    [Tooltip("右から入ってくるときの横移動量。")]
    public float startOffsetX = 900f;

    [Tooltip("開始時の横の伸び。1 より大きいほど速く見えます。")]
    public float startScaleX = 1.18f;

    [Tooltip("開始時の縦の縮み。1 より小さいほど速く見えます。")]
    public float startScaleY = 0.92f;

    public float delay;
    public float duration = 0.55f;

    [Tooltip("行き過ぎてから戻る量。0 で跳ねず、0.35 で大きく行き過ぎます。")]
    public float overshoot = 0.32f;

    public bool fadeIn = true;
    public float fadePortion = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("登場前の透明度。")]
    public float startAlpha;

    public RushInStyle Clone()
    {
        return (RushInStyle)MemberwiseClone();
    }
}

// タイトル画面のカート型ボタン（手描き風）の設定です。
[Serializable]
public sealed class CartButtonStyle
{
    [Header("配置")]
    public Vector2 size = new Vector2(440f, 132f);

    [Tooltip("先頭カートの中心（基準解像度ピクセル）。")]
    public Vector2 firstCenter = new Vector2(350f, 376f);

    public float spacing = 145f;
    public int labelFontSize = 52;

    [Tooltip("カート中心からのラベル位置（基準解像度ピクセル）。")]
    public Vector2 labelOffset = new Vector2(-86f, 26f);

    [Header("絵柄")]
    [Tooltip("カートを描くテクスチャ解像度。高いほどなめらかで、生成は重くなります。")]
    public float textureWidth = 1024f;

    [Tooltip("支柱の線の太さ（基準解像度ピクセル）。")]
    public float strokeWidth = 15f;

    [Tooltip("荷台の線の太さ（基準解像度ピクセル）。")]
    public float deckThickness = 14f;

    [Tooltip("取っ手の線の太さ（基準解像度ピクセル）。")]
    public float hookThickness = 13f;

    [Tooltip("速度線の太さ（基準解像度ピクセル）。")]
    public float dashThickness = 6.5f;

    [Tooltip("荷台の塗り。α を 0 にすると下書きのように抜けます。")]
    public Color bodyColor = new Color(1f, 1f, 1f, 0f);

    public Color lineColor = new Color(0.05f, 0.04f, 0.03f, 1f);

    [Tooltip("荷台の線。カート枠の左下を原点とした比率で指定します。")]
    public Vector2 deckStart = new Vector2(0.005f, 0.345f);
    public Vector2 deckEnd = new Vector2(0.790f, 0.345f);

    public Vector2 leftPostBottom = new Vector2(0.041f, 0.345f);
    public Vector2 leftPostTop = new Vector2(0.108f, 0.968f);

    public Vector2 rightPostBottom = new Vector2(0.772f, 0.345f);
    public Vector2 rightPostTop = new Vector2(0.824f, 0.800f);

    public Vector2 hookEnd = new Vector2(0.995f, 0.762f);

    public Vector2[] wheelCenters =
    {
        new Vector2(0.145f, 0.196f),
        new Vector2(0.635f, 0.168f)
    };

    public Vector2 wheelRadius = new Vector2(0.069f, 0.099f);
    public int wheelDashCount = 3;
    public float wheelDashLength = 0.058f;
    public float wheelDashSpacing = 0.078f;
    public float wheelDashGap = 0.030f;

    public float trailStartX = 0.895f;
    public float trailStartY = 0.460f;
    public float trailEndY = 0.660f;
    public float trailLengthMin = 0.062f;
    public float trailLengthMax = 0.040f;
    public int trailCount = 3;

    [Header("選択表示")]
    [Tooltip("未選択のカートの線の色。")]
    public Color normalLineColor = new Color(0.05f, 0.04f, 0.03f, 1f);

    [Tooltip("選択中のカートの線の色。")]
    public Color selectedLineColor = new Color(0.925f, 0.290f, 0.055f, 1f);

    [Tooltip("押している間のカートの線の色。")]
    public Color pressedLineColor = new Color(0.720f, 0.130f, 0.030f, 1f);

    public float selectedOffsetX = 22f;
    public float selectedScale = 1.06f;
    public float pressedOffsetX = 10f;
    public float pressedScale = 1.02f;

    [Header("待機・選択アニメーション")]
    public bool idleMotion = true;
    [Min(0f)] public float idleBob = 2.5f;
    [Min(0f)] public float idleTilt = 0.65f;
    [Min(0.1f)] public float idlePeriod = 2.2f;
    [Min(0f)] public float selectedPulse = 0.015f;
    [Min(0.1f)] public float responseSpeed = 14f;
}

// タイトル画面の見た目をまとめた設定です。既定値はアートボードの指示に合わせています。
[Serializable]
public sealed class StartMenuTheme
{
    public Color backgroundColor = Color.white;

    [Header("フィルム")]
    public FilmStripStyle topFilmStrip = new FilmStripStyle
    {
        angleDegrees = 10.6f,
        innerEdgePoint = new Vector2(0f, 845f),
        edgeOffset = 470f,
        length = 1700f,
        thickness = 250f,
        outwardSign = 1f,
        holeSize = 30f,
        holePitch = 75f,
        holeInset = 30f,
        scrollSpeed = 340f,
        scrollDirection = 1f,
        startPhase = 0f
    };

    public FilmStripStyle bottomFilmStrip = new FilmStripStyle
    {
        angleDegrees = 16f,
        innerEdgePoint = new Vector2(1917f, 237f),
        edgeOffset = -200f,
        length = 1405f,
        thickness = 250f,
        outwardSign = -1f,
        holeSize = 30f,
        holePitch = 75f,
        holeInset = 30f,
        scrollSpeed = 340f,
        scrollDirection = 1f,
        startPhase = 0.5f
    };

    [Header("素材レイヤー")]
    public MenuLayerStyle characterLayer = new MenuLayerStyle
    {
        enabled = true,
        offset = new Vector2(747f, 0f),
        size = new Vector2(1070f, 1032f),
        outlineEnabled = false
    };

    public MenuLayerStyle titleLogoLayer = new MenuLayerStyle
    {
        enabled = true,
        offset = new Vector2(99f, 444f),
        size = new Vector2(1002f, 573f),
        outlineEnabled = false,
        outlineWidth = 9f,
        outlineColor = Color.white
    };

    [Header("カートボタン")]
    public CartButtonStyle cartButtons = new CartButtonStyle();

    [Tooltip("開始・設定・終了のラベル（メニュー順）。")]
    public string[] cartLabels = { "開始", "設定", "終了" };

    [Header("登場演出")]
    public RushInStyle titleRush = new RushInStyle
    {
        startOffsetX = 1960f,
        startScaleX = 1.22f,
        startScaleY = 0.88f,
        delay = 0f,
        duration = 0.62f,
        overshoot = 0.24f,
        fadeIn = false
    };

    public RushInStyle characterRush = new RushInStyle
    {
        startOffsetX = 520f,
        startScaleX = 1.06f,
        startScaleY = 0.96f,
        delay = 0.06f,
        duration = 0.55f,
        overshoot = 0.18f
    };

    public RushInStyle cartRush = new RushInStyle
    {
        startOffsetX = 1500f,
        startScaleX = 1.3f,
        startScaleY = 0.86f,
        delay = 0.18f,
        duration = 0.48f,
        overshoot = 0.4f
    };

    [Tooltip("2 台目以降に足す遅延時間。")]
    public float cartStagger = 0.12f;

    public string GetCartLabel(int index)
    {
        if (cartLabels == null || index < 0 || index >= cartLabels.Length)
        {
            return string.Empty;
        }

        return cartLabels[index];
    }

    public FilmStripStyle CloneTopFilmStrip() => topFilmStrip == null ? null : topFilmStrip.Clone();

    public FilmStripStyle CloneBottomFilmStrip() => bottomFilmStrip == null ? null : bottomFilmStrip.Clone();

    public static StartMenuTheme CreateDefault()
    {
        return new StartMenuTheme();
    }
}
