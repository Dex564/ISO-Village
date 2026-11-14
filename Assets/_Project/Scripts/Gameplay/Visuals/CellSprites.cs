using UnityEngine;

/// <summary>
/// Спрайты ромба размером в одну клетку (32x16 пикселей при PPU 32), нарисованные кодом.
/// Используются курсором клетки и подсветкой выделенной области.
/// </summary>
public static class CellSprites
{
    private static Sprite outline;
    private static Sprite filled;

    /// <summary>Контур со слабой заливкой.</summary>
    public static Sprite Diamond
    {
        get
        {
            if (outline == null) outline = Create(40);
            return outline;
        }
    }

    /// <summary>Контур с заметной заливкой (для областей).</summary>
    public static Sprite DiamondFilled
    {
        get
        {
            if (filled == null) filled = Create(120);
            return filled;
        }
    }

    private static Sprite Create(byte fillAlpha)
    {
        const int w = 32;
        const int h = 16;
        const float edge = 0.16f;                // толщина контура в долях
        const float limit = 1f + 1f / 16f;       // ромб занимает ровно 32x16, как верхняя грань тайла

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f);
                float dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                float f = dx + dy; // f <= limit: внутри ромба

                byte alpha = 0;
                if (f <= limit) alpha = f > limit - edge ? (byte)255 : fillAlpha;

                pixels[x + y * w] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }
}