using System;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// What a button's color says about it. Panels take the colors from the DevBoardAssets' button tones.
	/// </summary>
	public enum ButtonTone
	{
		/// The usual look
		Neutral,

		/// The main action of a group, e.g. Apply or Start
		Accent,

		/// Something good or that adds, e.g. Give Gold or Unlock All
		Positive,

		/// Something destructive, e.g. Delete Save or Reset Progress
		Negative,

		/// Something to be careful with, e.g. Skip Tutorial or Force Disconnect
		Warning
	}

	/// <summary>
	/// A button that runs an action. Rebuilds started from the action happen at the end of the frame, so it can
	/// rebuild the scope it's in.
	/// </summary>
	public sealed class ButtonNode : Node
	{
		public Action Action { get; }

		/// <summary>
		/// The name of the icon to show, see <see cref="Icons"/>: before the label on a normal button, or on its own
		/// with <see cref="Styles.IconButton"/>. Null for the prefab's own.
		/// </summary>
		public string Icon { get; private set; }

		public ButtonTone Tone { get; private set; }

		/// <summary>
		/// No background until hovered or pressed, for buttons that shouldn't stand out, like icons in a row.
		/// </summary>
		public bool IsPlain { get; private set; }

		/// <summary>
		/// Only the icon is shown. The label still names the button for search, the palette and NodeAccess.
		/// </summary>
		public bool IsIconOnly { get; private set; }

		public ButtonNode(string label, Action action) : base(label)
		{
			Action = action;
		}

		/// <summary>
		/// Shows the icon with this name, from <see cref="Icons"/> or one a DevBoardAssets adds.
		/// </summary>
		public ButtonNode WithIcon(string icon)
		{
			EnsureEditable();
			Icon = icon;
			return this;
		}

		/// <summary>
		/// Colors the button for what it does, e.g. ButtonTone.Negative for Delete Save. On a plain button, the
		/// label and icon take the color instead of the background.
		/// </summary>
		public ButtonNode WithTone(ButtonTone tone)
		{
			EnsureEditable();
			Tone = tone;
			return this;
		}

		/// <summary>
		/// Leaves out the background until the button is hovered or pressed.
		/// </summary>
		public ButtonNode Plain()
		{
			EnsureEditable();
			IsPlain = true;
			return this;
		}

		/// <summary>
		/// Shows only the icon set with WithIcon. The label still names the button for search, the palette and
		/// NodeAccess.
		/// </summary>
		public ButtonNode IconOnly()
		{
			EnsureEditable();
			IsIconOnly = true;
			return this;
		}

		/// <summary>
		/// Runs the action, unless the button, or a container it's in, isn't interactable. Everything that runs a
		/// button goes through here.
		/// </summary>
		public void Run()
		{
			if (IsInteractable()) {
				Action?.Invoke();
			}
		}
	}

	/// <summary>
	/// A button that takes the panel it's clicked in to another page.
	/// </summary>
	public sealed class LinkNode : Node
	{
		public string Target { get; }

		public LinkNode(string label, string target) : base(label)
		{
			Target = PageTree.NormalizePath(target);
		}
	}

	public static class ButtonNodeFactory
	{
		public static ButtonNode Button(this ContainerNode parent, string label, Action action, Style<ButtonNode> style = default)
		{
			var node = parent.Add(new ButtonNode(label, action));
			node.SetStyle(style.Name);

			return node;
		}

		/// <summary>
		/// A small plain button showing only an icon, e.g. <c>row.IconButton("Clear", Icons.Trash, Clear)</c>. The
		/// label isn't shown, but names the button for search, the palette and NodeAccess. For an icon on a
		/// background, use Button(...).WithIcon(icon).IconOnly() instead.
		/// </summary>
		public static ButtonNode IconButton(this ContainerNode parent, string label, string icon, Action action)
		{
			return parent.Button(label, action, Styles.IconButton).WithIcon(icon).IconOnly().Plain();
		}

		/// <summary>
		/// A button that opens the page at path in the panel it's clicked in.
		/// </summary>
		public static LinkNode Link(this ContainerNode parent, string label, string path, Style<LinkNode> style = default)
		{
			var node = parent.Add(new LinkNode(label, path));
			node.SetStyle(style.Name);

			return node;
		}
	}

	/// <summary>
	/// The names of DevBoard's own icons, for <see cref="ButtonNode.WithIcon"/>. A DevBoardAssets can add more:
	/// each sprite is found by its name.
	/// </summary>
	public static class Icons
	{
		public const string Trash = "trash";
		public const string Copy = "copy";
		public const string Paste = "paste";
		public const string Refresh = "refresh";
		public const string Link = "link";
		public const string Download = "download";
		public const string Return = "return";
		public const string Close = "close";
		public const string Check = "check";
		public const string Search = "search";
		public const string Options = "options";
		public const string Pin = "pin";
		public const string NewTab = "newtab";
	}
}
