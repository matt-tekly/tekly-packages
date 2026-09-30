using Tekly.DevBoard.Components;
using UnityEngine;

namespace Tekly.DevBoard
{
	public class DevBoardTest : MonoBehaviour
	{
		private void Start()
		{
			var board = DevBoard.Instance.Board("Test Board");

			string testValue = "Starting Value";
			
			var card = board.Card().Form();
			card.Property("Bingus", () => Time.realtimeSinceStartup, "{0:N2}");
			card.Property("Test Value", () => testValue);
			card.Divider();
			card.TextInput("Debounced", "yo", () => testValue, value => testValue = value, InputMode.Debounced);
			card.TextInput("Delayed", "yo", () => testValue, value => testValue = value, InputMode.Delayed);
			card.TextInput("Immediate", "yo", () => testValue, value => testValue = value, InputMode.Immediate);

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
		
		
		#warning Text Field, Number Field, Scrolling Area, Dropdown Search Thingy - maybe this is full screen?
		// Text Field - maybe a text area?
		// Number Field
		// Scroll View
		// Dropdown Search that could be a full screen spotlight type thing
		// Slider
		// Foldout?
	}
}