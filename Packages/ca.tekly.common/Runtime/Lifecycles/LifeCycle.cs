// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections;
using Tekly.Common.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.LifeCycles
{
    public class LifeCycle : ILifeCycle
    {
        public static readonly ILifeCycle Instance = new LifeCycle();
        
        public event UpdateDelegate Update
        {
            add => m_updateDelegates.Add(value);
            remove => m_updateDelegates.Remove(value);
        }
        
        public event QuitDelegate Quit
        {
            add => m_quitDelegates.Add(value);
            remove => m_quitDelegates.Remove(value);
        }
        
        public event FocusDelegate Focus
        {
            add => m_focusDelegates.Add(value);
            remove => m_focusDelegates.Remove(value);
        }
        
        public event PauseDelegate Pause
        {
            add => m_pauseDelegates.Add(value);
            remove => m_pauseDelegates.Remove(value);
        }
        
        private static LifeCycleListener s_listener;
        
        private readonly SafeList<UpdateDelegate> m_updateDelegates = new SafeList<UpdateDelegate>();
        private readonly SafeList<QuitDelegate> m_quitDelegates = new SafeList<QuitDelegate>();
        private readonly SafeList<FocusDelegate> m_focusDelegates = new SafeList<FocusDelegate>();
        private readonly SafeList<PauseDelegate> m_pauseDelegates = new SafeList<PauseDelegate>();
        
        private static readonly Action<UpdateDelegate> s_updateInvoker = InvokeUpdate;
        private static readonly Action<QuitDelegate> s_quitInvoker = InvokeQuit;
        
        private static readonly Action<FocusDelegate> s_focusFalseInvoker = InvokeFocusFalse;
        private static readonly Action<FocusDelegate> s_focusTrueInvoker = InvokeFocusTrue;
        
        private static readonly Action<PauseDelegate> s_pauseFalseInvoker = InvokePauseFalse;
        private static readonly Action<PauseDelegate> s_pauseTrueInvoker = InvokePauseTrue;
        
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
            m_updateDelegates.ForEach(s_updateInvoker);
        }
        
        public void OnApplicationQuit()
        {
            m_quitDelegates.ForEach(s_quitInvoker);
        }

        public void OnApplicationPause(bool paused)
        {
            var del = paused ? s_pauseTrueInvoker : s_pauseFalseInvoker;
            m_pauseDelegates.ForEach(del);
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            var del = hasFocus ? s_focusTrueInvoker : s_focusFalseInvoker;
            m_focusDelegates.ForEach(del);
        }

        public Coroutine StartCoroutine(IEnumerator enumerator)
        {
            return s_listener.StartCoroutine(enumerator);
        }

        private static void InvokeUpdate(UpdateDelegate del)
        {
            del.Invoke();
        }
        
        private static void InvokeQuit(QuitDelegate del)
        {
            del.Invoke();
        }
        
        private static void InvokeFocusTrue(FocusDelegate del)
        {
            del.Invoke(true);
        }
        
        private static void InvokeFocusFalse(FocusDelegate del)
        {
            del.Invoke(false);
        }
        
        private static void InvokePauseTrue(PauseDelegate del)
        {
            del.Invoke(true);
        }
        
        private static void InvokePauseFalse(PauseDelegate del)
        {
            del.Invoke(false);
        }
    }
}
