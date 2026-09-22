using UnityEngine;

// Procedural felt/table backdrop: sits behind world sprites and never intercepts input.
public sealed class BoardBackdrop : MonoBehaviour
{
    private Texture2D texture;
    private Sprite sprite;
    private Transform surface;

    private void Start()
    {
        const int size = 256;
        texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - 127.5f) / 128f, dy = (y - 127.5f) / 128f;
                float glow = Mathf.Clamp01(1 - (dx * dx + dy * dy) * 0.65f);
                float grain = (((x * 73 + y * 151) % 17) / 16f - 0.5f) * 0.009f;
                var color = Color.Lerp(new Color(0.025f, 0.04f, 0.05f), new Color(0.10f, 0.19f, 0.17f), glow);
                pixels[y * size + x] = color + new Color(grain, grain, grain, 0);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        var go = new GameObject("Felt background", typeof(SpriteRenderer));
        surface = go.transform;
        surface.SetParent(transform, false);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -1000;
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        if (surface == null || camera == null) return;
        surface.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 5);
        surface.localScale = new Vector3(camera.orthographicSize * camera.aspect * 2.1f, camera.orthographicSize * 2.1f, 1);
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }
}
