using UnityEngine;

namespace MazeGame
{
    public static class SpriteFactory
    {
        private static Sprite _wallSprite;
        private static Sprite _floorSprite;
        private static Sprite _playerSprite;
        private static Sprite _coinSprite;
        private static Sprite _exitSprite;

        public static Sprite GetWallSprite()
        {
            if (_wallSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;

                Color baseColor = new Color(0.12f, 0.15f, 0.22f); // Dark navy slate
                Color borderTopLeft = new Color(0.24f, 0.30f, 0.42f); // Lighter highlight
                Color borderBottomRight = new Color(0.06f, 0.08f, 0.12f); // Deep shadow
                Color innerColor = new Color(0.16f, 0.20f, 0.28f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (x < 3 || y >= size - 3)
                            tex.SetPixel(x, y, borderTopLeft);
                        else if (x >= size - 3 || y < 3)
                            tex.SetPixel(x, y, borderBottomRight);
                        else if (x == 3 || y == 3 || x == size - 4 || y == size - 4)
                            tex.SetPixel(x, y, baseColor);
                        else
                        {
                            // Subtle texture grain
                            float noise = ((x * 17 + y * 31) % 7) * 0.01f;
                            tex.SetPixel(x, y, innerColor + new Color(noise, noise, noise));
                        }
                    }
                }
                tex.Apply();
                _wallSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _wallSprite;
        }

        public static Sprite GetFloorSprite()
        {
            if (_floorSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;

                Color floorBase = new Color(0.05f, 0.07f, 0.10f); // Dark floor
                Color floorDot = new Color(0.08f, 0.11f, 0.15f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool isBorder = (x == 0 || y == 0 || x == size - 1 || y == size - 1);
                        tex.SetPixel(x, y, isBorder ? floorDot : floorBase);
                    }
                }
                tex.Apply();
                _floorSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _floorSprite;
        }

        public static Sprite GetPlayerSprite()
        {
            if (_playerSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;

                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.42f;

                Color playerBody = new Color(0.18f, 0.85f, 0.75f); // Cyan
                Color rimColor = new Color(0.6f, 1f, 0.95f); // Bright rim
                Color eyeColor = new Color(0.05f, 0.1f, 0.15f); // Dark cute eye

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            if (dist > radius - 3.5f)
                            {
                                tex.SetPixel(x, y, rimColor);
                            }
                            else
                            {
                                // Eyes
                                float eye1Dist = Vector2.Distance(new Vector2(x, y), center + new Vector2(5f, 5f));
                                float eye2Dist = Vector2.Distance(new Vector2(x, y), center + new Vector2(14f, 5f));
                                if (eye1Dist < 3.5f || eye2Dist < 3.5f)
                                {
                                    tex.SetPixel(x, y, eyeColor);
                                }
                                else
                                {
                                    tex.SetPixel(x, y, playerBody);
                                }
                            }
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                _playerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _playerSprite;
        }

        public static Sprite GetCoinSprite()
        {
            if (_coinSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;

                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.32f;

                Color coinGold = new Color(1.0f, 0.84f, 0.0f);
                Color coinShine = new Color(1.0f, 0.98f, 0.6f);
                Color coinShadow = new Color(0.85f, 0.65f, 0.0f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            if (dist > radius - 2.5f)
                                tex.SetPixel(x, y, coinShadow);
                            else if (dist < radius * 0.5f && (x > center.x - 3 && y > center.y))
                                tex.SetPixel(x, y, coinShine);
                            else
                                tex.SetPixel(x, y, coinGold);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                _coinSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _coinSprite;
        }

        public static Sprite GetExitSprite()
        {
            if (_exitSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;

                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.44f;

                Color portalOuter = new Color(0.2f, 0.85f, 0.3f, 0.9f); // Glowing Green
                Color portalMid = new Color(0.4f, 1.0f, 0.6f, 1f);
                Color portalCore = new Color(0.9f, 1.0f, 0.95f, 1f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            float t = dist / radius;
                            Color c;
                            if (t < 0.35f)
                                c = Color.Lerp(portalCore, portalMid, t / 0.35f);
                            else
                                c = Color.Lerp(portalMid, portalOuter, (t - 0.35f) / 0.65f);

                            tex.SetPixel(x, y, c);
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                _exitSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            }
            return _exitSprite;
        }
    }
}
