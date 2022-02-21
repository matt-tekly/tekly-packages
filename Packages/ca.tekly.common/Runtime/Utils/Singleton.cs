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
    
    /// <summary>
    /// A singleton where the Instance is exposed as an interface
    /// </summary>
    public class Singleton<TImpl, TInterface> where TImpl : Singleton<TImpl, TInterface>, TInterface, new() where TInterface : class
    {
        public static TInterface Instance { get; private set; }

        static Singleton()
        {
            Instance = new TImpl();
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        private static void Reset()
        {
            Instance = null;
        }
    }
}