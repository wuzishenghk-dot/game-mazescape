#if UNITY_EDITOR
using UnityEditor;

namespace MazeGame.Editor
{
    public static class AutoPlayLauncher
    {
        [MenuItem("Maze Game/▶ Play Game")]
        public static void Play()
        {
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Maze Game/⏹ Stop Game")]
        public static void Stop()
        {
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
