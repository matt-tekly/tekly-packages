using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit
{
	public class DebugKitGui : MonoBehaviour
	{
		private UIDocument m_document;
		public VisualElement Root => m_document.rootVisualElement;
		
		private bool m_buttonEnabled;
		private int m_intValue;
		private float m_floatValue;
		private string m_stringValue;
		
		private void Awake()
		{
			var settings = Resources.Load<DebugKitSettings>("DebugKit/debug_kit");
			m_document = gameObject.AddComponent<UIDocument>();
			m_document.panelSettings = settings.PanelSettings;
			
			DebugKit.Instance.Settings = settings;
		}

		private void Update()
		{
			DebugKit.Instance.Update();
		}
		
		private void OnDestroy()
		{
			Debug.Log("Destroying");
		}
	}
}