using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tiny in-code rasteriser used by DinohoppDinoSpriteBuilder to draw the dino sprites.
/// Pixels are stored as Color32 in a flat row-major array; alpha-blends new strokes
/// on top of what's already there so a layered "shadow → body → highlight → eyes"
/// composition reads correctly. Antialiasing is done by computing signed-distance to
/// each shape's edge and softening over a one-pixel band.
///
/// Not general-purpose; meant for sprite-sheet baking from Unity Editor scripts.
/// </summary>
public class SpriteCanvas
{
    public readonly int width;
    public readonly int height;
    readonly Color32[] pixels;

    public SpriteCanvas(int width, int height)
    {
        this.width  = width;
        this.height = height;
        pixels = new Color32[width * height];
        // Start fully transparent — sprites need an alpha channel to be cut out.
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
    }

    // ---------- Primitives ----------

    /// <summary>Filled ellipse centred at (cx, cy) with half-extents (rx, ry).</summary>
    public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        if (rx <= 0f || ry <= 0f) return;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
        int x1 = Mathf.Min(width  - 1, Mathf.CeilToInt(cx + rx + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry + 1));

        for (int y = y0; y <= y1; y++)
        {
            float dy = (y - cy) / ry;
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx;
                float d  = Mathf.Sqrt(dx * dx + dy * dy); // 0 at centre, 1 at edge
                float a  = SmoothEdge(d, 1f, EdgeSoftness(rx, ry));
                if (a > 0f) BlendPixel(x, y, color, a);
            }
        }
    }

    /// <summary>Filled circle. Convenience wrapper around FillEllipse.</summary>
    public void FillCircle(float cx, float cy, float r, Color color)
        => FillEllipse(cx, cy, r, r, color);

    /// <summary>Filled rounded rectangle. (x, y) is the bottom-left corner.</summary>
    public void FillRoundedRect(float x, float y, float w, float h, float radius, Color color)
    {
        radius = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(x - 1));
        int x1 = Mathf.Min(width  - 1, Mathf.CeilToInt(x + w + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(y - 1));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(y + h + 1));

        float xMinInner = x + radius;
        float xMaxInner = x + w - radius;
        float yMinInner = y + radius;
        float yMaxInner = y + h - radius;

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                // Signed distance to nearest corner-rounded edge.
                float qx = Mathf.Abs(px - (x + w * 0.5f)) - (w * 0.5f - radius);
                float qy = Mathf.Abs(py - (y + h * 0.5f)) - (h * 0.5f - radius);
                float outside = Vector2.Distance(
                    new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)),
                    Vector2.zero);
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float d = outside + inside; // <0 inside, >0 outside
                float a = Mathf.Clamp01(0.5f - d);
                if (a > 0f) BlendPixel(px, py, color, a);
            }
        }
    }

    /// <summary>Filled triangle p1 → p2 → p3. Uses barycentric fill with edge softening.</summary>
    public void FillTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p1.x, Mathf.Min(p2.x, p3.x)) - 1));
        int x1 = Mathf.Min(width  - 1, Mathf.CeilToInt(Mathf.Max(p1.x, Mathf.Max(p2.x, p3.x)) + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p1.y, Mathf.Min(p2.y, p3.y)) - 1));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(p1.y, Mathf.Max(p2.y, p3.y)) + 1));

        float denom = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
        if (Mathf.Approximately(denom, 0f)) return;

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x, y);
                float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denom;
                float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denom;
                float c = 1f - a - b;
                float minBary = Mathf.Min(a, Mathf.Min(b, c));
                // Soft edge: minBary < 0 = outside, > 0 = inside. Smoothstep across half a pixel.
                float alpha = Mathf.Clamp01(minBary * 80f + 0.5f);
                if (alpha > 0f) BlendPixel(x, y, color, alpha);
            }
        }
    }

    /// <summary>Thick line from a to b with rounded caps. Thickness is a pixel diameter.</summary>
    public void DrawLine(Vector2 a, Vector2 b, float thickness, Color color)
    {
        float r = thickness * 0.5f;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r - 1));
        int x1 = Mathf.Min(width  - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r - 1));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r + 1));

        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 1e-6f)
        {
            FillCircle(a.x, a.y, r, color);
            return;
        }

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                Vector2 closest = a + ab * t;
                float d = Vector2.Distance(p, closest);
                float alpha = Mathf.Clamp01(r + 0.5f - d);
                if (alpha > 0f) BlendPixel(x, y, color, alpha);
            }
        }
    }

    /// <summary>
    /// Filled capsule (rectangle with two semicircle caps) along the segment a→b.
    /// Same as DrawLine but exposed under a clearer name for limbs/tails.
    /// </summary>
    public void FillCapsule(Vector2 a, Vector2 b, float thickness, Color color)
        => DrawLine(a, b, thickness, color);

    // ---------- Pixel ops ----------

    void BlendPixel(int x, int y, Color src, float coverage)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int i = y * width + x;
        Color32 dst = pixels[i];

        float srcA = src.a * coverage;
        float invA = 1f - srcA;
        float dstA = dst.a / 255f;
        float outA = srcA + dstA * invA;

        if (outA <= 0f) return;

        float outR = (src.r * srcA + (dst.r / 255f) * dstA * invA) / outA;
        float outG = (src.g * srcA + (dst.g / 255f) * dstA * invA) / outA;
        float outB = (src.b * srcA + (dst.b / 255f) * dstA * invA) / outA;

        pixels[i] = new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(outR * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(outG * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(outB * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(outA * 255f), 0, 255));
    }

    /// <summary>Smooth edge: returns alpha 1 inside radius, 0 outside, soft over `softness` band.</summary>
    static float SmoothEdge(float d, float edge, float softness)
    {
        float t = (edge - d) / softness;
        return Mathf.Clamp01(t);
    }

    /// <summary>Edge softness scaled to roughly one pixel in the smaller axis.</summary>
    static float EdgeSoftness(float rx, float ry)
        => 1f / Mathf.Max(1f, Mathf.Min(rx, ry));

    // ---------- Export ----------

    /// <summary>
    /// Save the canvas as a PNG and configure it as a Unity Sprite with the given
    /// pixels-per-unit and pivot (in normalised 0..1 sprite coords).
    /// </summary>
    public void SaveAsSprite(string assetPath, float pixelsPerUnit, Vector2 pivotNormalised)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
        tex.SetPixels32(pixels);
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(assetPath, bytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType        = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteImportMode   = SpriteImportMode.Single;
            importer.filterMode         = FilterMode.Bilinear;
            importer.mipmapEnabled      = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot     = pivotNormalised;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
