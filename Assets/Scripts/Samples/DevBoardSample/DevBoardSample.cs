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
		}

		private void NumberInputs(Segment page)
		{
			var form = page.Form();
			form.IntInput("Int", () => m_int, value => m_int = value);

			// The setter can clamp: the field shows the clamped value once editing ends
			form.IntInput("Int (0-10)", () => m_int, value => m_int = Mathf.Clamp(value, 0, 10));

			form.FloatInput("Float", () => m_float, value => m_float = value);
			form.FloatInput("Float (0.00)", () => m_float, value => m_float = value).WithFormat("0.00");
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

			// A card is a container with a background
			var card = page.Card();
			card.Heading("A card");
			card.Property("Clicks", () => m_clicks);
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
