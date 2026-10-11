using System;
using Tekly.DevBoard.Components.Inputs;

namespace Tekly.DevBoard.Components.Inputs
{
	public class TextInputWidget : InputWidget<string>
	{
		/// <summary>
		/// The text shown while the field is empty.
		/// </summary>
		public TextInputWidget WithPlaceholder(string placeholder)
		{
			SetPlaceholder(placeholder);
			return this;
		}

		/// <summary>
		/// When typed text reaches the setter. Delayed by default.
		/// </summary>
		public TextInputWidget WithInputMode(InputMode inputMode)
		{
			SetInputMode(inputMode);
			return this;
		}

		protected override bool TryParse(string text, out string value)
		{
			value = text;
			return true;
		}

		protected override string Format(string value)
		{
			return value ?? string.Empty;
		}
	}
}

namespace Tekly.DevBoard.Components
{
	public static class TextInputWidgetFactory
	{
		public static TextInputWidget TextInput(this ContainerWidget parent, string label, Func<string> getValue, Action<string> setValue,
			WidgetPrefab<TextInputWidget> prefab = default)
		{
			return parent.TextInput(label, new Binding<string>(getValue, setValue), prefab);
		}

		public static TextInputWidget TextInput(this ContainerWidget parent, string label, Binding<string> binding,
			WidgetPrefab<TextInputWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.TextInput));
			instance.Initialize(label, binding);

			return instance;
		}
	}
}
