using System;
using System.Globalization;
using Tekly.DevBoard.Nodes;
using UnityEngine;

namespace Tekly.DevBoard.Access
{
	public sealed partial class NodeAccess
	{
		private const string ON = "On";
		private const string OFF = "Off";
		private const string NO_CHOICE = "-";

		private void RegisterDefaults()
		{
			Register<LabelNode>("label", read: node => node.Text);

			Register<PropertyNode>("property", read: node => {
				var reader = new PropertyReader(node.Format);
				node.Accept(reader);

				return reader.Text;
			});

			Register<ButtonNode>("button", invoke: node => node.Run());

			// No state to read, so it's only set
			Register<OnOffButtonsNode>("onoff",
				write: (node, text) => {
					if (!TryParseBool(text, out var on)) {
						return $"'{text}' isn't on or off";
					}

					node.Run(on);
					return null;
				});

			// Opening a page needs a host, so whoever shows the link opens it
			Register<LinkNode>("link", read: node => node.Target);

			Register<ListNode>("list", read: node => node.Children.Count == 1 ? "1 row" : $"{node.Children.Count} rows");

			Register<ToggleNode>("toggle",
				read: node => node.Binding.Read() ? ON : OFF,
				write: (node, text) => {
					if (!TryParseBool(text, out var value)) {
						return $"'{text}' isn't on or off";
					}

					node.Binding.Write(value);
					return null;
				},
				invoke: node => node.Binding.Write(!node.Binding.Read()));

			Register<TextInputNode>("text",
				read: node => node.Binding.Read(),
				write: (node, text) => {
					node.Binding.Write(text ?? string.Empty);
					return null;
				});

			Register<NumberInputNode>("number",
				read: node => NumberText.FormatNumber(node.Binding.Read(), node.WholeNumbers, node.Format),
				write: (node, text) => {
					if (!NumberText.TryParse(text, node.WholeNumbers, out var value)) {
						return node.WholeNumbers ? $"'{text}' isn't a whole number" : $"'{text}' isn't a number";
					}

					node.Binding.Write(value);
					return null;
				});

			Register<Vector3InputNode>("vector3",
				read: node => {
					var value = node.Binding.Read();
					return string.Join(", ", NumberText.FormatNumber(value.x, false, node.Format),
						NumberText.FormatNumber(value.y, false, node.Format), NumberText.FormatNumber(value.z, false, node.Format));
				},
				write: (node, text) => {
					if (!TryParseVector3(text, out var value)) {
						return $"'{text}' isn't three numbers, e.g. 1, 2, 3";
					}

					node.Binding.Write(value);
					return null;
				});

			Register<SliderNode>("slider",
				read: node => NumberText.FormatSlider(node.Binding.Read(), node.WholeNumbers, node.Format),
				write: (node, text) => {
					if (!NumberText.TryParse(text, false, out var value)) {
						return $"'{text}' isn't a number";
					}

					node.Binding.Write(NumberText.ClampSlider(value, node.Min, node.Max, node.WholeNumbers));
					return null;
				});

			Register<DropdownNode>("dropdown",
				read: node => {
					var index = node.Index.Read();
					return index >= 0 && index < node.GetCount() ? node.GetChoice(index).Title : NO_CHOICE;
				},
				write: (node, text) => {
					var index = FindChoice(node, text);

					if (index < 0) {
						return $"'{text}' isn't one of the choices";
					}

					node.Index.Write(index);
					return null;
				});
		}

		/// <summary>
		/// The choice whose title is text, ignoring case, or failing that the choice at index text. -1 if neither.
		/// </summary>
		private static int FindChoice(DropdownNode node, string text)
		{
			var count = node.GetCount();
			var trimmed = text?.Trim() ?? string.Empty;

			for (var i = 0; i < count; i++) {
				if (string.Equals(node.GetChoice(i).Title, trimmed, StringComparison.OrdinalIgnoreCase)) {
					return i;
				}
			}

			if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < count) {
				return index;
			}

			return -1;
		}

		/// <summary>
		/// Three numbers separated by commas or spaces, e.g. "1, 2.5, -3".
		/// </summary>
		private static bool TryParseVector3(string text, out Vector3 value)
		{
			value = default;
			var parts = (text ?? string.Empty).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length != 3) {
				return false;
			}

			for (var i = 0; i < 3; i++) {
				if (!NumberText.TryParse(parts[i], false, out var part)) {
					return false;
				}

				value[i] = (float) part;
			}

			return true;
		}

		private static bool TryParseBool(string text, out bool value)
		{
			switch (text?.Trim().ToLowerInvariant()) {
				case "on":
				case "true":
				case "yes":
				case "1":
					value = true;
					return true;
				case "off":
				case "false":
				case "no":
				case "0":
					value = false;
					return true;
				default:
					value = false;
					return false;
			}
		}

		/// <summary>
		/// Formats a property's value the way its widget does.
		/// </summary>
		private sealed class PropertyReader : IBindingVisitor
		{
			public string Text { get; private set; }

			private readonly string m_format;

			public PropertyReader(string format)
			{
				m_format = format;
			}

			public void Visit<T>(Binding<T> binding)
			{
				Text = string.Format(m_format, binding.Read());
			}
		}
	}
}
