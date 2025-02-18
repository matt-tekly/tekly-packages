using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit
{
	[CreateAssetMenu(menuName = "Tekly/Debug Kit/Settings", fileName = "DebugKitSettings", order = 0)]
	public class DebugKitSettings : ScriptableObject
	{
#if DEBUGKIT_INPUT_SYSTEM
		public UnityEngine.InputSystem.Key OpenKey = UnityEngine.InputSystem.Key.F2;
#else
		public KeyCode OpenKey = KeyCode.F2;
#endif
		
		public PanelSettings PanelSettings;
	}
}