#if UNITY_EDITOR || !TEKLY_CONSOLE_DISABLE
    #define TERMINAL_ENABLED
#endif

using System;
using System.Collections.Generic;
using Tekly.Common.Terminal.Commands;
using UnityEngine;

namespace Tekly.Terminal
{
    #warning add prefs - font size?
    #warning more basic commmands
    #warning get mobile working
    #warning async commands

    [Serializable]
    public class TerminalPrefs
    {
        public float ViewScale;
        public float ViewSize;
        public List<string> CommandHistory;
    }
    
    public class TerminalRoot : MonoBehaviour
    {
        public bool EnableBasicCommands = true;
        [Range(0.3f, 1f)]
        public float DefaultViewSize = 1;
        [Range(0.5f, 3f)]
        public float DefaultViewScale = 1;

#if TERMINAL_ENABLED
        public TerminalView DesktopTerminal;
#endif
        private CommandStore m_commandStore;
        private TerminalView m_terminalView;
        
        private bool m_active;
        private bool m_wasActive;
        
        private float m_touchTimer;
        
        private const string PLAYER_PREFS_KEY = "_tk_terminal_prefs";

        public void SetActive(bool active)
        {
            m_active = active;
            m_terminalView.gameObject.SetActive(active);    
        }
        
        public void SetViewScale(float scale, bool save = true)
        {
            m_terminalView.SetScale(scale);

            if (save) {
                Save();
            }
        }

        public float GetViewScale()
        {
            return m_terminalView.GetScale();
        }
        
        public void SetViewSize(float size, bool save = true)
        {
            m_terminalView.SetSize(size);
            
            if (save) {
                Save();
            }
        }

        public float GetViewSize()
        {
            return m_terminalView.GetSize();
        }

        public void ResetPreferences()
        {
            SetViewSize(DefaultViewSize);
            SetViewScale(DefaultViewScale);
            
            m_commandStore.CommandHistory.Clear();
            
            Save();
        }

        public void Save()
        {
#if TERMINAL_ENABLED
            SavePrefs();
#endif            
        }
        
#if TERMINAL_ENABLED
        private void Awake()
        {
            Debug.Log("Terminal Enabled");
            
            m_commandStore = CommandStore.Instance;
            
            if (Application.isMobilePlatform) {
                throw new Exception("Mobile don't work");
            } else {
                m_terminalView = Instantiate(DesktopTerminal, transform);
            }
            
            SetActive(false);

            if (EnableBasicCommands) {
                m_commandStore.AddCommandSource(new BasicCommands());
                m_commandStore.AddCommandSource(new PlayerPrefsCommands());
            }
            
            m_commandStore.AddCommandSource(new TerminalCommands(m_commandStore, this));
            m_commandStore.CommandExecuted += Save;
            
            LoadPrefs();
            
            m_terminalView.Initialize(this, m_commandStore);
        }
        
        private void LoadPrefs()
        {
            var prefsJson = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
            
            if (!string.IsNullOrEmpty(prefsJson)) {
                var prefs = JsonUtility.FromJson<TerminalPrefs>(prefsJson);
                
                SetViewScale(prefs.ViewScale, false);
                SetViewSize(prefs.ViewSize, false);

                m_commandStore.Initialize(prefs);
            } else {
                m_commandStore.Initialize(null);
                ResetPreferences();
            }
        }
        
        private void SavePrefs()
        {
            var prefs = new TerminalPrefs();
            prefs.ViewScale = m_terminalView.GetScale();
            prefs.ViewSize = m_terminalView.GetSize();
            prefs.CommandHistory = m_commandStore.CommandHistory;
            
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, JsonUtility.ToJson(prefs));
        }
        
        private bool HasTapped()
        {
            if (Input.touchCount >= 3) {
                m_touchTimer += Time.deltaTime;
            } else {
                m_touchTimer = 0;
            }

            if (m_touchTimer > 1) {
                m_touchTimer = 0;
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (HasTapped()) {
                SetActive(!m_active);
            }
            
            // Checking if the it was active prevents opening the console again when it was closed by pressing backquote
            if (!m_wasActive && Input.GetKeyDown(KeyCode.BackQuote)) {
                SetActive(true);
            }
            
            m_wasActive = m_active;
        }
#endif
    }
}