using UnityEngine;
namespace MazeGame
{
    public class CombatEffect : MonoBehaviour
    {
        private static Sprite _ring, _slash;
        private SpriteRenderer _renderer;
        private float _age, _duration, _size;
        private Color _color;
        public static void Spawn(Vector3 position, Color color, bool slash = false, float angle = 0, float size = 1.5f)
        {
            var root=GameObject.Find("MazeRoot");
            var go=new GameObject(slash?"AttackSlash":"ItemPulse");if(root!=null)go.transform.SetParent(root.transform);
            go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,0,angle);
            var fx=go.AddComponent<CombatEffect>();fx._duration=slash?.25f:.65f;fx._size=size;fx._color=color;
            fx._renderer=go.AddComponent<SpriteRenderer>();fx._renderer.sprite=Sprite(slash);fx._renderer.sortingOrder=12;fx._renderer.color=color;
            go.transform.localScale=Vector3.one*size;
        }
        private static Sprite Sprite(bool slash)
        {
            if(slash&&_slash!=null)return _slash;if(!slash&&_ring!=null)return _ring;
            var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                var p=new Vector2(x-31.5f,y-31.5f);float d=p.magnitude;
                bool visible=d<30&&d>(slash?22:26)&&(!slash||p.x>0);
                tex.SetPixel(x,y,visible?Color.white:Color.clear);
            }
            tex.Apply();var s=UnityEngine.Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
            if(slash)_slash=s;else _ring=s;return s;
        }
        private void Update()
        {
            _age+=Time.deltaTime;float t=_age/_duration;
            transform.localScale=Vector3.one*_size*(.7f+t*.6f);
            _renderer.color=new Color(_color.r,_color.g,_color.b,1-t);
            if(t>=1)Destroy(gameObject);
        }
    }
}
