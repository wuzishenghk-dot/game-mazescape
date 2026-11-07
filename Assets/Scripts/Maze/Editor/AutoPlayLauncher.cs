#if UNITY_EDITOR
using UnityEditor;

namespace MazeGame.Editor
{
    [InitializeOnLoad]
    public static class AutoPlayLauncher
    {
        private static bool s_hasStarted = false;

        static AutoPlayLauncher()
        {
            EditorApplication.delayCall += TriggerPlayOnce;
        }

        private static void TriggerPlayOnce()
        {
            if (!s_hasStarted && !EditorApplication.isPlaying)
            {
                s_hasStarted = true;
                EditorApplication.isPlaying = true;
            }
        }

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
