using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.LifeCycles
{
    public class LifeCycle : ILifeCycle
    {
        public static readonly ILifeCycle Instance = new LifeCycle();
        
        public event UpdateDelegate Update
        {
            add => m_updatesDelegates.AddLast(value);
            remove => m_updatesDelegates.Remove(value);
        }
        
        public event QuitDelegate Quit
        {
            add => m_quitDelegates.AddLast(value);
            remove => m_quitDelegates.Remove(value);
        }
        
        public event FocusDelegate Focus
        {
            add => m_focusDelegates.AddLast(value);
            remove => m_focusDelegates.Remove(value);
        }
        
        public event PauseDelegate Pause
        {
            add => m_pauseDelegates.AddLast(value);
            remove => m_pauseDelegates.Remove(value);
        }
        
        private static LifeCycleListener s_listener;
        
        private readonly LinkedList<QuitDelegate> m_quitDelegates = new LinkedList<QuitDelegate>();
        private readonly LinkedList<FocusDelegate> m_focusDelegates = new LinkedList<FocusDelegate>();
        private readonly LinkedList<PauseDelegate> m_pauseDelegates = new LinkedList<PauseDelegate>();
        
        private readonly LinkedList<UpdateDelegate> m_updatesDelegates = new LinkedList<UpdateDelegate>();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            var gameObject = new GameObject("[TK] LifeCycle");
            Object.DontDestroyOnLoad(gameObject);

            s_listener = gameObject.AddComponent<LifeCycleListener>();
            s_listener.LifeCycle = Instance as LifeCycle;
        }

        public void Updated()
        {
            for (var node = m_updatesDelegates.First; node != null; node = node.Next) {
                node.Value.Invoke();
            }
        }
        
        public void OnApplicationQuit()
        {
            for (var node = m_quitDelegates.First; node != null; node = node.Next) {
                try {
                    node.Value.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void OnApplicationPause(bool paused)
        {
            for (var node = m_pauseDelegates.First; node != null; node = node.Next) {
                try {
                    node.Value.Invoke(paused);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            for (var node = m_focusDelegates.First; node != null; node = node.Next) {
                try {
                    node.Value.Invoke(hasFocus);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public Coroutine StartCoroutine(IEnumerator enumerator)
        {
            return s_listener.StartCoroutine(enumerator);
        }
    }
}
