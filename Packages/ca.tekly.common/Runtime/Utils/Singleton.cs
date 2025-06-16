using System;

namespace Tekly.Common.Utils
{
    public class Singleton<T> where T : new()
    {
        public static T Instance { get; private set; }

        static Singleton()
        {
            UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        private static void Init()
        {
            Instance = new T();
        }

        private static void Reset()
        {
            if (Instance is IDisposable disposable) {
                disposable.Dispose();
            }
            
            Instance = default;
        }

        protected static void ForceInit()
        {
            
        }
    }
    
    public abstract class SingletonFactory<T, TFactory> where TFactory : SingletonFactory<T, TFactory>, new()
    {
        public static T Instance { get; private set; }
        private static TFactory s_factory;

        static SingletonFactory()
        {
            UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        protected abstract T Create();

        private static void Init()
        {
            s_factory = new TFactory();
            Instance = s_factory.Create();
        }

        private static void Reset()
        {
            if (Instance is IDisposable disposable) {
                disposable.Dispose();
            }
            
            if (s_factory is IDisposable factoryDisposable) {
                factoryDisposable.Dispose();
            }
            
            Instance = default;
            s_factory = null;
        }

        protected static void ForceInit()
        {
            
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
            UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        private static void Init()
        {
            Instance = new TImpl();
        }

        private static void Reset()
        {
            if (Instance is IDisposable disposable) {
                disposable.Dispose();
            }
            
            Instance = null;
        }
        
        protected static void ForceInit()
        {
            
        }
    }
}