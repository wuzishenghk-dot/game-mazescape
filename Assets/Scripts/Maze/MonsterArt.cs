using UnityEngine;
namespace MazeGame
{
    public static class MonsterArt
    {
        private static readonly Sprite[] Sprites = new Sprite[6];
        public static readonly string[] Names = { "史莱姆", "骷髅守卫", "幽灵", "蝙蝠", "石像守卫", "小恶魔" };
        public static Sprite Get(int kind)
        {
            kind = Mathf.Clamp(kind, 0, 5);
            if (Sprites[kind] != null) return Sprites[kind];
            var atlas = Resources.Load<Texture2D>("Monsters/Monsters");
            if (atlas == null) return SpriteFactory.GetPlayerSprite();
            int w = atlas.width / 3, h = atlas.height / 2, ox = kind % 3 * w, oy = (1-kind/3)*h;
            var pixels = atlas.GetPixels32(); int minX=w,minY=h,maxX=0,maxY=0;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(pixels[(oy+y)*atlas.width+ox+x].a>40)
            {minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            var rect=new Rect(ox+minX,oy+minY,maxX-minX+1,maxY-minY+1);
            Sprites[kind]=Sprite.Create(atlas,rect,new Vector2(.5f,.4f),Mathf.Max(rect.width,rect.height)/.95f);
            return Sprites[kind];
        }
    }
}
