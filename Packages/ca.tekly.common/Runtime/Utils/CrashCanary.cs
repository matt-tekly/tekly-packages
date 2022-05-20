using System;
using System.Collections.Generic;
using Tekly.Common.LifeCycles;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public class CrashCanary : Singleton<CrashCanary>
    {
        private const string CRASH_KEY = "tekly.crashcanary.marker";
        private const int CRASH_MARKER = 1;
        
        public bool CrashDetected { get; private set; }

        public CrashCanary()
        {
            LifeCycle.Instance.Quit += OnQuit;

            if (UsePause()) {
                LifeCycle.Instance.Pause += OnPause;    
            }
            
            if (UseFocus()) {
                LifeCycle.Instance.Focus += OnFocus;    
            }
            
            CrashDetected = PlayerPrefs.GetInt(CRASH_KEY, 0) == CRASH_MARKER;
            
            SetCanary(true);
        }
        
        private void OnQuit()
        {
            LifeCycle.Instance.Quit -= OnQuit;
            LifeCycle.Instance.Focus -= OnFocus;
            LifeCycle.Instance.Pause -= OnPause;
            
            SetCanary(false);
        }

        private void OnFocus(bool hasFocus)
        {
            SetCanary(hasFocus);
        }
        
        private void OnPause(bool paused)
        {
            SetCanary(!paused);
        }

        private void SetCanary(bool enabled)
        {
            if (enabled) {
                PlayerPrefs.SetInt(CRASH_KEY, CRASH_MARKER);
            } else {
                PlayerPrefs.DeleteKey(CRASH_KEY);
            }
            
            PlayerPrefs.Save();
        }

        private bool UseFocus()
        {
            return Application.platform == RuntimePlatform.tvOS 
                   || Application.platform == RuntimePlatform.IPhonePlayer;
        }
        
        private bool UsePause()
        {
            return Application.platform == RuntimePlatform.Android;
        }
    }
}