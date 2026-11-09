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

        public float AttackCooldownRemaining { get; private set; }
        private Vector2 _facing = Vector2.down;
        public bool TryAttack()
        {
            var game=MazeGameManager.Instance;
            if(game==null||game.CurrentState!=MazeGameManager.GameState.Playing||AttackCooldownRemaining>0)return false;
            AttackCooldownRemaining=.45f;
            CombatEffect.Spawn(transform.position+(Vector3)_facing*.25f,new Color(1,.9f,.5f),true,Mathf.Atan2(_facing.y,_facing.x)*Mathf.Rad2Deg,2f);
            var visual=GetComponentInChildren<CharacterVisual>();if(visual!=null)visual.Attack();
            int hits=0;
            foreach(var enemy in FindObjectsByType<MazeEnemy>(FindObjectsSortMode.None))
            {
                Vector2 offset=enemy.transform.position-transform.position;
                if(offset.magnitude<=1.35f && (offset.sqrMagnitude<.1f||Vector2.Dot(_facing,offset.normalized)>.15f) && game.HasClearPath(transform.position,enemy.transform.position))
                {if(enemy.Hit())hits++;}
            }
            game.CombatNotice(hits>0?"命中！": "挥击 · 未命中");
            return true;
        }
        public float DashCooldownRemaining { get; private set; }
        private float _dashRemaining;
        private Vector2 _dashDirection;
        public bool IsDashing => _dashRemaining > 0f;
        public bool TryDash(Vector2 direction)
        {
            if (MazeGameManager.Instance == null || MazeGameManager.Instance.CurrentState != MazeGameManager.GameState.Playing || DashCooldownRemaining > 0 || direction.sqrMagnitude < 0.01f) return false;
            _dashDirection = direction.normalized;
            _dashRemaining = 0.18f;
            DashCooldownRemaining = 2.5f;
            return true;
        }
        public void ResetMovement()
        {
            _moveInput = Vector2.zero;
            AttackCooldownRemaining=0;
            _facing=Vector2.down;
            _dashRemaining = DashCooldownRemaining = 0;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            transform.localScale = Vector3.one;
        }
        private Rigidbody2D _rb;
        private Vector2 _moveInput;


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

            if(_moveInput.sqrMagnitude>.01f)_facing=_moveInput.normalized;
            AttackCooldownRemaining=Mathf.Max(0,AttackCooldownRemaining-Time.deltaTime);
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current!=null&&Keyboard.current.jKey.wasPressedThisFrame)TryAttack();
#else
            if(Input.GetKeyDown(KeyCode.J))TryAttack();
#endif
            DashCooldownRemaining = Mathf.Max(0, DashCooldownRemaining - Time.deltaTime);
            _dashRemaining = Mathf.Max(0, _dashRemaining - Time.deltaTime);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TryDash(_moveInput);
#else
            if (Input.GetKeyDown(KeyCode.Space)) TryDash(_moveInput);
#endif
            var visual = GetComponentInChildren<CharacterVisual>();
            if (visual != null) visual.Animate(IsDashing ? _dashDirection : _moveInput, IsDashing);
        }

        private void FixedUpdate()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = MazeGameManager.Instance != null && MazeGameManager.Instance.CurrentState == MazeGameManager.GameState.Playing ? (IsDashing ? _dashDirection * moveSpeed * 2.4f : _moveInput * moveSpeed) : Vector2.zero;
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
            if (item.itemKind >= 0) game.CollectItem(item.itemKind);
            else if (item.isKey) game.CollectKey();
            else game.CollectCoin();
            Destroy(other.gameObject);
            if (SoundEffects.Instance != null) SoundEffects.Instance.PlayCoinSound();
        }
    }
}
