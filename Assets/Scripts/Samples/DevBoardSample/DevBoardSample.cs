using System.Collections.Generic;
using Tekly.DevBoard;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Pages;
using Tekly.Leaf;
using Tekly.Trellis;
using UnityEngine;

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

		// State lives here, not in the builders: builders can run several times (once per panel showing the page)
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

			var devBoard = DevBoard.Instance;

			// Each section is a segment of the same page, in order. All are removed when this GameObject is destroyed
			devBoard.Page(CONTROLS, Labels, "Labels and Properties", 0).BindTo(gameObject);
			devBoard.Page(CONTROLS, Buttons, "Buttons", 1).BindTo(gameObject);
			devBoard.Page(CONTROLS, Toggles, "Toggles", 2).BindTo(gameObject);
			devBoard.Page(CONTROLS, TextInputs, "Text Inputs", 3).BindTo(gameObject);
			devBoard.Page(CONTROLS, NumberInputs, "Number Inputs", 4).BindTo(gameObject);
			devBoard.Page(CONTROLS, Sliders, "Sliders", 5).BindTo(gameObject);
			devBoard.Page(CONTROLS, Dropdowns, "Dropdowns", 6).BindTo(gameObject);
			devBoard.Page(CONTROLS, Layout, "Layout", 7).BindTo(gameObject);
			devBoard.Page(CONTROLS, Search, "Search", 8).BindTo(gameObject);

			devBoard.Page("Sample/Leaf", page => {
				page.Root.Toggle("Input Disabled", () => m_isHoldingInput, value => {
					if (value == m_isHoldingInput) {
						return;
					}

					m_isHoldingInput = value;

					if (value) {
						LeafCore.Instance.DisableInput.Hold(this);
					} else {
						LeafCore.Instance.DisableInput.Release(this);
					}
				});
			}).BindTo(gameObject);
		}

		private void Labels(PageContext page)
		{
			var form = page.Root.Form();
			form.Label("A label is plain text.");

			// Properties read their getter every tick and show the value with a format
			form.Property("Time", () => Time.time, "{0:N2}");
			form.PropertyMonospaced("Monospaced", () => Time.time, "{0:N2}");

			// With an epsilon, small float changes don't redraw the text
			form.PropertyMonospaced("Frame", () => Time.frameCount);
			form.PropertyMonospaced("Epsilon 0.5", () => Time.time, 0.5f, "{0:N1}");
		}

		private void Buttons(PageContext page)
		{
			var form = page.Root.Form();
			form.Property("Clicks", () => m_clicks);
			form.Button("Click", () => m_clicks++);

			var row = form.Row();
			row.Button("-1", () => m_clicks--);
			row.Button("+1", () => m_clicks++);
			row.Button("Reset", () => m_clicks = 0);
		}

		private void Toggles(PageContext page)
		{
			var form = page.Root.Form();
			form.Toggle("Toggle", () => m_toggle, value => m_toggle = value);
			form.Property("Value", () => m_toggle);
		}

		private void TextInputs(PageContext page)
		{
			var form = page.Root.Form();
			form.Property("Value", () => m_text);

			// When typed text reaches the setter
			form.TextInput("Delayed", "On Enter or deselect", () => m_text, value => m_text = value, InputMode.Delayed);
			form.TextInput("Debounced", "When typing pauses", () => m_text, value => m_text = value, InputMode.Debounced);
			form.TextInput("Immediate", "Every keystroke", () => m_text, value => m_text = value, InputMode.Immediate);
		}

		private void NumberInputs(PageContext page)
		{
			var form = page.Root.Form();
			form.IntInput("Int", () => m_int, value => m_int = value);

			// The setter can clamp: the field shows the clamped value once editing ends
			form.IntInput("Int (0-10)", () => m_int, value => m_int = Mathf.Clamp(value, 0, 10));

			form.FloatInput("Float", () => m_float, value => m_float = value);
			form.FloatInput("Float (0.00)", () => m_float, value => m_float = value).WithFormat("0.00");
		}

		private void Sliders(PageContext page)
		{
			var form = page.Root.Form();
			form.Slider("Float", 0f, 1f, () => m_float, value => m_float = value).WithFormat("0.00");
			form.SliderInt("Int", 0, 10, () => m_int, value => m_int = value);
			form.Slider("Time Scale", 0f, 2f, () => Time.timeScale, value => Time.timeScale = value).WithFormat("0.0x");
		}

		private void Dropdowns(PageContext page)
		{
			var form = page.Root.Form();

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

		private void Layout(PageContext page)
		{
			var form = page.Root.Form();
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
			var card = page.Root.Card();
			card.Heading("A card");
			card.Property("Clicks", () => m_clicks);
		}

		private void Search(PageContext page)
		{
			// A search field over a list, with each result built by a row builder
			page.Search(page.Root.Form(), () => m_items, item => item, row => {
				var line = row.Root.Row()
					.WithAlignment(LayoutAlignment.SpaceBetween)
					.WithCrossAlignment(CrossAlignment.Center)
					.WithPadding(0);
				line.Label(row.Item);
				line.Button("Pick", () => m_item = row.Item);
			}).WithScroll(150)
			.WithResultSpacing(1)
			.WithMaxResults(8);
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
