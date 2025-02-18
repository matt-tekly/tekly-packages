using UnityEngine.UIElements;

namespace Tekly.DebugKit.Utils
{
	public static class ElementExtensions
	{
		public static void AddClassNames(this VisualElement element, string classNames)
		{
			if (classNames == null) {
				return;
			}

			if (classNames.Contains(" ")) {
				foreach (var className in classNames.Split(" ")) {
					element.AddToClassList(className);
				}
			} else {
				element.AddToClassList(classNames);
			}
		}
	}
}