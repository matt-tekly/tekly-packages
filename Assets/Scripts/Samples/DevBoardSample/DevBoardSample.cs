using Tekly.DevBoard;
using Tekly.DevBoard.Components.Inputs;
using UnityEngine;

namespace TeklySample.Samples.DevBoardSample
{
	public class DevBoardSample : MonoBehaviour
	{
		private void Start()
		{
			var board = DevBoard.Instance.Board("Test Board")
				.WithMaxHeight(250)
				.ScrollView()
				;
			
			string testValue = "Starting Value";
			bool toggleValue = false;
			int intValue = 0;
			float floatValue = 0;
			
			var card = board.Card().Form();
			var timeProperty = card.Property("Bingus", () => Time.realtimeSinceStartup, "{0:N2}");
			card.Toggle("Monospaced", () => timeProperty.Value.Monospaced, value => timeProperty.Value.Monospaced = value);
			card.PropertyMonospaced("Bingus Mono", () => Time.realtimeSinceStartup, "{0:N2}");
			
			card.Divider();
			card.Property("Test Value", () => testValue);
			card.Property("Test Value", () => testValue);
			card.TextInput("Debounced", "Placeholder", () => testValue, value => testValue = value, InputMode.Debounced);
			card.TextInput("Delayed", "Placeholder", () => testValue, value => testValue = value, InputMode.Delayed);
			card.TextInput("Immediate", "Placeholder", () => testValue, value => testValue = value, InputMode.Immediate);
			card.Toggle("Togglo", () => toggleValue, value => toggleValue = value);
			card.IntInput("Int", () => intValue, value => intValue = value);
			card.FloatInput("Float", () => floatValue, value => floatValue = value);

			var row = card.Row();
			row.Button("a", () => Debug.Log("a"));
			row.Button("b", () => Debug.Log("b"));
			row.Button("c", () => Debug.Log("c"));
			
			var row2 = card.Row();
			row2.Button("a", () => Debug.Log("a"));
			row2.Button("b", () => Debug.Log("b"));
			row2.Button("c", () => Debug.Log("c"));

			card.FloatInput("Float", () => floatValue, value => floatValue = value);

			// var row = board.Row().WithSpacing(0);
			// row.Button("Poop", () => Debug.Log("Poop"));
			// row.Button("Poop 2", () => Debug.Log("Poop"));
			//
			//
			// var foldout = card.Foldout("Test Foldout");
			// foldout.InfoContainer.Property("Time", () => Time.realtimeSinceStartup, "{0:N2}");
			// foldout.Property("Real Time", () => Time.realtimeSinceStartup, "{0:N2}");
			// foldout.Divider();
			// foldout.Button("Poop", () => Debug.Log("Poop"));
			// foldout.Button("Poop 2", () => Debug.Log("Poop"));
		}
		
		
		#warning Scrolling Area, Dropdown Search Thingy - maybe this is full screen?
		// Text Field - maybe a text area?
		// Number Field
		// Scroll View
		// Dropdown Search that could be a full screen spotlight type thing
		// Slider
		// Foldout?
	}
}