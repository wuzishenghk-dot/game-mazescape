using UnityEngine;

namespace MazeGame
{
    public class CharacterVisual : MonoBehaviour
    {
        public static readonly string[] Names = { "林野 · 探险者", "艾琳 · 寻路者", "洛恩 · 游侠", "小葵 · 侦察员" };
        private static readonly Sprite[,] Sprites = new Sprite[4, 4];
        private static Texture2D _atlas;
        private SpriteRenderer _renderer;
        private int _character, _direction;
        private float _step;
        public static Sprite GetSprite(int character, int direction)
        {
            character = Mathf.Clamp(character, 0, 3); direction = Mathf.Clamp(direction, 0, 3);
            if (Sprites[character, direction] != null) return Sprites[character, direction];
            if (_atlas == null) _atlas = Resources.Load<Texture2D>("Characters/Adventurers");
            if (_atlas == null) return null;
            int w = _atlas.width / 4, h = _atlas.height / 4;
            int ox = character * w, oy = (3 - direction) * h;
            int minX = w, minY = h, maxX = 0, maxY = 0;
            var pixels = _atlas.GetPixels32();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                if (pixels[(oy + y) * _atlas.width + ox + x].a > 40)
                { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
            var rect = new Rect(ox + minX, oy + minY, maxX - minX + 1, maxY - minY + 1);
            Sprites[character, direction] = Sprite.Create(_atlas, rect, new Vector2(0.5f, 0.25f), rect.height / 1.05f);
            return Sprites[character, direction];
        }

        public void Select(int character)
        {
            _character = Mathf.Clamp(character, 0, 3); _direction = 0;
            if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 5;
            _renderer.sprite = GetSprite(_character, _direction);
        }

        public void Animate(Vector2 movement, bool dash)
        {
            if (movement.sqrMagnitude > 0.01f)
            {
                _direction = Mathf.Abs(movement.x) > Mathf.Abs(movement.y) ? (movement.x < 0 ? 1 : 2) : (movement.y > 0 ? 3 : 0);
                _step += Time.deltaTime * (dash ? 24f : 13f);
            }
            float bob = movement.sqrMagnitude > 0.01f ? Mathf.Abs(Mathf.Sin(_step)) * 0.045f : 0;
            transform.localPosition = new Vector3(0, bob, 0);
            transform.localRotation = Quaternion.Euler(0, 0, movement.sqrMagnitude > 0.01f ? Mathf.Sin(_step) * 3f : 0);
            _renderer.sprite = GetSprite(_character, _direction);
        }

        private void LateUpdate()
        {
            var game = MazeGameManager.Instance;
            if (_renderer != null) _renderer.color = game != null && game.IsInvulnerable && Mathf.Sin(Time.time * 24) > 0 ? new Color(1, 1, 1, 0.35f) : Color.white;
        }
    }
}
