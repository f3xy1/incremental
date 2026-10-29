using System.Collections.Generic;
using UnityEngine;

namespace VhsInheritance
{
    // Small, antialiased code-drawn UI assets. No external image generation.
    public static class VhsVectorArt
    {
        private static Sprite rounded;
        private static Sprite border;
        private static readonly Dictionary<UpgradeType, Sprite> Icons = new Dictionary<UpgradeType, Sprite>();

        public static Sprite RoundedPanel()
        {
            if (rounded != null) return rounded;
            const int size = 64;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(Mathf.Abs(x + 0.5f - 32f), Mathf.Abs(y + 0.5f - 32f));
                Vector2 q = new Vector2(Mathf.Max(0f, p.x - 18f), Mathf.Max(0f, p.y - 18f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(14f - q.magnitude));
            }
            Texture2D texture = Texture(pixels, size);
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f, 0,
                SpriteMeshType.FullRect, new Vector4(17, 17, 17, 17));
            rounded.name = "Vector rounded panel";
            return rounded;
        }

        public static Sprite UpgradeIcon(UpgradeType type)
        {
            if (Icons.TryGetValue(type, out Sprite sprite) && sprite != null) return sprite;
            const int size = 64;
            Color[] pixels = new Color[size * size];
            void Line(float ax, float ay, float bx, float by)
            {
                Vector2 a = new Vector2(ax, ay), b = new Vector2(bx, by), ab = b - a;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.01f, ab.sqrMagnitude));
                    float alpha = Mathf.Clamp01(3f - Vector2.Distance(p, a + ab * t));
                    if (alpha > pixels[y * size + x].a) pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }
            void Box(float x, float y, float w, float h)
            {
                Line(x, y, x + w, y); Line(x + w, y, x + w, y + h);
                Line(x + w, y + h, x, y + h); Line(x, y + h, x, y);
            }
            switch (type)
            {
                case UpgradeType.CarryCapacity: Box(13, 18, 38, 27); Line(22, 33, 42, 33); Line(19, 51, 45, 51); break;
                case UpgradeType.WalkSpeed: Line(34, 52, 20, 30); Line(20, 30, 40, 30); Line(40, 30, 27, 12); Line(10, 42, 20, 42); break;
                case UpgradeType.BasketCapacity: Line(12, 43, 20, 16); Line(20, 16, 44, 16); Line(44, 16, 52, 43); Line(10, 43, 54, 43); Line(24, 47, 28, 54); Line(40, 47, 36, 54); break;
                case UpgradeType.ExtraGenre: Box(13, 13, 38, 38); Line(21, 25, 43, 25); Line(32, 32, 32, 45); Line(26, 38, 38, 38); break;
                case UpgradeType.ShelfCapacity: Box(13, 13, 38, 38); Line(16, 26, 48, 26); Line(16, 39, 48, 39); Line(25, 29, 25, 36); Line(37, 42, 37, 49); break;
                case UpgradeType.OrderPayout: Box(11, 20, 42, 27); Line(29, 52, 29, 14); Line(38, 41, 24, 41); Line(24, 41, 24, 33); Line(24, 33, 38, 33); Line(38, 33, 38, 26); Line(38, 26, 24, 26); break;
                case UpgradeType.Signage: Box(12, 23, 40, 25); Line(21, 14, 21, 23); Line(43, 14, 43, 23); Line(22, 35, 42, 35); Line(17, 53, 17, 57); Line(47, 53, 47, 57); break;
                case UpgradeType.VisitorPatience: Box(19, 17, 26, 31); Line(32, 42, 32, 31); Line(32, 31, 39, 26); Line(25, 54, 39, 54); break;
                case UpgradeType.CounterAssistant: Box(23, 38, 18, 16); Line(15, 12, 15, 25); Line(15, 25, 23, 32); Line(23, 32, 41, 32); Line(41, 32, 49, 25); Line(49, 25, 49, 12); Line(15, 12, 49, 12); break;
                case UpgradeType.AudioCassettes: Box(11, 18, 42, 29); Box(18, 27, 8, 9); Box(38, 27, 8, 9); Line(23, 20, 26, 25); Line(26, 25, 38, 25); Line(38, 25, 41, 20); break;
            }
            sprite = Sprite.Create(Texture(pixels, size), new Rect(0, 0, size, size), Vector2.one * 0.5f, 32f);
            sprite.name = "Vector " + type;
            Icons[type] = sprite;
            return sprite;
        }

        public static Sprite PanelBorder()
        {
            if (border != null) return border;
            const int size = 64;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(Mathf.Abs(x + 0.5f - 32f), Mathf.Abs(y + 0.5f - 32f));
                float outer = new Vector2(Mathf.Max(0, p.x - 18), Mathf.Max(0, p.y - 18)).magnitude - 14;
                float inner = new Vector2(Mathf.Max(0, p.x - 18), Mathf.Max(0, p.y - 18)).magnitude - 10;
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(-outer) * Mathf.Clamp01(inner));
            }
            border = Sprite.Create(Texture(pixels, size), new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f, 0,
                SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
            return border;
        }

        private static Texture2D Texture(Color[] pixels, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
