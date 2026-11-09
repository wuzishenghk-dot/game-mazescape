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
            if(_player!=null)CombatEffect.Spawn(_player.transform.position,Color.red,false,0,1.2f);
            _invulnerableUntil = Time.time + 1.5f;
            _notice = "受伤了！利用冲刺拉开距离";
            _noticeUntil = Time.unscaledTime + 2f;
            if (Health <= 0) { CurrentState = GameState.Failed; Time.timeScale = 0f; }
            return true;
        }
        public int SelectedCharacter { get; private set; }
        public void SelectCharacter(int index)
        {
            SelectedCharacter = Mathf.Clamp(index, 0, 3);
            PlayerPrefs.SetInt("Maze.Character", SelectedCharacter);
            if (_player != null) _player.GetComponentInChildren<CharacterVisual>().Select(SelectedCharacter);
        }
        public int DefeatedEnemies { get; private set; }
        public void EnemyDefeated(){DefeatedEnemies++;Notify("怪物已击败！");}
        public void CombatNotice(string text){if(_noticeUntil<Time.unscaledTime || (text.StartsWith("命中")&&_notice!="怪物已击败！"))Notify(text);}
        public bool HasClearPath(Vector3 from,Vector3 to)
        {
            int steps=Mathf.CeilToInt(Vector3.Distance(from,to)*12);
            for(int i=0;i<=steps;i++){var p=Vector3.Lerp(from,to,steps==0?0:(float)i/steps);int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);if(x<0||y<0||x>=_generator.Width||y>=_generator.Height||_generator.Grid[x,y]==CellType.Wall)return false;}
            return true;
        }
        public int[] Inventory { get; } = new int[3];
        private Vector2Int _keyCell;
        private GameObject _route;
        public void CollectItem(int kind)
        {
            if(CurrentState != GameState.Playing || kind<0 || kind>2)return;
            Inventory[kind]++; Notify("获得"+ItemArt.Names[kind]+" · 按 "+(kind+1)+" 使用");
        }
        private void Notify(string message){_notice=message;_noticeUntil=Time.unscaledTime+3;}
        public bool UseItem(int kind)
        {
            if(CurrentState!=GameState.Playing || kind<0 || kind>2 || Inventory[kind]<=0){if(CurrentState==GameState.Playing&&kind>=0&&kind<3)Notify(ItemArt.Names[kind]+"数量不足");return false;}
            if(kind==0)
            {
                if(Health>=3){Notify("生命已满，药水已保留");return false;}
                Health++; CombatEffect.Spawn(_player.transform.position,Color.green,false,0,1.8f); Notify("药水已使用 · 恢复 1 点生命");
            }
            else if(kind==1)
            {
                int frozen=0;
                foreach(var enemy in FindObjectsByType<MazeEnemy>(FindObjectsSortMode.None))
                    if(Vector2.Distance(enemy.transform.position,_player.transform.position)<=5f){enemy.Freeze(3);frozen++;}
                if(frozen==0){Notify("附近没有怪物，符石已保留");return false;}
                CombatEffect.Spawn(_player.transform.position,Color.cyan,false,0,5f);
                Notify("符石已使用 · 附近怪物冻结 3 秒");
            }
            else
            {
                if(!ShowRoute()){Notify("当前没有可显示的路线");return false;}
                CombatEffect.Spawn(_player.transform.position,new Color(1,.85f,.3f),false,0,2);
                Notify("卷轴已使用 · 引导路线持续 8 秒");
            }
            Inventory[kind]--;return true;
        }
        private bool ShowRoute()
        {
            var start=new Vector2Int(Mathf.RoundToInt(_player.transform.position.x),Mathf.RoundToInt(_player.transform.position.y));
            var goal=HasKey?new Vector2Int(_generator.ExitPos.x,_generator.ExitPos.y):_keyCell;
            var queue=new Queue<Vector2Int>();var parents=new Dictionary<Vector2Int,Vector2Int>();
            queue.Enqueue(start);parents[start]=start;
            var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();if(cell==goal)break;
                foreach(var d in directions){var n=cell+d;if(n.x<0||n.y<0||n.x>=_generator.Width||n.y>=_generator.Height||_generator.Grid[n.x,n.y]==CellType.Wall||parents.ContainsKey(n))continue;parents[n]=cell;queue.Enqueue(n);}
            }
            if(!parents.ContainsKey(goal))return false;
            if(_route!=null){_route.SetActive(false);Destroy(_route);}
            _route=new GameObject("GuideRoute");_route.transform.SetParent(_mazeRoot.transform);_route.AddComponent<TimedRoute>();
            for(var cell=goal;cell!=start;cell=parents[cell])
            {
                var dot=new GameObject("RouteDot");dot.transform.SetParent(_route.transform);dot.transform.position=new Vector3(cell.x,cell.y,0);dot.transform.localScale=Vector3.one*.18f;
                var sr=dot.AddComponent<SpriteRenderer>();sr.sprite=ItemArt.Get(1);sr.sortingOrder=21;sr.color=new Color(.4f,1f,1f,.85f);
            }
            return true;
        }
        private void SpawnSupplies()
        {
            var cells=new List<Vector2Int>();
            for(int x=1;x<_generator.Width-1;x++)for(int y=1;y<_generator.Height-1;y++)
                if(_generator.Grid[x,y]==CellType.Floor && (x!=_keyCell.x||y!=_keyCell.y))cells.Add(new Vector2Int(x,y));
            for(int i=0;i<6&&cells.Count>0;i++)
            {
                int index=Random.Range(0,cells.Count);var cell=cells[index];cells.RemoveAt(index);int kind=i%3;
                var item=new GameObject(ItemArt.Names[kind]);item.transform.SetParent(_mazeRoot.transform);item.transform.position=new Vector3(cell.x,cell.y,0);
                var sr=item.AddComponent<SpriteRenderer>();sr.sprite=ItemArt.Get(kind);sr.sortingOrder=3;
                var col=item.AddComponent<CircleCollider2D>();col.radius=.3f;col.isTrigger=true;item.AddComponent<CollectibleItem>().itemKind=kind;
            }
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
            SelectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt("Maze.Character", 0), 0, 3);

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
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current!=null){if(Keyboard.current.digit1Key.wasPressedThisFrame)UseItem(0);if(Keyboard.current.digit2Key.wasPressedThisFrame)UseItem(1);if(Keyboard.current.digit3Key.wasPressedThisFrame)UseItem(2);}
#else
            if(Input.GetKeyDown(KeyCode.Alpha1))UseItem(0);if(Input.GetKeyDown(KeyCode.Alpha2))UseItem(1);if(Input.GetKeyDown(KeyCode.Alpha3))UseItem(2);
#endif
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
            DefeatedEnemies=0;
            System.Array.Clear(Inventory,0,Inventory.Length);
            _invulnerableUntil = 0;
            _notice = "";
            _noticeUntil = 0;
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
                        int mask = 0;
                        if (y > 0 && _generator.Grid[x,y-1] != CellType.Wall) mask |= 1;
                        if (y < _generator.Height-1 && _generator.Grid[x,y+1] != CellType.Wall) mask |= 2;
                        if (x > 0 && _generator.Grid[x-1,y] != CellType.Wall) mask |= 4;
                        if (x < _generator.Width-1 && _generator.Grid[x+1,y] != CellType.Wall) mask |= 8;
                        sr.sprite = DungeonArt.Wall(currentLevel, mask, (x*7+y*3)%3);
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
                        sr.sprite = DungeonArt.Floor(currentLevel, (x*7+y*11)%4);
                        sr.sortingOrder = -2;
                    }
                }
            }

            DungeonArt.Decorate(_mazeRoot.transform, _generator, currentLevel);

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
            _keyCell = new Vector2Int(keyPos.x,keyPos.y);
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
                playerSr.enabled = false;
                var visual = new GameObject("CharacterVisual");
                visual.transform.SetParent(_player.transform, false);
                visual.AddComponent<CharacterVisual>().Select(SelectedCharacter);
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
                for (int i = 0; i < (currentLevel == 3 ? 2 : currentLevel - 2) && candidates.Count > 0; i++)
                {
                    int index = Random.Range(0, candidates.Count);
                    var cell = candidates[index]; candidates.RemoveAt(index);
                    var enemy = new GameObject("Patrol_" + i);
                    enemy.transform.SetParent(_mazeRoot.transform);
                    int[][] kinds={new int[0],new int[0],new[]{0,1},new[]{2,3},new[]{4,5,1}};
                    enemy.AddComponent<MazeEnemy>().Initialize(_generator, _player.transform, cell, kinds[currentLevel-1][i%kinds[currentLevel-1].Length]);
                }
            }

            SpawnSupplies();

            // 5. Setup Camera Follow
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
                mainCam.orthographicSize = 4.5f;
                CameraFollow follow = mainCam.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = mainCam.gameObject.AddComponent<CameraFollow>();
                }
                follow.target = _player.transform;
                follow.SetBounds(-0.5f, -0.5f, _generator.Width - 0.5f, _generator.Height - 0.5f);
                follow.SnapToTarget();
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
                GUILayout.Label($"第 {currentLevel} 关 · {DungeonArt.Names[currentLevel - 1]}", _headerStyle);
                GUILayout.Label($"金币  {CoinsCollected} / {TotalCoins}     用时  {ElapsedTime:F1} 秒", _labelStyle);
                GUILayout.Label($"生命  {Health} / 3" + (currentLevel >= 3 ? "    小心巡逻怪" : "    安全探索"), _labelStyle);
                GUILayout.Label(HasKey ? "目标：前往绿色出口" : "目标：找到金色钥匙，开启出口", _labelStyle);
                GUILayout.Label("WASD 移动 · J 攻击 · R 重新生成", _labelStyle);
                var controller = _player != null ? _player.GetComponent<PlayerController>() : null;
                GUILayout.Label(controller != null && controller.DashCooldownRemaining > 0 ? $"冲刺冷却  {controller.DashCooldownRemaining:F1} 秒" : "空格冲刺 · 已就绪", _labelStyle);
                if (GUILayout.Button("暂停 / 继续 · Esc", _btnStyle, GUILayout.Height(34))) TogglePause();
                GUILayout.EndArea();
            }
            if(CurrentState == GameState.Playing)
            {
                GUILayout.BeginArea(new Rect((width-570)/2,height-68,570,54),_cardStyle);
                GUILayout.BeginHorizontal();
                for(int i=0;i<3;i++)if(GUILayout.Button($"[{i+1}] {ItemArt.Names[i]} ×{Inventory[i]}",GUILayout.Height(30)))UseItem(i);
                GUILayout.EndHorizontal();GUILayout.EndArea();
            }
            if (Time.unscaledTime < _noticeUntil && CurrentState == GameState.Playing)
                GUI.Box(new Rect((width - 420) / 2, height - 125, 420, 45), _notice, _cardStyle);
            if (CurrentState != GameState.Playing)
            {
                GUI.color = new Color(0, 0, 0, 0.65f);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float panelWidth = CurrentState == GameState.MainMenu ? 760 : 440;
                float panelHeight = CurrentState == GameState.MainMenu ? 480 : 360;
                GUILayout.BeginArea(new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight), _cardStyle);
                GUILayout.Space(16);
                string title = CurrentState == GameState.MainMenu ? "迷宫探险" : CurrentState == GameState.Paused ? "休息一下" : CurrentState == GameState.Failed ? "这次没能逃出去" : "成功逃出迷宫";
                GUILayout.Label(title, _winTitleStyle);
                GUILayout.Space(20);
                if (CurrentState == GameState.MainMenu)
                {
                    GUILayout.Label("选择你的探险伙伴 · 四位角色能力相同", _labelStyle);
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < 4; i++)
                    {
                        GUILayout.BeginVertical(GUILayout.Width(174));
                        Rect portrait = GUILayoutUtility.GetRect(160, 150);
                        var sprite = CharacterVisual.GetSprite(i, 0);
                        if (sprite != null)
                        {
                            Rect uv = sprite.rect;
                            uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
                            uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
                            float portraitWidth = 140 * sprite.rect.width / sprite.rect.height;
                            GUI.DrawTextureWithTexCoords(new Rect(portrait.center.x - portraitWidth / 2, portrait.y, portraitWidth, 140), sprite.texture, uv);
                        }
                        GUI.backgroundColor = i == SelectedCharacter ? new Color(0.4f, 1, 0.7f) : Color.white;
                        if (GUILayout.Button((i == SelectedCharacter ? "已选 · " : "") + CharacterVisual.Names[i], GUILayout.Height(34))) SelectCharacter(i);
                        GUI.backgroundColor = Color.white;
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(12);
                    GUILayout.Label("找到金色钥匙，让出口重新亮起。", _labelStyle);
                    GUILayout.Label("WASD 移动 · J 攻击 · 空格冲刺 · 1/2/3 道具", _labelStyle);
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
                    GUILayout.Label("小心地牢怪物，冰冻符石可以帮助脱险。", _labelStyle);
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
