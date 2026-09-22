using UnityEngine;
using UnityEngine.UI;

// Resolution-independent rounded button and eight-tooth gear, matching the felt theme.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SettingsButtonGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = GetPixelAdjustedRect();
        Vector2 center = bounds.center;
        float scale = Mathf.Min(bounds.width, bounds.height) / 40f;
        RoundedPanel(mesh, center, 20 * scale, 8 * scale, new Color(0.22f, 0.39f, 0.33f));
        RoundedPanel(mesh, center, 19 * scale, 7 * scale, new Color(0.07f, 0.14f, 0.13f));
        int start = mesh.currentVertCount;
        const int segments = 64;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            int phase = i % 8;
            float outer = phase >= 2 && phase <= 5 ? 12.5f : 10f;
            Color mint = new Color(0.70f, 0.88f, 0.79f);
            mesh.AddVert(center + direction * outer * scale, mint, Vector2.zero);
            mesh.AddVert(center + direction * 5.6f * scale, mint, Vector2.zero);
            if (i == segments) continue;
            int v = start + i * 2;
            mesh.AddTriangle(v, v + 2, v + 1);
            mesh.AddTriangle(v + 1, v + 2, v + 3);
        }
    }

    private static void RoundedPanel(VertexHelper mesh, Vector2 center, float half, float radius, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        const int perCorner = 8;
        for (int corner = 0; corner < 4; corner++)
        {
            float baseAngle = corner * Mathf.PI * 0.5f;
            var offset = new Vector2(corner == 0 || corner == 3 ? 1 : -1, corner < 2 ? 1 : -1) * (half - radius);
            for (int i = 0; i <= perCorner; i++)
            {
                float angle = baseAngle + i * Mathf.PI * 0.5f / perCorner;
                mesh.AddVert(center + offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
            }
        }
        int count = 4 * (perCorner + 1);
        for (int i = 0; i < count; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
    }
}
