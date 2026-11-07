using System.Collections.Generic;
using UnityEngine;

namespace MazeGame
{
    public static class DungeonArt
    {
        public static readonly string[] Names = { "青苔遗迹", "幽蓝水牢", "暖金古库", "紫晶密室", "赤烬深层" };
        private static readonly Color[] Stone = { new Color(.25f,.36f,.42f), new Color(.24f,.33f,.48f), new Color(.43f,.36f,.25f), new Color(.35f,.28f,.46f), new Color(.40f,.26f,.28f) };
        private static readonly Color[] Lights = { new Color(.25f,1f,.78f), new Color(.3f,.75f,1f), new Color(1f,.72f,.25f), new Color(.76f,.48f,1f), new Color(1f,.35f,.15f) };
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
        private static int Theme(int level) => Mathf.Clamp(level - 1, 0, 4);
        public static Color LightColor(int level) => Lights[Theme(level)];
        private static Color Shade(Color color, float factor) => new Color(color.r * factor, color.g * factor, color.b * factor, 1);
        private static Sprite Make(int key, System.Func<int, int, Color> pixel, int size = 32)
        {
            if (Cache.TryGetValue(key, out var sprite)) return sprite;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), size);
            Cache[key] = sprite; return sprite;
        }
        public static Sprite Floor(int level, int variant)
        {
            int t = Theme(level); variant %= 4;
            Color stone = Shade(Stone[t], .43f);
            return Make(100 + t * 4 + variant, (x, y) =>
            {
                int row = y / 16, xx = (x + row * 9) % 32;
                if (y % 16 == 0 || xx == 0) return Shade(stone, .40f);
                if (y % 16 == 15 || xx == 1) return Shade(stone, 1.25f);
                float grain = ((x / 3 * 13 + y / 3 * 7 + variant * 11) % 9) * .018f;
                if (variant == 1 && x > 19 && y == 8 + (x - 19) / 3) return Shade(stone,.55f);
                if (variant == 2 && x > 5 && x < 12 && y > 18 && y < 23) return Color.Lerp(stone, Lights[t], .06f);
                return Shade(stone, .9f + grain);
            });
        }
        // Exposed edges receive a cap; south-facing walls show a darker brick face.
        public static Sprite Wall(int level, int mask, int variant)
        {
            int t = Theme(level); variant %= 3;
            Color stone = Stone[t];
            return Make(1000 + t * 48 + mask * 3 + variant, (x,y) =>
            {
                bool south = (mask & 1) != 0, north = (mask & 2) != 0, west = (mask & 4) != 0, east = (mask & 8) != 0;
                if ((west && x < 2) || (east && x > 29) || (south && y == 0)) return Shade(stone,.28f);
                if ((north && y > 29) || (west && x == 2)) return Shade(stone,1.45f);
                if (south && y < 11)
                {
                    int xx = (x + (y / 5) * 9) % 16;
                    if (y % 5 == 0 || xx == 0) return Shade(stone,.26f);
                    return Shade(stone, y % 5 == 4 ? .80f : .56f + (xx % 3) * .025f);
                }
                if (south && y == 11) return Shade(stone,1.5f);
                int bx = (x + y / 8 * 8) % 16;
                if (bx == 0 || y % 8 == 0) return Shade(stone,.50f);
                if (bx == 1 || y % 8 == 7) return Shade(stone,1.12f);
                float grain = ((x / 2 * 7 + y / 2 * 11 + variant * 13) % 7) * .025f;
                return Shade(stone,.85f + grain);
            });
        }
        public static Sprite Lantern(int level)
        {
            int t = Theme(level); var light = Lights[t];
            return Make(2000+t, (x,y) =>
            {
                if (y >= 25 && y <= 29 && x >= 14 && x <= 17) return new Color(.13f,.18f,.22f);
                if (y >= 8 && y <= 24 && x >= 9 && x <= 22)
                {
                    if (x <= 10 || x >= 21 || y <= 10 || y >= 22) return new Color(.12f,.20f,.24f);
                    if (x == 11 || y == 21) return Color.Lerp(light,Color.white,.45f);
                    return x < 17 ? Color.Lerp(light,Color.white,.72f) : light;
                }
                return Color.clear;
            });
        }
        public static Sprite Ornament(int level)
        {
            int t = Theme(level);
            return Make(2100+t,(x,y) =>
            {
                if (t == 0)
                {
                    int stem = 14 + (y / 5 % 2) * 3;
                    if (Mathf.Abs(x-stem)<1 && y>3) return new Color(.12f,.3f,.24f);
                    if (y % 7 < 4 && Mathf.Abs(x - stem - (y/7%2==0 ? 3 : -3)) + Mathf.Abs(y%7-2) < 5) return new Color(.18f,.48f,.35f);
                }
                else if (t == 2)
                {
                    if (x>9 && x<23 && y>7 && y<25) return x<12 || x>20 || y<10 || y>22 ? new Color(.65f,.48f,.22f) : new Color(.25f,.18f,.11f);
                    if (y==26 && x>6 && x<26) return Lights[t];
                }
                else
                {
                    int peak = 25 - Mathf.Abs(x-16)*2;
                    if (x>9 && x<23 && y>5 && y<peak) return Color.Lerp(Lights[t],Color.white,x<16?.35f:0);
                    if (x>21 && x<27 && y>4 && y<18-Mathf.Abs(x-24)*2) return Shade(Lights[t],.65f);
                }
                return Color.clear;
            });
        }
        public static Sprite Glow()
        {
            return Make(2200,(x,y) =>
            {
                float d = Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32;
                return new Color(1,1,1,Mathf.Pow(Mathf.Max(0,1-d),2)*.32f);
            },64);
        }
        public static void Decorate(Transform root, MazeGenerator maze, int level)
        {
            int count = 0;
            for (int x=0; x<maze.Width; x++) for(int y=1; y<maze.Height; y++)
            {
                if (maze.Grid[x,y] != CellType.Wall || maze.Grid[x,y-1] == CellType.Wall) continue;
                bool lamp = (x*7+y*11)%9==0 || (count==0 && x<=3);
                if (lamp)
                {
                    Add(root,"Lantern",new Vector3(x,y-.1f,0),Lantern(level),Vector3.one,2,Color.white);
                    Add(root,"LanternGlow",new Vector3(x,y-.55f,0),Glow(),new Vector3(3.4f,3.4f,1),1,LightColor(level));
                    count++;
                }
                else if ((x*3+y*7)%7==0) Add(root,"WallOrnament",new Vector3(x,y,0),Ornament(level),Vector3.one,1,Color.white);
            }
        }
        private static void Add(Transform root,string name,Vector3 position,Sprite sprite,Vector3 scale,int order,Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(root); go.transform.position=position; go.transform.localScale=scale;
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sortingOrder=order; sr.color=color;
        }
    }
}
