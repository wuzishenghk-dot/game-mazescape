using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace MazeGame
{
    public class TreasureChallenge : MonoBehaviour
    {
        private MazeGenerator _maze;
        private Transform _player;
        private MazeEnemy _guardian;
        private bool _active, _claimed;
        private Vector2Int _center;
        private static Sprite _sprite;
        public void Initialize(MazeGenerator maze,Transform player)
        {
            _maze=maze;_player=player;_center=maze.ChallengeCenter;transform.position=new Vector3(_center.x,_center.y,0);
            if(_sprite==null)
            {
                var t=new Texture2D(32,32,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Point;
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    Color c=Color.clear;if(x>3&&x<29&&y>5&&y<25)c=x<7||x>25||y<9||y>21?new Color(.95f,.65f,.2f):new Color(.35f,.17f,.08f);
                    if(x>13&&x<19&&y>11&&y<19)c=Color.yellow;t.SetPixel(x,y,c);
                }
                t.Apply();_sprite=Sprite.Create(t,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            }
            var sr=gameObject.AddComponent<SpriteRenderer>();sr.sprite=_sprite;sr.sortingOrder=3;
        }
        public bool BeginChallenge()
        {
            if(_active||_claimed||MazeGameManager.Instance.CurrentState!=MazeGameManager.GameState.Playing||Vector2.Distance(_player.position,transform.position)>1.5f)return false;
            _active=true;var go=new GameObject("TreasureGuardian");go.transform.SetParent(transform.parent);
            _guardian=go.AddComponent<MazeEnemy>();_guardian.Initialize(_maze,_player,_center+Vector2Int.one,4);
            CombatEffect.Spawn(go.transform.position,new Color(1,.5f,.15f),false,0,2);return true;
        }
        private void Update()
        {
            if(MazeGameManager.Instance.CurrentState!=MazeGameManager.GameState.Playing)return;
            if(_active&&!_claimed&&(_guardian==null||_guardian.Health<=0))
            {
                _claimed=true;MazeGameManager.Instance.ClaimChallengeReward();GetComponent<SpriteRenderer>().color=new Color(.45f,.45f,.45f);
                CombatEffect.Spawn(transform.position,Color.yellow,false,0,2);
            }
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)BeginChallenge();
#else
            if(Input.GetKeyDown(KeyCode.E))BeginChallenge();
#endif
        }
        private void OnGUI()
        {
            if(_player==null||_claimed||MazeGameManager.Instance.CurrentState!=MazeGameManager.GameState.Playing||Vector2.Distance(_player.position,transform.position)>1.5f)return;
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/960f,Screen.height/640f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale;
            if(GUI.Button(new Rect((width-460)/2,20,460,44),_active?"击败石像守卫开启宝箱 · 可以撤退":"[E] 自愿挑战守卫 · 奖励 5 金币 + 1 道具"))BeginChallenge();GUI.matrix=old;
        }
    }
}
