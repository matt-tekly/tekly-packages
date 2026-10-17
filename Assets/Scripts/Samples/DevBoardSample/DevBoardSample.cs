using System;
using System.Collections.Generic;
using Tekly.DevBoard;
using Tekly.DevBoard.Commands;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Nodes;
using Tekly.Leaf;
using Tekly.Trellis;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeklySample.Samples.DevBoardSample
{
	public enum Difficulty
	{
		Easy,
		Normal,
		Hard,
		Nightmare
	}

	/// <summary>
	/// One of each DevBoard control, on the "Sample/Controls" page, one section per kind of control.
	/// </summary>
	public class DevBoardSample : MonoBehaviour
	{
		private const string CONTROLS = "Sample/Controls";

		// Builders run once and every panel shows the same nodes, so state could live in them. It's kept here so
		// other code (and the inspector) can see it
		private string m_text = "Hello";
		private bool m_toggle = true;
		private int m_int = 5;
		private float m_float = 0.5f;
		private int m_clicks;
		private Difficulty m_difficulty = Difficulty.Normal;
		private string m_color = "Red";
		private string m_item;

		// This page's own hold. IsInputDisabled is also true while anything else holds the latch, e.g. a button's
		// press delay
		private bool m_isHoldingInput;

		private static readonly Difficulty[] s_difficulties = (Difficulty[]) System.Enum.GetValues(typeof(Difficulty));

		private static readonly string[] s_colors = { "Red", "Green", "Blue", "Yellow", "Purple" };

		// A long list, for the searchable dropdown and the search
		private readonly List<string> m_items = new();

		// Data that changes in the background, like a server response, for the Data section
		private readonly List<string> m_inventory = new() { "Wood Sword", "Iron Shield", "Gold Ring" };
		private bool m_showDetails;

		// Stands in for a slow job, like a scene load, that some buttons can't run during
		private float m_busyUntil;
		private bool IsBusy => Time.unscaledTime < m_busyUntil;

		private Vector3 m_position = new(1f, 2f, 3f);
		private bool m_locked;

		// Kept in PlayerPrefs, so it's still there next session
		private Binding<string> m_savedText;

		private void Start()
		{
			string[] materials = { "Wood", "Stone", "Iron", "Gold", "Crystal", "Bone", "Leather", "Silk" };
			string[] things = { "Sword", "Shield", "Helmet", "Boots", "Ring", "Potion", "Arrow", "Bow", "Wand", "Key" };

			foreach (var material in materials) {
				foreach (var thing in things) {
					m_items.Add($"{material} {thing}");
				}
			}

			m_items.Sort();
			m_item = m_items[0];
			m_savedText = PrefBinding.String("devboard.sample.saved-text", "Remember me");
			
			// Each section is a segment of the same page, in order. All are removed when this GameObject is destroyed.
			// In builds without DevBoard these calls are removed by the compiler
			DevBoard.Page(CONTROLS, Labels, "Labels and Properties", 0, owner: gameObject);
			DevBoard.Page(CONTROLS, Buttons, "Buttons", 1, owner: gameObject);
			DevBoard.Page(CONTROLS, Toggles, "Toggles", 2, owner: gameObject);
			DevBoard.Page(CONTROLS, TextInputs, "Text Inputs", 3, owner: gameObject);
			DevBoard.Page(CONTROLS, NumberInputs, "Number Inputs", 4, owner: gameObject);
			DevBoard.Page(CONTROLS, Sliders, "Sliders", 5, owner: gameObject);
			DevBoard.Page(CONTROLS, Dropdowns, "Dropdowns", 6, owner: gameObject);
			DevBoard.Page(CONTROLS, Layout, "Layout", 7, owner: gameObject);
			DevBoard.Page(CONTROLS, Search, "Search", 8, owner: gameObject);
			DevBoard.Page(CONTROLS, Data, "Data", 9, owner: gameObject);

			// Commands for the console (F2), removed with this GameObject like the pages. A last text argument takes
			// the rest of the line, so "inventory add Gold Ring" needs no quotes. suggest gives the values the console
			// offers as an argument is typed
			DevBoard.Command("inventory add", (string item) => AddItem(item), "Adds an item to the inventory", gameObject,
				suggest: _ => m_items);
			DevBoard.Command("inventory remove", (CommandContext context, string item) => RemoveItem(context, item),
				"Removes an item from the inventory", gameObject, suggest: _ => m_inventory);
			DevBoard.Command("inventory list", () => string.Join(", ", m_inventory), "Lists the inventory", gameObject);
			DevBoard.Command("inventory clear", () => m_inventory.Clear(), "Empties the inventory", gameObject);
			DevBoard.Command("exception", () => throw new Exception("Meow"), "Throws Exception", gameObject);
		}

		private string AddItem(string item)
		{
			if (m_inventory.Contains(item)) {
				return $"Already have {item}";
			}

			m_inventory.Add(item);
			return $"Added {item}";
		}

		// A CommandContext parameter isn't typed by the user: it's for output, here reporting a failure
		private void RemoveItem(CommandContext context, string item)
		{
			var index = m_inventory.FindIndex(other => string.Equals(other, item, System.StringComparison.OrdinalIgnoreCase));

			if (index < 0) {
				context.Error($"No {item} in the inventory");
				return;
			}

			context.Log($"Removed {m_inventory[index]}");
			m_inventory.RemoveAt(index);
		}

		// A static method becomes a command with the attribute alone. [Suggest] names a static method giving the
		// values the console offers for a parameter
		[DevCommand("sample greet", "Says hello, a number of times")]
		private static void Greet(CommandContext context, [Suggest(nameof(GreetNames))] string name = "world", int times = 1)
		{
			for (var i = 0; i < times; i++) {
				context.Log($"Hello, {name}!");
			}
		}

		private static IEnumerable<string> GreetNames() => new[] { "world", "team", "Unity" };

		private void Labels(Segment page)
		{
			var form = page.Form();
			form.Label("A label is plain text.");
			
			// Properties read their getter every tick and show the value with a format
			form.Property("Time", () => Time.time, "{0:N2}");
			form.Property("Monospaced", () => Time.time, "{0:N2}").WithMonospaced();

			// With an epsilon, small float changes don't redraw the text
			form.Property("Frame", () => Time.frameCount).WithMonospaced();
			form.Property("Epsilon 0.5", () => Time.time, 0.5f, "{0:N1}").WithMonospaced();
		}

		private void Buttons(Segment page)
		{
			var form = page.Form();
			form.Property("Clicks", () => m_clicks);
			form.Button("Click", () => m_clicks++);

			var row = form.Row().WithPadding(0);
			row.Button("-1", () => m_clicks--);
			row.Button("+1", () => m_clicks++);
			row.Button("Reset", () => m_clicks = 0);

			// InteractableWhen greys a button out while it returns false. Clicks, the palette and NodeAccess skip it
			form.Property("Busy", () => IsBusy ? $"{m_busyUntil - Time.unscaledTime:F1} s" : "No");
			row = form.Row().WithPadding(0);
			row.Button("Start Job", () => m_busyUntil = Time.unscaledTime + 3f).InteractableWhen(() => !IsBusy);
			row.Button("Needs Idle", () => m_clicks++).InteractableWhen(() => !IsBusy);

			// Icon buttons show one of DevBoard's icons. The label names them for search and the palette
			row = form.Row().WithPadding(0);
			row.IconButton("Clear Clicks", Icons.Trash, () => m_clicks = 0);
			row.IconButton("Copy Clicks", Icons.Copy, () => GUIUtility.systemCopyBuffer = m_clicks.ToString());
			row.IconButton("Paste Clicks", Icons.Paste, () => int.TryParse(GUIUtility.systemCopyBuffer, out m_clicks));
			row.IconButton("Refresh", Icons.Refresh, () => Debug.Log("Refresh"));
			row.IconButton("Open Link", Icons.Link, () => Debug.Log("Link"));
			row.IconButton("Download", Icons.Download, () => Debug.Log("Download"));
			row.IconButton("Return", Icons.Return, () => Debug.Log("Return"));
			row.IconButton("Busy Trash", Icons.Trash, () => m_clicks = 0).InteractableWhen(() => !IsBusy);

			// What a button shows follows what it's given: text, an icon before the text, or only the icon
			row = form.Row().WithPadding(0);
			row.Button("Text", () => m_clicks++);
			row.Button("Text and Icon", () => m_clicks++).WithIcon(Icons.Refresh);
			row.Button("Icon Only", () => m_clicks++).WithIcon(Icons.Refresh).IconOnly();

			// Tones say what a button does. Plain buttons have no background until hovered, and carry the tone
			// in their text and icon
			row = form.Row().WithPadding(0);
			row.Button("Neutral", () => m_clicks++);
			row.Button("Accent", () => m_clicks++).WithTone(ButtonTone.Accent);
			row.Button("Positive", () => m_clicks++).WithTone(ButtonTone.Positive);
			row.Button("Negative", () => m_clicks = 0).WithIcon(Icons.Trash).WithTone(ButtonTone.Negative);
			row.Button("Warning", () => m_clicks++).WithTone(ButtonTone.Warning);

			row = form.Row().WithPadding(0);
			row.Button("Plain", () => m_clicks++).Plain();
			row.Button("Plain Negative", () => m_clicks = 0).WithIcon(Icons.Trash).WithTone(ButtonTone.Negative).Plain();
			row.IconButton("Delete", Icons.Trash, () => m_clicks = 0).WithTone(ButtonTone.Negative);
			row.Button("Disabled Positive", () => m_clicks++).WithTone(ButtonTone.Positive).InteractableWhen(() => false);

			// For turning something on or off that has no single state to read. Use a toggle when there is one
			form.OnOffButtons("Logging", on => Debug.Log(on ? "Logging on" : "Logging off"));

			// A segmented row joins its buttons into one bar, rounding only the outer corners
			row = form.ButtonRow();
			row.Button("-10", () => m_clicks -= 10);
			row.Button("-1", () => m_clicks--);
			row.Button("+1", () => m_clicks++);
			row.Button("+10", () => m_clicks += 10);
		}

		private void Toggles(Segment page)
		{
			var form = page.Form();
			form.Toggle("Toggle", () => m_toggle, value => m_toggle = value);
			form.Property("Value", () => m_toggle);
		}

		private void TextInputs(Segment page)
		{
			var form = page.Form();
			form.Property("Value", () => m_text);

			// When typed text reaches the setter
			form.TextInput("Delayed", () => m_text, value => m_text = value)
				.WithPlaceholder("On Enter or deselect")
				.WithInputMode(InputMode.Delayed);
			form.TextInput("Debounced", () => m_text, value => m_text = value)
				.WithPlaceholder("When typing pauses")
				.WithInputMode(InputMode.Debounced);
			form.TextInput("Immediate", () => m_text, value => m_text = value)
				.WithPlaceholder("Every keystroke")
				.WithInputMode(InputMode.Immediate);

			// A PrefBinding keeps the value in PlayerPrefs, like DebugKit's TextFieldPersist
			form.TextInput("Saved", m_savedText).WithDescription("Kept between sessions");
		}

		private void NumberInputs(Segment page)
		{
			var form = page.Form();
			form.IntInput("Int", () => m_int, value => m_int = value);

			// The setter can clamp: the field shows the clamped value once editing ends
			form.IntInput("Int (0-10)", () => m_int, value => m_int = Mathf.Clamp(value, 0, 10));

			form.FloatInput("Float", () => m_float, value => m_float = value);
			form.FloatInput("Float (0.00)", () => m_float, value => m_float = value).WithFormat("0.00");

			// X, Y and Z inputs in a row
			form.Vector3Input("Position", () => m_position, value => m_position = value);
			form.Property("Position", () => m_position);
		}

		private void Sliders(Segment page)
		{
			var form = page.Form();
			form.Slider("Float", 0f, 1f, () => m_float, value => m_float = value).WithFormat("0.00");
			form.SliderInt("Int", 0, 10, () => m_int, value => m_int = value);
			form.Slider("Time Scale", 0f, 2f, () => Time.timeScale, value => Time.timeScale = value).WithFormat("0.0");

			// Without a label or a value input, the bar takes the whole row
			form.Slider("No Input", 0f, 1f, () => m_float, value => m_float = value).WithValueInput(false);
			form.Slider(null, 0f, 1f, () => m_float, value => m_float = value).WithValueInput(false);

			// The value under the label instead of in an input
			form.Slider("Compact", 0f, 1f, () => m_float, value => m_float = value)
				.WithFormat("0.00")
				.WithValueInput(false)
				.WithValueInLabel(true);
		}

		private void Dropdowns(Segment page)
		{
			var form = page.Form();

			// Every value of an enum
			form.Dropdown("Enum", () => m_difficulty, value => m_difficulty = value);

			// A list of strings
			form.Dropdown("Strings", s_colors, () => m_color, value => m_color = value);

			// Each choice can have a subtitle, shown under the title in the popup
			form.Dropdown("With Subtitles", s_difficulties, d => new DropdownChoice(d.ToString(), Describe(d)),
				() => m_difficulty, value => m_difficulty = value);

			// Any list, with the text for each item. WithSearch suits long lists
			form.Dropdown("Searchable", m_items, item => item, () => m_item, value => m_item = value).WithSearch();

			// Without a label the dropdown takes the whole row
			form.Dropdown(null, s_colors, () => m_color, value => m_color = value);
		}

		private static string Describe(Difficulty difficulty)
		{
			switch (difficulty) {
				case Difficulty.Easy:
					return "For learning the ropes";
				case Difficulty.Normal:
					return "The intended experience";
				case Difficulty.Hard:
					return "Enemies hit harder";
				default:
					return "One life, no saves";
			}
		}

		private void Layout(Segment page)
		{
			var form = page.Form();
			form.Label("A row lays widgets out side by side:");

			var row = form.Row();
			row.Button("A", () => Debug.Log("A"));
			row.Button("B", () => Debug.Log("B"));
			row.Button("C", () => Debug.Log("C"));

			form.Divider();

			// A foldout is a container that opens and closes, and remembers which
			var foldout = form.Foldout("Foldout");
			foldout.Label("Inside the foldout");
			foldout.Property("Time", () => Time.time, "{0:N2}");

			var nested = foldout.Foldout("Nested Foldout");
			nested.Label("Foldouts can nest");

			// Tabs show one tab's content at a time, remember which, and build each tab the first time it's shown
			var tabs = page.Tabs();

			var general = tabs.Tab("General").Form();
			general.Toggle("Toggle", () => m_toggle, value => m_toggle = value);
			general.Property("Clicks", () => m_clicks);

			var audio = tabs.Tab("Audio").Form();
			audio.Slider("Music", 0f, 1f, () => m_float, value => m_float = value).WithFormat("0.00");
			audio.Label("Built the first time this tab was shown");

			var advanced = tabs.Tab("Advanced");
			advanced.Button("Click", () => m_clicks++);

			// A card is a container with a background
			var card = page.Card();
			card.Heading("A card");
			card.Property("Clicks", () => m_clicks);

			// InteractableWhen on a container greys out everything inside it. Values keep refreshing
			page.Form().Toggle("Lock the card below", () => m_locked, value => m_locked = value);
			var locked = page.Card().InteractableWhen(() => !m_locked);
			locked.Heading("Locked while the toggle is on");
			locked.Property("Clicks", () => m_clicks);
			locked.Button("Click", () => m_clicks++);
			locked.Toggle("Toggle", () => m_toggle, value => m_toggle = value);
			locked.Slider("Float", 0f, 1f, () => m_float, value => m_float = value).WithFormat("0.00");
			locked.TextInput("Text", () => m_text, value => m_text = value);
		}

		private void Search(Segment page)
		{
			// A list with a search field. Each result is built by the row builder, so a row can be any nodes
			page.Form().List(() => m_items, (row, item) => {
					var line = row.Row()
						.WithAlignment(LayoutAlignment.SpaceBetween)
						.WithCrossAlignment(CrossAlignment.Center)
						.WithPadding(0);
					line.Label(item);
					line.Button("Pick", () => m_item = item);
				})
				.WithSearch(item => item)
				.WithScroll(150)
				.WithRowSpacing(1)
				.WithMaxResults(8);
		}

		private void Data(Segment page)
		{
			var form = page.Form();

			// The buttons only change the data. The list follows it by key: rows come and go, and the rest stay put
			var row = form.Row().WithPadding(0);
			row.Button("Add", () => {
				// Rows are keyed by name, so keep names unique
				var name = m_items[Random.Range(0, m_items.Count)];

				if (!m_inventory.Contains(name)) {
					m_inventory.Add(name);
				}
			});
			row.Button("Remove", () => {
				if (m_inventory.Count > 0) {
					m_inventory.RemoveAt(Random.Range(0, m_inventory.Count));
				}
			});
			row.Button("Sort", () => m_inventory.Sort());

			form.List(() => m_inventory, (item, name) => item.Label(name));

			// A dynamic scope rebuilds when its key changes, for content whose shape depends on data
			form.Toggle("Show details", () => m_showDetails, value => m_showDetails = value);
			form.Dynamic(() => m_showDetails, (scope, showDetails) => {
				if (!showDetails) {
					scope.Label("Details are hidden");
					return;
				}

				scope.Property("Items", () => m_inventory.Count);
				scope.Property("Clicks", () => m_clicks);
			});

			// VisibleWhen shows and hides a node without rebuilding anything
			form.Label("Shown while the Toggles section's toggle is on").VisibleWhen(() => m_toggle);

			// A link opens another page in the panel it's clicked in
			form.Link("Leaf options", "Sample/Leaf");
		}

		private void OnDestroy()
		{
			// LeafCore outlives this scene, so a hold left behind would keep input disabled
			if (m_isHoldingInput) {
				m_isHoldingInput = false;
				LeafCore.Instance.DisableInput.Release(this);
			}
		}
	}
}
