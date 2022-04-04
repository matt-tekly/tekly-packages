// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections;
using UnityEngine;

namespace Tekly.Common.LifeCycles
{
    public class LifeCycleListener : MonoBehaviour
    {
        public LifeCycle LifeCycle { get; set; }
        
#if !TEKLY_DISABLE_LIFECYCLE_LISTENER
        private void OnApplicationQuit()
        {
            LifeCycle.OnApplicationQuit();
        }
        
        private void OnApplicationPause(bool paused)
        {
            LifeCycle.OnApplicationPause(paused);
        }
        
        private void Update()
        {
            LifeCycle.Updated();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            LifeCycle.OnApplicationFocus(hasFocus);
        }
#endif

        public Coroutine DoCoroutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }
    }
}