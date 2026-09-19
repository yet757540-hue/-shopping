using UnityEngine;

// フィルムとカートの絵を実行時に描き起こします。
// 追加のテクスチャ素材を持たずに、Inspector の値だけで見た目を調整できます。
public static class MenuProceduralSprites
{
    public static Texture2D CreateFilmStripTexture(
        int pitch,
        int thickness,
        int holeSize,
        int holeInsetFromBottom,
        Color bandColor,
        Color holeColor
    )
    {
        pitch = Mathf.Max(6, pitch);
        thickness = Mathf.Max(6, thickness);
        holeSize = Mathf.Clamp(holeSize, 2, Mathf.Min(pitch, thickness));
        holeInsetFromBottom = Mathf.Clamp(holeInsetFromBottom, 0, thickness - holeSize);

        Texture2D texture = new Texture2D(pitch, thickness, TextureFormat.RGBA32, false)
        {
            name = "MenuFilmStrip",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0
        };

        Color32[] pixels = new Color32[pitch * thickness];
        Color32 band = bandColor;
        Color32 hole = holeColor;

        float radius = Mathf.Clamp(holeSize * 0.3f, 0f, holeSize * 0.5f);
        float centerX = pitch * 0.5f;
        float centerY = holeInsetFromBottom + holeSize * 0.5f;
        Vector2 half = new Vector2(holeSize * 0.5f - radius, holeSize * 0.5f - radius);

        for (int y = 0; y < thickness; y++)
        {
            for (int x = 0; x < pitch; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - centerX);
                dx = Mathf.Min(dx, pitch - dx);
                Vector2 local = new Vector2(dx, Mathf.Abs(y + 0.5f - centerY)) - half;
                float distance = RoundedBoxDistance(local, radius);
                float coverage = Mathf.Clamp01(0.5f - distance);
                pixels[y * pitch + x] = Lerp(band, hole, coverage);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    public static Sprite CreateCartSprite(CartButtonStyle style)
    {
        return CreateCartSprite(style, style.lineColor, style.bodyColor);
    }

    public static Sprite CreateCartSprite(CartButtonStyle style, Color lineColor, Color fillColor)
    {
        int width = Mathf.Clamp(Mathf.RoundToInt(style.textureWidth), 128, 4096);
        float aspect = Mathf.Max(0.05f, style.size.y / Mathf.Max(1f, style.size.x));
        int height = Mathf.Clamp(Mathf.RoundToInt(width * aspect), 48, 4096);

        Color[] buffer = new Color[width * height];
        float pixelsPerReference = width / Mathf.Max(1f, style.size.x);

        Vector2 Scale(Vector2 normalized)
        {
            return new Vector2(normalized.x * width, normalized.y * height);
        }

        float postStroke = Mathf.Max(2f, style.strokeWidth * pixelsPerReference);
        float deckStroke = Mathf.Max(2f, style.deckThickness * pixelsPerReference);
        float hookStroke = Mathf.Max(2f, style.hookThickness * pixelsPerReference);
        float dashStroke = Mathf.Max(1.5f, style.dashThickness * pixelsPerReference);

        Vector2 deckStart = Scale(style.deckStart);
        Vector2 deckEnd = Scale(style.deckEnd);
        Vector2 leftPostBottom = Scale(style.leftPostBottom);
        Vector2 leftPostTop = Scale(style.leftPostTop);
        Vector2 rightPostBottom = Scale(style.rightPostBottom);
        Vector2 rightPostTop = Scale(style.rightPostTop);

        if (fillColor.a > 0.001f)
        {
            Vector2[] tray = { leftPostBottom, leftPostTop, rightPostTop, rightPostBottom };
            FillPolygon(buffer, width, height, tray, fillColor);
        }

        StrokeSegment(buffer, width, height, deckStart, deckEnd, deckStroke, lineColor);
        StrokeSegment(buffer, width, height, leftPostBottom, leftPostTop, postStroke, lineColor);
        StrokeSegment(buffer, width, height, rightPostBottom, rightPostTop, postStroke, lineColor);
        StrokeSegment(buffer, width, height, rightPostTop, Scale(style.hookEnd), hookStroke, lineColor);

        for (int i = 0; i < style.wheelCenters.Length; i++)
        {
            Vector2 center = Scale(style.wheelCenters[i]);
            float radiusX = style.wheelRadius.x * width;
            float radiusY = style.wheelRadius.y * height;

            FillEllipse(buffer, width, height, center, radiusX, radiusY, lineColor);

            float dashStart = center.x + radiusX + style.wheelDashGap * width;
            float dashLength = style.wheelDashLength * width;
            float dashStep = style.wheelDashSpacing * height;

            for (int dash = 0; dash < style.wheelDashCount; dash++)
            {
                float offsetY = (dash - (style.wheelDashCount - 1) * 0.5f) * dashStep;
                float length = dashLength * (1f - dash * 0.2f);
                StrokeSegment(
                    buffer,
                    width,
                    height,
                    new Vector2(dashStart, center.y + offsetY),
                    new Vector2(dashStart + length, center.y + offsetY),
                    dashStroke,
                    lineColor
                );
            }
        }

        for (int i = 0; i < style.trailCount; i++)
        {
            float t = style.trailCount <= 1 ? 0f : i / (style.trailCount - 1f);
            float normalizedY = Mathf.Lerp(style.trailStartY, style.trailEndY, t);
            float length = Mathf.Lerp(style.trailLengthMin, style.trailLengthMax, t);
            StrokeSegment(
                buffer,
                width,
                height,
                new Vector2(width * style.trailStartX, normalizedY * height),
                new Vector2(width * (style.trailStartX + length), normalizedY * height),
                dashStroke,
                lineColor
            );
        }

        return BuildSprite(buffer, width, height, "MenuCartFrame");
    }

    public static Sprite CreateSolidSprite(Color color, int size = 8)
    {
        size = Mathf.Clamp(size, 1, 256);
        Color[] buffer = new Color[size * size];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = color;
        }

        return BuildSprite(buffer, size, size, "MenuSolid");
    }

    private static Sprite BuildSprite(Color[] buffer, int width, int height, string name)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0
        };

        texture.SetPixels(buffer);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        sprite.name = name;
        return sprite;
    }

    private static void FillPolygon(Color[] buffer, int width, int height, Vector2[] polygon, Color color)
    {
        if (polygon == null || polygon.Length < 3)
        {
            return;
        }

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int i = 0; i < polygon.Length; i++)
        {
            minX = Mathf.Min(minX, Mathf.FloorToInt(polygon[i].x) - 2);
            minY = Mathf.Min(minY, Mathf.FloorToInt(polygon[i].y) - 2);
            maxX = Mathf.Max(maxX, Mathf.CeilToInt(polygon[i].x) + 2);
            maxY = Mathf.Max(maxY, Mathf.CeilToInt(polygon[i].y) + 2);
        }

        minX = Mathf.Clamp(minX, 0, width - 1);
        minY = Mathf.Clamp(minY, 0, height - 1);
        maxX = Mathf.Clamp(maxX, 0, width - 1);
        maxY = Mathf.Clamp(maxY, 0, height - 1);

        const int Samples = 2;
        float step = 1f / Samples;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int inside = 0;

                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        Vector2 point = new Vector2(x + (sx + 0.5f) * step, y + (sy + 0.5f) * step);

                        if (ContainsPoint(polygon, point))
                        {
                            inside++;
                        }
                    }
                }

                if (inside == 0)
                {
                    continue;
                }

                Composite(buffer, y * width + x, color, inside / (float)(Samples * Samples));
            }
        }
    }

    private static void StrokeSegment(Color[] buffer, int width, int height, Vector2 a, Vector2 b, float strokeWidth, Color color)
    {
        float half = strokeWidth * 0.5f;
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - half - 2f), 0, width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + half + 2f), 0, width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - half - 2f), 0, height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + half + 2f), 0, height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float distance = DistanceToSegment(point, a, b);
                float coverage = Mathf.Clamp01(half + 0.5f - distance);

                if (coverage <= 0f)
                {
                    continue;
                }

                Composite(buffer, y * width + x, color, coverage);
            }
        }
    }

    private static void FillEllipse(Color[] buffer, int width, int height, Vector2 center, float radiusX, float radiusY, Color color)
    {
        radiusX = Mathf.Max(1f, radiusX);
        radiusY = Mathf.Max(1f, radiusY);

        int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radiusX - 2f), 0, width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radiusX + 2f), 0, width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radiusY - 2f), 0, height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radiusY + 2f), 0, height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x + 0.5f - center.x) / radiusX;
                float ny = (y + 0.5f - center.y) / radiusY;
                float distance = (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(radiusX, radiusY);
                float coverage = Mathf.Clamp01(0.5f - distance);

                if (coverage <= 0f)
                {
                    continue;
                }

                Composite(buffer, y * width + x, color, coverage);
            }
        }
    }

    private static bool ContainsPoint(Vector2[] polygon, Vector2 point)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            if ((a.y > point.y) == (b.y > point.y))
            {
                continue;
            }

            float t = (point.y - a.y) / (b.y - a.y);

            if (point.x < a.x + t * (b.x - a.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;

        if (lengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
        return Vector2.Distance(point, a + ab * t);
    }

    private static float RoundedBoxDistance(Vector2 local, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.y));
        float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
        return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + outside - radius;
    }

    private static void Composite(Color[] buffer, int index, Color source, float coverage)
    {
        float sourceAlpha = Mathf.Clamp01(source.a) * Mathf.Clamp01(coverage);

        if (sourceAlpha <= 0f)
        {
            return;
        }

        Color destination = buffer[index];
        float outAlpha = sourceAlpha + destination.a * (1f - sourceAlpha);

        if (outAlpha <= 0f)
        {
            buffer[index] = new Color(0f, 0f, 0f, 0f);
            return;
        }

        float inverse = destination.a * (1f - sourceAlpha);
        buffer[index] = new Color(
            (source.r * sourceAlpha + destination.r * inverse) / outAlpha,
            (source.g * sourceAlpha + destination.g * inverse) / outAlpha,
            (source.b * sourceAlpha + destination.b * inverse) / outAlpha,
            outAlpha
        );
    }

    private static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t))
        );
    }
}
