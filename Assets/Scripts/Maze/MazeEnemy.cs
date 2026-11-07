using System.Collections.Generic;
using UnityEngine;

namespace MazeGame
{
    public class MazeEnemy : MonoBehaviour
    {
        private MazeGenerator _maze;
        private Transform _player;
        private Vector2Int _cell, _next, _previous;
        private SpriteRenderer _renderer;
        public bool IsChasing { get; private set; }
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public void Initialize(MazeGenerator maze, Transform player, Vector2Int cell)
        {
            _maze = maze; _player = player; _cell = _next = _previous = cell;
            transform.position = new Vector3(cell.x, cell.y, 0);
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = SpriteFactory.GetPlayerSprite();
            _renderer.sortingOrder = 4;
            _renderer.color = new Color(1f, 0.35f, 0.4f);
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
                if (depth[p] >= 6) continue;
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
            if (Vector2.Distance(transform.position, _player.position) < 0.65f) game.TakeDamage();
            if (game.CurrentState != MazeGameManager.GameState.Playing) return;
            Vector3 destination = new Vector3(_next.x, _next.y, 0);
            transform.position = Vector3.MoveTowards(transform.position, destination, (IsChasing ? 3.1f : 1.6f) * Time.deltaTime);
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
            _renderer.color = IsChasing ? new Color(1f, 0.12f, 0.15f) : new Color(1f, 0.5f, 0.6f);
        }
    }
}
