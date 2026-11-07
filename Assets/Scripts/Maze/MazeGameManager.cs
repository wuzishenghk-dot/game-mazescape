using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MazeGame
{
    public class MazeGameManager : MonoBehaviour
    {
        public static MazeGameManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartGame()
        {
            if (FindFirstObjectByType<MazeGameManager>() == null)
            {
                GameObject go = new GameObject("MazeGameManager");
                go.AddComponent<MazeGameManager>();
            }
        }

#if UNITY_EDITOR
        [MenuItem("Maze Game/Setup Game in Scene")]
        public static void SetupInScene()
        {
            if (FindFirstObjectByType<MazeGameManager>() == null)
            {
                GameObject go = new GameObject("MazeGameManager");
                go.AddComponent<MazeGameManager>();
                Selection.activeGameObject = go;
                Undo.RegisterCreatedObjectUndo(go, "Create MazeGameManager");
            }
        }
#endif

        public enum GameState
        {
            MainMenu,
            Paused,
            Playing,
            Failed,
            LevelCompleted
        }

        [Header("Settings")]
        public int currentLevel = 1;
        public int baseMazeWidth = 15;
        public int baseMazeHeight = 15;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public int CoinsCollected { get; private set; }
        public int TotalCoins { get; private set; }
        public float ElapsedTime { get; private set; }

        public int Health { get; private set; } = 3;
        private float _invulnerableUntil;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public bool TakeDamage()
        {
            if (CurrentState != GameState.Playing || IsInvulnerable) return false;
            Health--;
            _invulnerableUntil = Time.time + 1.5f;
            _notice = "受伤了！利用冲刺拉开距离";
            _noticeUntil = Time.unscaledTime + 2f;
            if (Health <= 0) { CurrentState = GameState.Failed; Time.timeScale = 0f; }
            return true;
        }
        public bool HasKey { get; private set; }
        private SpriteRenderer _exitRenderer;
        private string _notice = "";
        private float _noticeUntil;
        private GameObject _mazeRoot;
        private GameObject _player;
        private MazeGenerator _generator;
        private GUIStyle _cardStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _winTitleStyle;
        private bool _stylesInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure SoundEffects is present
            if (FindFirstObjectByType<SoundEffects>() == null)
            {
                GameObject sfxObj = new GameObject("SoundEffects");
                sfxObj.AddComponent<SoundEffects>();
            }
        }

        private void Start()
        {
            StartLevel(currentLevel);
            ShowMainMenu();
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;
            }

            bool restartPressed = false;
            bool pausePressed = false;
#if ENABLE_INPUT_SYSTEM
            pausePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                restartPressed = true;
            }
#else
            pausePressed = Input.GetKeyDown(KeyCode.Escape);
            if (Input.GetKeyDown(KeyCode.R))
            {
                restartPressed = true;
            }
#endif

            if (pausePressed) TogglePause();
            if (restartPressed && CurrentState == GameState.Playing)
            {
                StartLevel(currentLevel);
            }
        }

        public void StartLevel(int level)
        {
            Time.timeScale = 1f;
            HasKey = false;
            Health = 3;
            _invulnerableUntil = 0;
            _notice = "";
            currentLevel = Mathf.Clamp(level, 1, 5);
            CoinsCollected = 0;
            ElapsedTime = 0f;
            CurrentState = GameState.Playing;

            // Scale maze dimensions with level
            int w = baseMazeWidth + (currentLevel - 1) * 2;
            int h = baseMazeHeight + (currentLevel - 1) * 2;

            BuildMaze(w, h);
        }

        public void NextLevel()
        {
            StartLevel(currentLevel + 1);
        }

        public void CollectCoin()
        {
            if (CurrentState == GameState.Playing) CoinsCollected++;
        }

        public void ReachExit()
        {
            if (CurrentState == GameState.Playing)
            {
                if (!HasKey)
                {
                    _notice = "出口尚未开启，先寻找金色钥匙";
                    _noticeUntil = Time.unscaledTime + 3f;
                    return;
                }
                CurrentState = GameState.LevelCompleted;
                Time.timeScale = 0f;
                if (SoundEffects.Instance != null)
                {
                    SoundEffects.Instance.PlayWinSound();
                }
            }
        }

        private void BuildMaze(int width, int height)
        {
            if (_mazeRoot != null)
            {
                _mazeRoot.SetActive(false);
                Destroy(_mazeRoot);
            }

            _mazeRoot = new GameObject("MazeRoot");
            _generator = new MazeGenerator(width, height);
            _generator.Generate();

            Sprite wallSprite = SpriteFactory.GetWallSprite();
            Sprite floorSprite = SpriteFactory.GetFloorSprite();
            Sprite coinSprite = SpriteFactory.GetCoinSprite();
            Sprite exitSprite = SpriteFactory.GetExitSprite();

            // 1. Build Walls and Floor
            for (int x = 0; x < _generator.Width; x++)
            {
                for (int y = 0; y < _generator.Height; y++)
                {
                    CellType cell = _generator.Grid[x, y];
                    Vector3 pos = new Vector3(x, y, 0);

                    if (cell == CellType.Wall)
                    {
                        GameObject wall = new GameObject($"Wall_{x}_{y}");
                        wall.transform.SetParent(_mazeRoot.transform);
                        wall.transform.position = pos;

                        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
                        sr.sprite = wallSprite;
                        sr.sortingOrder = 0;

                        BoxCollider2D bc = wall.AddComponent<BoxCollider2D>();
                        bc.size = Vector2.one;
                    }
                    else
                    {
                        GameObject floor = new GameObject($"Floor_{x}_{y}");
                        floor.transform.SetParent(_mazeRoot.transform);
                        floor.transform.position = pos;

                        SpriteRenderer sr = floor.AddComponent<SpriteRenderer>();
                        sr.sprite = floorSprite;
                        sr.sortingOrder = -2;
                    }
                }
            }

            // 2. Build Coins
            TotalCoins = _generator.CoinPositions.Count;
            for (int i = 0; i < _generator.CoinPositions.Count; i++)
            {
                CellPosition cp = _generator.CoinPositions[i];
                Vector3 pos = new Vector3(cp.x, cp.y, 0);

                GameObject coin = new GameObject($"Coin_{i}");
                coin.transform.SetParent(_mazeRoot.transform);
                coin.transform.position = pos;
                coin.tag = "Untagged";

                SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
                sr.sprite = coinSprite;
                sr.sortingOrder = 2;

                CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.3f;

                CollectibleItem anim = coin.AddComponent<CollectibleItem>();
                anim.isExit = false;
            }

            // 3. Build Exit Portal
            Vector3 exitPos = new Vector3(_generator.ExitPos.x, _generator.ExitPos.y, 0);
            GameObject exit = new GameObject("ExitPortal");
            exit.transform.SetParent(_mazeRoot.transform);
            exit.transform.position = exitPos;

            SpriteRenderer exitSr = exit.AddComponent<SpriteRenderer>();
            exitSr.sprite = exitSprite;
            exitSr.sortingOrder = 1;
            _exitRenderer = exitSr;
            exitSr.color = new Color(0.45f, 0.45f, 0.55f);

            CircleCollider2D exitCol = exit.AddComponent<CircleCollider2D>();
            exitCol.isTrigger = true;
            exitCol.radius = 0.45f;

            CollectibleItem exitAnim = exit.AddComponent<CollectibleItem>();
            exitAnim.isExit = true;

            // Choose a reachable floor far from the entrance, avoiding coins and exit.
            CellPosition keyPos = new CellPosition(1, 2);
            int farthest = -1;
            for (int x = 1; x < _generator.Width - 1; x++)
                for (int y = 1; y < _generator.Height - 1; y++)
                    if (_generator.Grid[x, y] == CellType.Floor && x + y > farthest)
                    { farthest = x + y; keyPos = new CellPosition(x, y); }
            GameObject key = new GameObject("Key");
            key.transform.SetParent(_mazeRoot.transform);
            key.transform.position = new Vector3(keyPos.x, keyPos.y, 0);
            var keySr = key.AddComponent<SpriteRenderer>();
            keySr.sprite = SpriteFactory.GetKeySprite();
            keySr.sortingOrder = 3;
            var keyCol = key.AddComponent<CircleCollider2D>();
            keyCol.radius = 0.35f;
            keyCol.isTrigger = true;
            key.AddComponent<CollectibleItem>().isKey = true;

            // 4. Setup or reposition Player
            Vector3 startPos = new Vector3(_generator.StartPos.x, _generator.StartPos.y, 0);
            if (_player == null)
            {
                _player = new GameObject("Player");
                _player.transform.position = startPos;

                SpriteRenderer playerSr = _player.AddComponent<SpriteRenderer>();
                playerSr.sprite = SpriteFactory.GetPlayerSprite();
                playerSr.sortingOrder = 5;

                _player.AddComponent<Rigidbody2D>();
                _player.AddComponent<CircleCollider2D>();

                PlayerController controller = _player.AddComponent<PlayerController>();
                controller.Initialize();
            }
            else
            {
                _player.transform.position = startPos;
                Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            _player.GetComponent<PlayerController>().ResetMovement();
            GameObject fog = new GameObject("ExplorationFog");
            fog.transform.SetParent(_mazeRoot.transform);
            fog.AddComponent<ExplorationFog>().Initialize(_generator.Width, _generator.Height, _player.transform);

            // Introduce patrols only after the two introductory levels.
            if (currentLevel >= 3)
            {
                var candidates = new List<Vector2Int>();
                for (int x = 1; x < _generator.Width - 1; x++)
                    for (int y = 1; y < _generator.Height - 1; y++)
                        if (_generator.Grid[x, y] == CellType.Floor && x + y >= 12 && (x != keyPos.x || y != keyPos.y)) candidates.Add(new Vector2Int(x, y));
                for (int i = 0; i < currentLevel - 2 && candidates.Count > 0; i++)
                {
                    int index = Random.Range(0, candidates.Count);
                    var cell = candidates[index]; candidates.RemoveAt(index);
                    var enemy = new GameObject("Patrol_" + i);
                    enemy.transform.SetParent(_mazeRoot.transform);
                    enemy.AddComponent<MazeEnemy>().Initialize(_generator, _player.transform, cell);
                }
            }

            // 5. Setup Camera Follow
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
                CameraFollow follow = mainCam.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = mainCam.gameObject.AddComponent<CameraFollow>();
                }
                follow.target = _player.transform;
                follow.SetBounds(0, 0, _generator.Width, _generator.Height);
                mainCam.transform.position = new Vector3(startPos.x, startPos.y, -10f);
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            Texture2D cardTex = new Texture2D(1, 1);
            cardTex.SetPixel(0, 0, new Color(0.08f, 0.10f, 0.16f, 0.88f));
            cardTex.Apply();

            Texture2D btnTex = new Texture2D(1, 1);
            btnTex.SetPixel(0, 0, new Color(0.20f, 0.60f, 0.45f, 0.95f));
            btnTex.Apply();

            Texture2D btnHoverTex = new Texture2D(1, 1);
            btnHoverTex.SetPixel(0, 0, new Color(0.25f, 0.75f, 0.55f, 1f));
            btnHoverTex.Apply();

            _cardStyle = new GUIStyle(GUI.skin.box);
            _cardStyle.normal.background = cardTex;
            _cardStyle.padding = new RectOffset(16, 16, 12, 12);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 18;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(0.2f, 0.85f, 0.75f);

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 14;
            _labelStyle.normal.textColor = Color.white;

            _winTitleStyle = new GUIStyle(GUI.skin.label);
            _winTitleStyle.fontSize = 26;
            _winTitleStyle.fontStyle = FontStyle.Bold;
            _winTitleStyle.alignment = TextAnchor.MiddleCenter;
            _winTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.2f);

            _btnStyle = new GUIStyle(GUI.skin.button);
            _btnStyle.normal.background = btnTex;
            _btnStyle.hover.background = btnHoverTex;
            _btnStyle.fontSize = 16;
            _btnStyle.fontStyle = FontStyle.Bold;
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.hover.textColor = Color.white;
            _btnStyle.alignment = TextAnchor.MiddleCenter;

            _stylesInitialized = true;
        }

        public void CollectKey()
        {
            if (CurrentState != GameState.Playing) return;
            HasKey = true;
            if (_exitRenderer != null) _exitRenderer.color = Color.white;
            _notice = "钥匙已找到！前往绿色出口";
            _noticeUntil = Time.unscaledTime + 3f;
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing) { CurrentState = GameState.Paused; Time.timeScale = 0f; }
            else if (CurrentState == GameState.Paused) { CurrentState = GameState.Playing; Time.timeScale = 1f; }
        }

        public void ShowMainMenu() { CurrentState = GameState.MainMenu; Time.timeScale = 0f; }
        private void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1f; } }

        private void OnGUI()
        {
            InitStyles();
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 640f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            if (CurrentState != GameState.MainMenu)
            {
                GUILayout.BeginArea(new Rect(20, 20, 350, 250), _cardStyle);
                GUILayout.Label($"迷宫探险  /  第 {currentLevel} 关", _headerStyle);
                GUILayout.Label($"金币  {CoinsCollected} / {TotalCoins}     用时  {ElapsedTime:F1} 秒", _labelStyle);
                GUILayout.Label($"生命  {Health} / 3" + (currentLevel >= 3 ? "    小心巡逻怪" : "    安全探索"), _labelStyle);
                GUILayout.Label(HasKey ? "目标：前往绿色出口" : "目标：找到金色钥匙，开启出口", _labelStyle);
                GUILayout.Label("WASD / 方向键移动 · R 重新生成", _labelStyle);
                var controller = _player != null ? _player.GetComponent<PlayerController>() : null;
                GUILayout.Label(controller != null && controller.DashCooldownRemaining > 0 ? $"冲刺冷却  {controller.DashCooldownRemaining:F1} 秒" : "空格冲刺 · 已就绪", _labelStyle);
                if (GUILayout.Button("暂停 / 继续 · Esc", _btnStyle, GUILayout.Height(34))) TogglePause();
                GUILayout.EndArea();
            }
            if (Time.unscaledTime < _noticeUntil && CurrentState == GameState.Playing)
                GUI.Box(new Rect((width - 420) / 2, height - 70, 420, 45), _notice, _cardStyle);
            if (CurrentState != GameState.Playing)
            {
                GUI.color = new Color(0, 0, 0, 0.65f);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUILayout.BeginArea(new Rect((width - 440) / 2, (height - 360) / 2, 440, 360), _cardStyle);
                GUILayout.Space(16);
                string title = CurrentState == GameState.MainMenu ? "迷宫探险" : CurrentState == GameState.Paused ? "休息一下" : CurrentState == GameState.Failed ? "这次没能逃出去" : "成功逃出迷宫";
                GUILayout.Label(title, _winTitleStyle);
                GUILayout.Space(20);
                if (CurrentState == GameState.MainMenu)
                {
                    GUILayout.Label("拨开迷雾，探索岔路，收集金币。", _labelStyle);
                    GUILayout.Label("找到金色钥匙，让出口重新亮起。", _labelStyle);
                    GUILayout.Label("WASD 移动 · 空格冲刺 · Esc 暂停", _labelStyle);
                    GUILayout.Space(20);
                    if (GUILayout.Button("开始探险", _btnStyle, GUILayout.Height(48))) StartLevel(1);
                }
                else if (CurrentState == GameState.Paused)
                {
                    if (GUILayout.Button("继续探险", _btnStyle, GUILayout.Height(46))) TogglePause();
                    GUILayout.Space(10);
                    if (GUILayout.Button("重新生成本关", _btnStyle, GUILayout.Height(46))) StartLevel(currentLevel);
                }
                else if (CurrentState == GameState.Failed)
                {
                    GUILayout.Label("注意红色巡逻怪，冲刺可以帮助脱险。", _labelStyle);
                    GUILayout.Label("受伤后有 1.5 秒保护时间。", _labelStyle);
                    GUILayout.Space(20);
                    if (GUILayout.Button("重新挑战", _btnStyle, GUILayout.Height(46))) StartLevel(currentLevel);
                }
                else
                {
                    GUILayout.Label($"用时 {ElapsedTime:F1} 秒    金币 {CoinsCollected} / {TotalCoins}", _labelStyle);
                    GUILayout.Label(CoinsCollected == TotalCoins ? "全部金币已收集！" : "下次试着探索更多岔路", _labelStyle);
                    GUILayout.Space(18);
                    if (GUILayout.Button(currentLevel < 5 ? "进入下一关" : "完成五关 · 再次挑战", _btnStyle, GUILayout.Height(46))) StartLevel(currentLevel < 5 ? currentLevel + 1 : 1);
                    if (GUILayout.Button("重玩本关", _btnStyle, GUILayout.Height(40))) StartLevel(currentLevel);
                }
                if (CurrentState != GameState.MainMenu)
                {
                    GUILayout.Space(10);
                    if (GUILayout.Button("返回首页", _btnStyle, GUILayout.Height(40))) ShowMainMenu();
                }
                GUILayout.EndArea();
            }
            if (IsInvulnerable && CurrentState == GameState.Playing)
            {
                GUI.color = new Color(1, 0.12f, 0.1f, 0.12f);
                GUI.DrawTexture(new Rect(0, 0, width, 8), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, height - 8, width, 8), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, 0, 8, height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(width - 8, 0, 8, height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.matrix = previous;
        }
    }
}
