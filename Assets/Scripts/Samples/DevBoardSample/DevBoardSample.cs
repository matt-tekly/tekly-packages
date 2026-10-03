using System.Collections.Generic;
using Tekly.DevBoard;
using Tekly.DevBoard.Components.Inputs;
using Tekly.DevBoard.Pages;
using Tekly.Trellis;
using UnityEngine;

namespace TeklySample.Samples.DevBoardSample
{
	public class DevBoardSample : MonoBehaviour
	{
		// State lives here, not in the builders: builders can run several times (once per panel showing the page)
		private string m_testValue = "Starting Value";
		private bool m_toggleValue;
		private int m_intValue;
		private float m_floatValue;
		private int m_coins;

		// A stand-in item database and inventory for the search sample
		private readonly List<string> m_items = new();
		private readonly Dictionary<string, int> m_inventory = new();

		private void DoPage(PageContext page)
		{
			var card = page.Root.Form();
			var timeProperty = card.Property("Bingus", () => Time.realtimeSinceStartup, "{0:N2}");
			card.Toggle("Monospaced", () => timeProperty.Value.Monospaced, value => timeProperty.Value.Monospaced = value);
			card.PropertyMonospaced("Bingus Mono", () => Time.realtimeSinceStartup, "{0:N2}");

			card.Divider();
			card.Property("Test Value", () => m_testValue);
			card.TextInput("Debounced", "Placeholder", () => m_testValue, value => m_testValue = value, InputMode.Debounced);
			card.TextInput("Delayed", "Placeholder", () => m_testValue, value => m_testValue = value, InputMode.Delayed);
			card.TextInput("Immediate", "Placeholder", () => m_testValue, value => m_testValue = value, InputMode.Immediate);
			card.Toggle("Togglo", () => m_toggleValue, value => m_toggleValue = value);
			card.IntInput("Int", () => m_intValue, value => m_intValue = value);
			card.FloatInput("Float", () => m_floatValue, value => m_floatValue = value);

			var row = card.Row();
			row.Button("a", () => Debug.Log("a"));
			row.Button("b", () => Debug.Log("b"));
			row.Button("c", () => Debug.Log("c"));

			var foldout = card.Foldout("Test Foldout");
			foldout.Property("Real Time", () => Time.realtimeSinceStartup, "{0:N2}");
			foldout.Button("Log", () => Debug.Log("Foldout button"));
		}
		
		private void Start()
		{
			var devBoard = DevBoard.Instance;

			string[] materials = { "Wood", "Stone", "Iron", "Gold", "Crystal", "Bone", "Leather", "Silk" };
			string[] things = { "Sword", "Shield", "Helmet", "Boots", "Ring", "Potion", "Arrow", "Bow", "Wand", "Key" };

			foreach (var material in materials) {
				foreach (var thing in things) {
					m_items.Add($"{material} {thing}");
				}
			}

			m_items.Sort();

			// A search whose results are built by a row builder
			devBoard.Page("Sample/Inventory", page => {
				page.Search(page.Root.Form(), () => m_items, item => item, row => {
					var line = row.Root.Row()
						.WithCrossAlignment(CrossAlignment.Center)
						.WithPadding(0);
					
					line.Property(row.Item, () => m_inventory.TryGetValue(row.Item, out var count) ? count : 0)
						.WithWidthGroup("property");
					
					line.Button("+1", () => AddItem(row.Item, 1));
					line.Button("+10", () => AddItem(row.Item, 10));
				}).WithScroll(150)
				.WithResultSpacing(1)
				.WithMaxResults(8);
				
			}).BindTo(gameObject);

			devBoard.Page("Sample/Widgets/Smoop", page => {
				var card = page.Root.Form();
				card.Property("Bingus", () => Time.realtimeSinceStartup, "{0:N2}");
			});
			
			// Each segment is removed when this GameObject is destroyed
			devBoard.Page("Sample/Widgets", DoPage).BindTo(gameObject);
			devBoard.Page("Sample/Widgets", DoPage).BindTo(gameObject);

			// Two segments on the same page, as if registered by two different systems
			devBoard.Page("Sample/Cheats", page => {
				page.Root.Property("Coins", () => m_coins);
				page.Root.Button("Add 100 coins", () => m_coins += 100);
			}, "Economy").BindTo(gameObject);

			devBoard.Page("Sample/Cheats", page => {
				page.Root.Button("Reset test value", () => m_testValue = "Starting Value");
				page.Root.Property("Reset test value", () => m_testValue);
			}, "Misc", order: 10).BindTo(gameObject);

			// A small page meant for an overlay: open it, then press Pop
			devBoard.Page("Sample/Stats", page => {
				page.Root.PropertyMonospaced("Time", () => Time.realtimeSinceStartup, 0.05f, "{0:N1}");
				page.Root.PropertyMonospaced("Frame", () => Time.frameCount);
			}).BindTo(gameObject);
		}


		private void AddItem(string item, int count)
		{
			m_inventory.TryGetValue(item, out var current);
			m_inventory[item] = current + count;
		}

		#warning Dropdown Search Thingy - maybe this is full screen?
		// Text Field - maybe a text area?
		// Number Field
		// Scroll View
		// Dropdown Search that could be a full screen spotlight type thing
		// Slider
		// Foldout?
	}
}
