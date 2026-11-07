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
            Playing,
            LevelCompleted
        }

        [Header("Settings")]
        public int currentLevel = 1;
        public int baseMazeWidth = 15;
        public int baseMazeHeight = 15;

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public int CoinsCollected { get; private set; }
        public int TotalCoins { get; private set; }
        public float ElapsedTime { get; private set; }

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
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;
            }

            bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                restartPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.R))
            {
                restartPressed = true;
            }
#endif

            if (restartPressed)
            {
                StartLevel(currentLevel);
            }
        }

        public void StartLevel(int level)
        {
            currentLevel = level;
            CoinsCollected = 0;
            ElapsedTime = 0f;
            CurrentState = GameState.Playing;

            // Scale maze dimensions with level
            int w = baseMazeWidth + (level - 1) * 4;
            int h = baseMazeHeight + (level - 1) * 4;

            BuildMaze(w, h);
        }

        public void NextLevel()
        {
            StartLevel(currentLevel + 1);
        }

        public void CollectCoin()
        {
            CoinsCollected++;
        }

        public void ReachExit()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.LevelCompleted;
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

            CircleCollider2D exitCol = exit.AddComponent<CircleCollider2D>();
            exitCol.isTrigger = true;
            exitCol.radius = 0.45f;

            CollectibleItem exitAnim = exit.AddComponent<CollectibleItem>();
            exitAnim.isExit = true;

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

            // 5. Setup Camera Follow
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
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

        private void OnGUI()
        {
            InitStyles();

            // HUD Card on Top-Left
            GUILayout.BeginArea(new Rect(20, 20, 260, 160), _cardStyle);
            GUILayout.Label("🎮 迷宫探险 (Maze Escape)", _headerStyle);
            GUILayout.Space(6);
            GUILayout.Label($"第 {currentLevel} 关 (Level {currentLevel})", _labelStyle);
            GUILayout.Label($"💰 金币: {CoinsCollected} / {TotalCoins}", _labelStyle);
            GUILayout.Label($"⏱ 用时: {ElapsedTime:F1}s", _labelStyle);
            GUILayout.Space(4);
            GUIStyle hintStyle = new GUIStyle(_labelStyle) { fontSize = 11, fontStyle = FontStyle.Italic };
            hintStyle.normal.textColor = new Color(0.7f, 0.75f, 0.85f);
            GUILayout.Label("操作: WASD / 方向键移动 | [R] 重置", hintStyle);
            GUILayout.EndArea();

            // Level Complete Modal
            if (CurrentState == GameState.LevelCompleted)
            {
                // Dark background tint
                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float modalWidth = 360f;
                float modalHeight = 260f;
                float x = (Screen.width - modalWidth) * 0.5f;
                float y = (Screen.height - modalHeight) * 0.5f;

                GUILayout.BeginArea(new Rect(x, y, modalWidth, modalHeight), _cardStyle);
                GUILayout.Space(10);
                GUILayout.Label("🎉 恭喜通关！", _winTitleStyle);
                GUILayout.Space(12);

                GUIStyle winInfoStyle = new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
                GUILayout.Label($"本关用时: {ElapsedTime:F1} 秒", winInfoStyle);
                GUILayout.Label($"金币收集: {CoinsCollected} / {TotalCoins}", winInfoStyle);

                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("重玩本关", _btnStyle, GUILayout.Height(42)))
                {
                    StartLevel(currentLevel);
                }
                GUILayout.Space(12);
                if (GUILayout.Button("下一关 ➔", _btnStyle, GUILayout.Height(42)))
                {
                    NextLevel();
                }
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }
        }
    }
}
