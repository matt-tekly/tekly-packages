namespace Tekly.Common.Utils
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        public static T Instance { get; private set; }

        static Singleton()
        {
            Instance = new T();
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        private static void Reset()
        {
            Instance = null;
        }
    }
}