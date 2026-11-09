using System.Collections.Generic;
using UnityEngine;

namespace MazeGame
{
    public class MazeEnemy : MonoBehaviour
    {
        public int Health { get; private set; }
        private float _hitUntil;
        private int _maxHealth;
        private Transform _healthFill;
        private GameObject _healthBar;
        private static Sprite _barSprite;
        private void LateUpdate()
        {
            if(_healthBar==null)return;
            _healthBar.SetActive(Health<_maxHealth||IsChasing||IsFrozen);
            float fraction=(float)Health/_maxHealth;
            _healthFill.localScale=new Vector3(.7f*fraction,.06f,1);
            _healthFill.localPosition=new Vector3(-.35f+.35f*fraction,0,0);
            _healthFill.GetComponent<SpriteRenderer>().color=IsFrozen?Color.cyan:new Color(1,.3f,.2f);
        }
        public bool Hit()
        {
            var game=MazeGameManager.Instance;
            if(game==null||game.CurrentState!=MazeGameManager.GameState.Playing||Health<=0)return false;
            Health--;_hitUntil=Time.time+.25f;
            CombatEffect.Spawn(transform.position,Health<=0?new Color(1,.6f,.2f):Color.white,false,0,Health<=0?1.5f:.8f);
            if(Health<=0){game.EnemyDefeated();gameObject.SetActive(false);Destroy(gameObject);}
            return true;
        }
        public int Kind { get; private set; }
        private float _frozenUntil;
        public bool IsFrozen => Time.time < _frozenUntil;
        public void Freeze(float seconds) { _frozenUntil = Mathf.Max(_frozenUntil, Time.time + seconds); }
        private static readonly float[] PatrolSpeed = { 1.0f, 1.6f, 1.5f, 2.1f, .65f, 1.9f };
        private static readonly float[] ChaseSpeed = { 1.8f, 2.8f, 2.5f, 3.7f, 1.5f, 3.4f };
        private static readonly int[] Detection = { 4, 6, 5, 4, 3, 7 };
        private MazeGenerator _maze;
        private Transform _player;
        private Vector2Int _cell, _next, _previous;
        private SpriteRenderer _renderer;
        public bool IsChasing { get; private set; }
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public void Initialize(MazeGenerator maze, Transform player, Vector2Int cell, int kind = 0)
        {
            Kind = Mathf.Clamp(kind,0,5);
            Health=Kind==4?4:Kind==0||Kind==3?2:3;
            _maxHealth=Health;
            if(_barSprite==null)_barSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),Texture2D.whiteTexture.width);
            _healthBar=new GameObject("HealthBar");_healthBar.transform.SetParent(transform,false);_healthBar.transform.localPosition=new Vector3(0,.7f,0);
            var back=new GameObject("Background");back.transform.SetParent(_healthBar.transform,false);back.transform.localScale=new Vector3(.76f,.1f,1);var br=back.AddComponent<SpriteRenderer>();br.sprite=_barSprite;br.color=new Color(.05f,.05f,.08f);br.sortingOrder=8;
            var fill=new GameObject("RemainingHealth");fill.transform.SetParent(_healthBar.transform,false);_healthFill=fill.transform;var fr=fill.AddComponent<SpriteRenderer>();fr.sprite=_barSprite;fr.sortingOrder=9;
            _healthBar.SetActive(false);
            gameObject.name = MonsterArt.Names[Kind];
            _maze = maze; _player = player; _cell = _next = _previous = cell;
            transform.position = new Vector3(cell.x, cell.y, 0);
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = MonsterArt.Get(Kind);
            _renderer.sortingOrder = 4;
            _renderer.color = Color.white;
        }

        private bool Open(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < _maze.Width && p.y < _maze.Height && _maze.Grid[p.x, p.y] != CellType.Wall;

        // Bounded breadth-first search measures corridor distance, so walls block detection.
        private Vector2Int ChaseStep(Vector2Int target)
        {
            var queue = new Queue<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var depth = new Dictionary<Vector2Int, int>();
            queue.Enqueue(_cell); depth[_cell] = 0;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (p == target)
                {
                    IsChasing = true;
                    while (parent.ContainsKey(p) && parent[p] != _cell) p = parent[p];
                    return p;
                }
                if (depth[p] >= Detection[Kind]) continue;
                foreach (var direction in Directions)
                {
                    var n = p + direction;
                    if (!Open(n) || depth.ContainsKey(n)) continue;
                    depth[n] = depth[p] + 1; parent[n] = p; queue.Enqueue(n);
                }
            }
            IsChasing = false;
            return _cell;
        }

        private void Update()
        {
            var game = MazeGameManager.Instance;
            if (game == null || game.CurrentState != MazeGameManager.GameState.Playing || _player == null) return;
            _renderer.color = IsFrozen ? new Color(.35f,.8f,1) : Color.white;
            if (IsFrozen) return;
            if(Time.time<_hitUntil){_renderer.color=new Color(1,.3f,.3f);return;}
            float pulse=Mathf.Sin(Time.time*(Kind==3?18:7))*.04f;
            transform.localScale=new Vector3(1+pulse,1-pulse,1);
            if (Kind == 2) _renderer.color = new Color(1,1,1,.65f + .35f*Mathf.Abs(Mathf.Sin(Time.time*2)));
            if (Vector2.Distance(transform.position, _player.position) < 0.55f) game.TakeDamage();
            if (game.CurrentState != MazeGameManager.GameState.Playing) return;
            Vector3 destination = new Vector3(_next.x, _next.y, 0);
            _renderer.flipX = destination.x < transform.position.x;
            transform.position = Vector3.MoveTowards(transform.position, destination, (IsChasing ? ChaseSpeed[Kind] : PatrolSpeed[Kind]) * Time.deltaTime);
            if (Vector3.Distance(transform.position, destination) > 0.001f) return;
            if (_cell != _next) { _previous = _cell; _cell = _next; }
            _next = ChaseStep(new Vector2Int(Mathf.RoundToInt(_player.position.x), Mathf.RoundToInt(_player.position.y)));
            if (!IsChasing)
            {
                var options = new List<Vector2Int>();
                foreach (var direction in Directions) if (Open(_cell + direction)) options.Add(_cell + direction);
                if (options.Count > 1) options.Remove(_previous);
                if (options.Count > 0) _next = options[Random.Range(0, options.Count)];
            }

        }
    }
}
