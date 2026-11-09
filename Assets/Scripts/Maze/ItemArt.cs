using UnityEngine;
namespace MazeGame
{
    public static class ItemArt
    {
        private static readonly Sprite[] Sprites = new Sprite[3];
        public static readonly string[] Names = { "生命药水", "冰冻符石", "探路卷轴" };
        public static Sprite Get(int kind)
        {
            if(Sprites[kind]!=null)return Sprites[kind];
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                Color c=Color.clear;
                if(kind==0){if(x>=12&&x<=19&&y>=22&&y<28)c=new Color(.65f,.42f,.22f);if(Vector2.Distance(new Vector2(x,y),new Vector2(16,14))<9)c=y<18?new Color(.95f,.2f,.35f):new Color(.8f,.95f,1);if(x==12&&y>10&&y<18)c=Color.white;}
                if(kind==1&&Mathf.Abs(x-16)+Mathf.Abs(y-16)<12)c=x<16?new Color(.65f,1,1):new Color(.2f,.55f,1);
                if(kind==2&&x>6&&x<26&&y>6&&y<26){c=y<9||y>23?new Color(.65f,.4f,.18f):new Color(1,.88f,.6f);if(y%4==0&&x>11&&x<22)c=new Color(.35f,.45f,.35f);}
                tex.SetPixel(x,y,c);
            }
            tex.Apply();return Sprites[kind]=Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f),40);
        }
    }
}
