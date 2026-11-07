using UnityEngine;

namespace MazeGame
{
    // One texture for the whole maze; explored cells remain faintly visible.
    public class ExplorationFog : MonoBehaviour
    {
        private Texture2D _texture;
        private Sprite _sprite;
        private Color32[] _pixels;
        private bool[] _explored;
        private Transform _target;
        private int _width, _height;
        private Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);
        public int ExploredCount { get; private set; }

        public void Initialize(int width, int height, Transform target)
        {
            _width = width; _height = height; _target = target;
            _pixels = new Color32[width * height];
            _explored = new bool[_pixels.Length];
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _sprite = Sprite.Create(_texture, new Rect(0, 0, width, height), Vector2.zero, 1f);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sortingOrder = 20;
            transform.position = new Vector3(-0.5f, -0.5f, 0);
            Reveal();
        }

        private void LateUpdate()
        {
            if (MazeGameManager.Instance != null && MazeGameManager.Instance.CurrentState == MazeGameManager.GameState.Playing) Reveal();
        }

        public void Reveal()
        {
            if (_target == null) return;
            var cell = new Vector2Int(Mathf.RoundToInt(_target.position.x), Mathf.RoundToInt(_target.position.y));
            if (cell == _lastCell) return;
            _lastCell = cell;
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    int i = y * _width + x;
                    bool visible = (x - cell.x) * (x - cell.x) + (y - cell.y) * (y - cell.y) <= 16;
                    if (visible && !_explored[i]) { _explored[i] = true; ExploredCount++; }
                    _pixels[i] = new Color32(5, 8, 16, visible ? (byte)0 : _explored[i] ? (byte)145 : (byte)255);
                }
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void OnDestroy()
        {
            if (_sprite != null) Destroy(_sprite);
            if (_texture != null) Destroy(_texture);
        }
    }
}
