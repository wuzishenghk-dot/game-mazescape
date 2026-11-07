using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MazeGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 4.5f;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Transform _visualTransform;
        private float _bobTimer;

        public void Initialize()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.radius = 0.35f;
            }

            _visualTransform = transform;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            _moveInput = Vector2.zero;
            if (MazeGameManager.Instance == null || MazeGameManager.Instance.CurrentState != MazeGameManager.GameState.Playing) return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    _moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    _moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    _moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    _moveInput.x += 1f;
            }
#else
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");
#endif

            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }

            // Visual breathing / walking bob
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _bobTimer += Time.deltaTime * 12f;
                float scale = 1f + Mathf.Sin(_bobTimer) * 0.06f;
                _visualTransform.localScale = new Vector3(scale, scale, 1f);

                // Rotate slightly toward movement direction
                float targetAngle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg - 90f;
                _visualTransform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
            else
            {
                _visualTransform.localScale = Vector3.Lerp(_visualTransform.localScale, Vector3.one, Time.deltaTime * 10f);
            }
        }

        private void FixedUpdate()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = MazeGameManager.Instance != null && MazeGameManager.Instance.CurrentState == MazeGameManager.GameState.Playing ? _moveInput * moveSpeed : Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var game = MazeGameManager.Instance;
            if (game == null || game.CurrentState != MazeGameManager.GameState.Playing) return;
            var item = other.GetComponent<CollectibleItem>();
            if (item == null || !other.enabled) return;
            if (item.isExit) { game.ReachExit(); return; }
            other.enabled = false;
            if (item.isKey) game.CollectKey();
            else game.CollectCoin();
            Destroy(other.gameObject);
            if (SoundEffects.Instance != null) SoundEffects.Instance.PlayCoinSound();
        }
    }
}
