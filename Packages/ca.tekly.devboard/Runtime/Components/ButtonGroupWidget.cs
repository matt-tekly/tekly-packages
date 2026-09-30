using System;

namespace Tekly.DevBoard.Components
{
	public class ButtonGroupWidget : Widget
	{
		// public ButtonWidget Button(string label, Action action)
		// {
		// 	var instance = Instantiate(DevBoard.Instance.Assets.Button, transform, false);
		// 	instance.Initialize(action, label);
		// 	
		// 	return instance;
		// }
		//
		private void UpdateButtons()
		{
			
		}

		private void OnTransformChildrenChanged()
		{
			UpdateButtons();
		}
	}
}