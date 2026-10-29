using System;
using System.Collections.Generic;
using UnityEngine;

namespace VhsInheritance
{
    public enum VhsFixtureKind
    {
        Counter,
        Shelf,
        Machine,
        Pile,
        Television,
        UpgradeStand,
        Door
    }

    public enum VhsDecorationKind
    {
        Rug,
        Plant,
        Poster,
        TrashCan,
        CardboardBox,
        CableCoil
    }

    public static class VhsSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Color32 Ink = new Color32(27, 24, 42, 255);
        private static readonly Color32 Cream = new Color32(244, 225, 184, 255);
        private static readonly Color32 Teal = new Color32(55, 164, 158, 255);
        private static readonly Color32 Pink = new Color32(218, 74, 132, 255);
        private static readonly Color32 Gold = new Color32(236, 168, 70, 255);
        private static readonly Color32 Purple = new Color32(99, 69, 140, 255);
        private static readonly Color32 Wood = new Color32(112, 66, 45, 255);
        private static readonly Color32 WoodLight = new Color32(170, 102, 65, 255);

        private static Sprite Cartoon(string name)
        {
            if (Cache.TryGetValue("cartoon_" + name, out Sprite sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("Art/Cartoon/" + name);
            if (sprite == null) throw new InvalidOperationException("Cartoon sprite not imported: " + name);
            Cache["cartoon_" + name] = sprite;
            return sprite;
        }

        public static Sprite White()
        {
            return GetOrCreate("white", 2, 2, 1f, new Vector2(0.5f, 0.5f), (pixels, width, height) =>
            {
                Fill(pixels, new Color32(255, 255, 255, 255));
            });
        }

        public static Sprite Background()
        {
            return Cartoon("background");
        }

        public static Sprite Glow()
        {
            return Cartoon("glow");
        }

        public static Sprite Vignette()
        {
            return Cartoon("vignette");
        }

        public static Sprite Decoration(VhsDecorationKind kind)
        {
            return Cartoon("decoration_" + kind);
        }

        public static Sprite Shadow()
        {
            return Cartoon("shadow");
        }

        public static Sprite Player()
        {
            return Cartoon("player");
        }

        public static Sprite Assistant()
        {
            return Cartoon("assistant");
        }

        public static Sprite Visitor(int variant)
        {
            return Cartoon("visitor_" + Mathf.Abs(variant % 6));
        }

        public static Sprite MysteryPerson()
        {
            return Cartoon("mystery");
        }

        public static Sprite Fixture(VhsFixtureKind kind)
        {
            return Cartoon("fixture_" + kind);
        }

        public static Sprite Cassette(CassetteGenre genre, bool foreign = false)
        {
            return Cartoon("cassette_" + (foreign ? "foreign" : genre.ToString()));
        }

        public static Sprite SlotMarker(CassetteGenre genre, bool misplaced)
        {
            return Cartoon("slot_" + genre + (misplaced ? "_wrong" : ""));
        }

        public static Sprite SpeechBubble()
        {
            return Cartoon("bubble");
        }

        public static Color GenreColor(CassetteGenre genre)
        {
            return GenreColor32(genre);
        }

        private static Color32 GenreColor32(CassetteGenre genre)
        {
            switch (genre)
            {
                case CassetteGenre.Action: return new Color32(203, 130, 127, 255);
                case CassetteGenre.Comedy: return new Color32(239, 204, 132, 255);
                case CassetteGenre.Drama: return new Color32(197, 175, 216, 255);
                case CassetteGenre.Audio: return new Color32(121, 170, 167, 255);
                default: return Cream;
            }
        }

        private static void DrawCounter(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(4, 10), new Vector2Int(width - 4, 10), new Vector2Int(width - 17, 66), new Vector2Int(17, 66)
            }, new Color32(22, 20, 27, 220));
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(8, 15), new Vector2Int(width - 8, 15), new Vector2Int(width - 24, 62), new Vector2Int(24, 62)
            }, Wood);
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(8, 15), new Vector2Int(width - 8, 15), new Vector2Int(width - 8, 7), new Vector2Int(8, 7)
            }, Ink);
            Rect(pixels, width, height, 19, 28, width - 38, 21, new Color32(91, 50, 40, 255));
            Rect(pixels, width, height, 24, 33, 48, 11, new Color32(40, 35, 43, 255));
            Rect(pixels, width, height, 29, 36, 38, 5, Pink);
            Rect(pixels, width, height, 84, 31, 59, 14, new Color32(43, 41, 48, 255));
            for (int i = 0; i < 5; i++)
            {
                Rect(pixels, width, height, 90 + i * 10, 35, 6, 6, i % 2 == 0 ? Teal : Gold);
            }
            Rect(pixels, width, height, 19, 57, width - 38, 9, new Color32(205, 142, 83, 255));
            Rect(pixels, width, height, 25, 61, width - 50, 3, Cream);
            FillEllipse(pixels, width, height, 148, 54, 6, 4, new Color32(31, 30, 37, 255));
            AddNoise(pixels, width, height, 7, 41);
        }

        private static void DrawShelf(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(7, 2), new Vector2Int(width - 5, 7), new Vector2Int(width - 5, height - 7),
                new Vector2Int(width - 13, height - 2), new Vector2Int(7, height - 7)
            }, new Color32(19, 17, 24, 255));
            Rect(pixels, width, height, 8, 6, width - 20, height - 13, new Color32(91, 49, 39, 255));
            Rect(pixels, width, height, 13, 11, width - 30, height - 24, new Color32(27, 27, 34, 255));
            Rect(pixels, width, height, width - 12, 10, 6, height - 22, new Color32(54, 31, 35, 255));
            Rect(pixels, width, height, 4, height - 17, width - 11, 12, Ink);
            Rect(pixels, width, height, 9, height - 14, width - 21, 6, WoodLight);
            for (int y = 21; y < height - 13; y += 14)
            {
                Rect(pixels, width, height, 10, y, width - 22, 5, Ink);
                Rect(pixels, width, height, 13, y + 2, width - 28, 3, new Color32(157, 84, 53, 255));
                Line(pixels, width, height, 14, y + 5, width - 16, y + 5, new Color32(226, 142, 74, 120));
            }
            Rect(pixels, width, height, 14, height - 18, width - 28, 11, Cream);
            Rect(pixels, width, height, 18, height - 15, width - 36, 5, Pink);
            Rect(pixels, width, height, 10, 2, 10, 6, Ink);
            Rect(pixels, width, height, width - 24, 2, 10, 6, Ink);
            AddNoise(pixels, width, height, 6, 17);
        }

        private static void DrawMachine(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(7, 3), new Vector2Int(width - 12, 3), new Vector2Int(width - 4, 11),
                new Vector2Int(width - 4, height - 13), new Vector2Int(width - 12, height - 5),
                new Vector2Int(7, height - 5)
            }, new Color32(17, 18, 24, 255));
            Rect(pixels, width, height, 11, 7, width - 26, height - 16, new Color32(50, 59, 62, 255));
            Rect(pixels, width, height, 15, 11, width - 34, height - 25, new Color32(75, 82, 77, 255));
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(0, 45), new Vector2Int(30, 45), new Vector2Int(30, 65),
                new Vector2Int(10, 72), new Vector2Int(0, 67)
            }, Ink);
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(4, 49), new Vector2Int(28, 49), new Vector2Int(28, 61),
                new Vector2Int(10, 67), new Vector2Int(4, 64)
            }, new Color32(90, 65, 52, 255));
            Rect(pixels, width, height, 7, 52, 18, 7, new Color32(28, 27, 34, 255));
            Rect(pixels, width, height, 20, 79, width - 40, 25, new Color32(38, 43, 48, 255));
            Rect(pixels, width, height, 25, 84, width - 50, 14, new Color32(101, 213, 192, 255));
            Line(pixels, width, height, 25, 84, 66, 97, Pink);
            Rect(pixels, width, height, 17, 49, width - 34, 22, new Color32(46, 45, 47, 255));
            Rect(pixels, width, height, 24, 55, width - 48, 9, Ink);
            Rect(pixels, width, height, 30, 57, width - 60, 5, Pink);
            Rect(pixels, width, height, 17, 16, 26, 25, new Color32(45, 48, 48, 255));
            Rect(pixels, width, height, 22, 21, 16, 15, new Color32(185, 105, 52, 255));
            Rect(pixels, width, height, 51, 17, 22, 24, new Color32(33, 36, 40, 255));
            for (int i = 0; i < 4; i++)
            {
                FillEllipse(pixels, width, height, 62, 22 + i * 6, 3, 3, i % 2 == 0 ? Pink : Gold);
            }
            Rect(pixels, width, height, width - 15, 15, 5, 79, new Color32(31, 35, 40, 255));
            for (int vent = 0; vent < 7; vent++)
            {
                Rect(pixels, width, height, width - 14, 24 + vent * 9, 3, 5, new Color32(112, 119, 107, 255));
            }
            FillEllipse(pixels, width, height, 27, 73, 5, 5, Gold);
            FillEllipse(pixels, width, height, 45, 73, 5, 5, Teal);
            FillEllipse(pixels, width, height, 64, 73, 5, 5, Pink);
            Rect(pixels, width, height, 40, height - 6, 12, 4, Ink);
            FillEllipse(pixels, width, height, 46, height - 2, 5, 5, Pink);
            FillEllipse(pixels, width, height, 46, height - 2, 2, 2, Cream);
            for (int i = 0; i < 9; i++)
            {
                int x = 14 + HashNoise(i, 7, 5) % 62;
                int y = 12 + HashNoise(i, 3, 9) % 92;
                Rect(pixels, width, height, x, y, 2, 2, new Color32(185, 100, 61, 255));
            }
            AddNoise(pixels, width, height, 8, 53);
        }

        private static void DrawPile(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(4, 5), new Vector2Int(width - 4, 5), new Vector2Int(width - 13, 34), new Vector2Int(13, 34)
            }, Ink);
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(9, 10), new Vector2Int(width - 9, 10), new Vector2Int(width - 18, 30), new Vector2Int(18, 30)
            }, new Color32(70, 50, 44, 255));
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(14, 16), new Vector2Int(width - 14, 16), new Vector2Int(width - 23, 27), new Vector2Int(23, 27)
            }, new Color32(24, 24, 30, 255));
            Rect(pixels, width, height, 8, 4, width - 16, 9, new Color32(100, 58, 45, 255));
            Rect(pixels, width, height, 14, 7, width - 28, 3, WoodLight);
            for (int brace = 0; brace < 4; brace++)
            {
                Rect(pixels, width, height, 22 + brace * 20, 13, 4, 16, new Color32(39, 34, 37, 255));
            }
            FillEllipse(pixels, width, height, 14, 9, 2, 2, Gold);
            FillEllipse(pixels, width, height, width - 14, 9, 2, 2, Gold);
            AddNoise(pixels, width, height, 6, 73);
        }

        private static void DrawTelevision(Color32[] pixels, int width, int height)
        {
            Rect(pixels, width, height, 7, 17, width - 14, height - 24, new Color32(17, 17, 23, 255));
            Rect(pixels, width, height, 12, 22, width - 24, height - 35, new Color32(42, 43, 47, 255));
            Rect(pixels, width, height, 18, 29, width - 36, height - 49, new Color32(36, 57, 62, 255));
            Rect(pixels, width, height, 22, 33, width - 44, height - 57, new Color32(82, 155, 151, 255));
            Line(pixels, width, height, 23, 35, width - 24, height - 31, new Color32(230, 109, 171, 255));
            Line(pixels, width, height, 22, height - 34, width - 24, 37, new Color32(226, 225, 187, 255));
            FillEllipse(pixels, width, height, width - 18, 34, 4, 4, Gold);
            FillEllipse(pixels, width, height, width - 18, 48, 4, 4, Pink);
            Rect(pixels, width, height, 15, 5, width - 30, 16, new Color32(67, 46, 40, 255));
            Rect(pixels, width, height, 21, 9, width - 42, 7, Ink);
            Rect(pixels, width, height, 29, 11, width - 58, 3, Pink);
            AddNoise(pixels, width, height, 5, 83);
        }

        private static void DrawStand(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(10, 6), new Vector2Int(width - 10, 6), new Vector2Int(width - 18, 51), new Vector2Int(18, 51)
            }, Wood);
            Rect(pixels, width, height, 15, 48, width - 30, 8, Cream);
            Rect(pixels, width, height, 24, 56, width - 48, 19, Pink);
            Rect(pixels, width, height, 29, 61, width - 58, 9, Gold);
            Rect(pixels, width, height, 28, 23, width - 56, 13, Teal);
            Rect(pixels, width, height, 32, 26, width - 64, 7, new Color32(22, 25, 31, 255));
            AddNoise(pixels, width, height, 7, 97);
        }

        private static void DrawDoor(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(5, 9), new Vector2Int(width - 5, 9), new Vector2Int(width - 14, height - 6), new Vector2Int(14, height - 6)
            }, Ink);
            FillPolygon(pixels, width, height, new[]
            {
                new Vector2Int(11, 13), new Vector2Int(width - 11, 13), new Vector2Int(width - 19, height - 11), new Vector2Int(19, height - 11)
            }, new Color32(75, 112, 111, 255));
            Rect(pixels, width, height, width - 26, 23, 5, 5, Gold);
            Rect(pixels, width, height, 28, 22, width - 56, 7, Pink);
            AddNoise(pixels, width, height, 5, 101);
        }

        private static void DrawRug(Color32[] pixels, int width, int height)
        {
            FillEllipse(pixels, width, height, width / 2, height / 2, width / 2 - 4, height / 2 - 4, new Color32(31, 23, 36, 230));
            FillEllipse(pixels, width, height, width / 2, height / 2, width / 2 - 10, height / 2 - 10, new Color32(78, 35, 59, 235));
            for (int x = 16; x < width - 16; x += 13)
            {
                Line(pixels, width, height, x, 12, width - x, height - 13, new Color32(128, 62, 75, 170));
            }
            Rect(pixels, width, height, 24, 36, width - 48, 7, new Color32(195, 104, 70, 170));
            AddNoise(pixels, width, height, 6, 113);
        }

        private static void DrawPlant(Color32[] pixels, int width, int height)
        {
            Rect(pixels, width, height, 19, 4, 30, 28, new Color32(74, 43, 37, 255));
            FillPolygon(pixels, width, height, new[] { new Vector2Int(15, 32), new Vector2Int(53, 32), new Vector2Int(46, 8), new Vector2Int(22, 8) }, new Color32(129, 69, 48, 255));
            Line(pixels, width, height, 34, 30, 34, 69, new Color32(44, 86, 57, 255));
            for (int i = 0; i < 11; i++)
            {
                int cx = 12 + HashNoise(i, 2, 41) % 45;
                int cy = 43 + HashNoise(i, 6, 43) % 38;
                int rx = 8 + i % 4;
                int ry = 5 + (i + 1) % 5;
                FillEllipse(pixels, width, height, cx, cy, rx, ry, i % 3 == 0 ? new Color32(51, 119, 75, 255) : new Color32(37, 91, 61, 255));
                Line(pixels, width, height, 34, 47, cx, cy, new Color32(42, 83, 56, 255));
            }
            AddNoise(pixels, width, height, 5, 127);
        }

        private static void DrawPoster(Color32[] pixels, int width, int height)
        {
            Rect(pixels, width, height, 3, 3, width - 6, height - 6, new Color32(20, 20, 28, 255));
            Rect(pixels, width, height, 7, 7, width - 14, height - 14, new Color32(42, 48, 75, 255));
            FillEllipse(pixels, width, height, 32, 57, 22, 18, new Color32(221, 86, 118, 255));
            FillPolygon(pixels, width, height, new[] { new Vector2Int(7, 18), new Vector2Int(57, 18), new Vector2Int(47, 42), new Vector2Int(17, 42) }, new Color32(28, 112, 119, 255));
            Rect(pixels, width, height, 13, 70, width - 26, 6, Gold);
            Rect(pixels, width, height, 18, 80, width - 36, 4, Cream);
            AddNoise(pixels, width, height, 5, 131);
        }

        private static void DrawTrashCan(Color32[] pixels, int width, int height)
        {
            FillEllipse(pixels, width, height, 24, 49, 18, 7, new Color32(23, 23, 29, 255));
            FillPolygon(pixels, width, height, new[] { new Vector2Int(9, 47), new Vector2Int(39, 47), new Vector2Int(34, 6), new Vector2Int(14, 6) }, new Color32(73, 75, 79, 255));
            for (int x = 15; x < 37; x += 6)
            {
                Line(pixels, width, height, x, 9, x + 2, 44, new Color32(41, 43, 48, 255));
            }
            Rect(pixels, width, height, 12, 48, 24, 5, new Color32(125, 119, 102, 255));
            AddNoise(pixels, width, height, 7, 137);
        }

        private static void DrawBox(Color32[] pixels, int width, int height)
        {
            FillPolygon(pixels, width, height, new[] { new Vector2Int(5, 7), new Vector2Int(63, 7), new Vector2Int(57, 43), new Vector2Int(12, 43) }, new Color32(132, 80, 50, 255));
            Line(pixels, width, height, 34, 8, 34, 43, new Color32(82, 48, 40, 255));
            Line(pixels, width, height, 11, 42, 34, 31, new Color32(193, 123, 71, 255));
            Line(pixels, width, height, 58, 42, 34, 31, new Color32(193, 123, 71, 255));
            Rect(pixels, width, height, 13, 15, 22, 12, Cream);
            Line(pixels, width, height, 16, 18, 31, 23, new Color32(78, 55, 46, 255));
            AddNoise(pixels, width, height, 8, 149);
        }

        private static void DrawCableCoil(Color32[] pixels, int width, int height)
        {
            for (int radius = 9; radius < 26; radius += 5)
            {
                for (int angle = 0; angle < 360; angle += 8)
                {
                    float radians = angle * Mathf.Deg2Rad;
                    int x = 40 + Mathf.RoundToInt(Mathf.Cos(radians) * radius);
                    int y = 25 + Mathf.RoundToInt(Mathf.Sin(radians) * radius * 0.55f);
                    Set(pixels, width, height, x, y, new Color32(24, 23, 30, 255));
                    Set(pixels, width, height, x + 1, y, new Color32(119, 42, 49, 255));
                }
            }
            Rect(pixels, width, height, 63, 21, 12, 6, new Color32(25, 25, 31, 255));
            Rect(pixels, width, height, 69, 18, 5, 12, Gold);
        }

        private static Sprite GetOrCreate(string key, int width, int height, float pixelsPerUnit, Vector2 pivot, Action<Color32[], int, int> draw)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Color32[] pixels = new Color32[width * height];
            draw(pixels, width, height);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "VHS_" + key,
                filterMode = key == "background" || key == "glow" || key == "vignette" ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "VHS_" + key;
            sprite.hideFlags = HideFlags.DontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        private static void Rect(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color)
        {
            int xMin = Mathf.Max(0, x);
            int xMax = Mathf.Min(width, x + rectWidth);
            int yMin = Mathf.Max(0, y);
            int yMax = Mathf.Min(height, y + rectHeight);
            for (int py = yMin; py < yMax; py++)
            {
                int offset = py * width;
                for (int px = xMin; px < xMax; px++)
                {
                    pixels[offset + px] = color;
                }
            }
        }

        private static void FillEllipse(Color32[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float nx = radiusX == 0 ? 0f : x / (float)radiusX;
                    float ny = radiusY == 0 ? 0f : y / (float)radiusY;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        Set(pixels, width, height, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void Line(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                Set(pixels, width, height, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int twice = error * 2;
                if (twice >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (twice <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void FillPolygon(Color32[] pixels, int width, int height, Vector2Int[] points, Color32 color)
        {
            int minX = width - 1;
            int maxX = 0;
            int minY = height - 1;
            int maxY = 0;
            for (int i = 0; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInPolygon(x + 0.5f, y + 0.5f, points))
                    {
                        Set(pixels, width, height, x, y, color);
                    }
                }
            }
        }

        private static bool PointInPolygon(float x, float y, Vector2Int[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2Int pi = points[i];
                Vector2Int pj = points[j];
                bool intersects = (pi.y > y) != (pj.y > y) && x < (pj.x - pi.x) * (y - pi.y) / (float)(pj.y - pi.y) + pi.x;
                if (intersects)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void AddNoise(Color32[] pixels, int width, int height, int amount, int seed)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color32 color = pixels[index];
                    if (color.a == 0)
                    {
                        continue;
                    }
                    int delta = HashNoise(x, y, seed) % (amount * 2 + 1) - amount;
                    pixels[index] = new Color32(
                        (byte)Mathf.Clamp(color.r + delta, 0, 255),
                        (byte)Mathf.Clamp(color.g + delta, 0, 255),
                        (byte)Mathf.Clamp(color.b + delta, 0, 255),
                        color.a);
                }
            }
        }

        private static void DarkenEdges(Color32[] pixels, int width, int height, float strength)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = Mathf.Abs(x - width * 0.5f) / (width * 0.5f);
                    float ny = Mathf.Abs(y - height * 0.5f) / (height * 0.5f);
                    float shade = Mathf.Clamp01(Mathf.Max(nx, ny) - 0.55f) / 0.45f * strength;
                    int index = y * width + x;
                    Color32 color = pixels[index];
                    pixels[index] = new Color32(
                        (byte)(color.r * (1f - shade)),
                        (byte)(color.g * (1f - shade)),
                        (byte)(color.b * (1f - shade)),
                        color.a);
                }
            }
        }

        private static int HashNoise(int x, int y, int seed)
        {
            unchecked
            {
                uint value = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                value = (value ^ (value >> 13)) * 1274126177u;
                return (int)(value ^ (value >> 16)) & 0x7fffffff;
            }
        }

        private static Color32 Offset(Color color, int delta)
        {
            Color32 value = color;
            return new Color32(
                (byte)Mathf.Clamp(value.r + delta, 0, 255),
                (byte)Mathf.Clamp(value.g + delta, 0, 255),
                (byte)Mathf.Clamp(value.b + delta, 0, 255),
                value.a);
        }

        private static void Set(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                pixels[y * width + x] = color;
            }
        }
    }
}
