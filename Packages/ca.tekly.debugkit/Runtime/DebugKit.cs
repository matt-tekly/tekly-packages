using Tekly.Common.Utils;
using Tekly.DebugKit.Widgets;
using UnityEngine;

namespace Tekly.DebugKit
{
    public class DebugKit : Singleton<DebugKit>
    {
        public bool Visible { get; set; }
        
        private DebugKitGui m_debugKitGui;
        private MenuController m_menuController;
        
        public DebugKitSettings Settings;

        public void Initialize()
        {
            var go = new GameObject("DebugKit");
            m_debugKitGui = go.AddComponent<DebugKitGui>();

            m_menuController = new MenuController(m_debugKitGui.Root);
            Object.DontDestroyOnLoad(go);
        }

        public Menu Menu(string name, string classNames = null)
        {
            return m_menuController.Create(name, classNames);
        }

        public void Update()
        {
#if DEBUGKIT_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current[Settings.OpenKey].wasPressedThisFrame) {
                m_menuController.Toggle();
            }
#else
            if (Input.GetKeyDown(Settings.OpenKey)) {
                m_menuController.Toggle();
            }
#endif
            
            m_menuController.Update();
        }
    }
}
